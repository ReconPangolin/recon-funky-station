using Content.Server.Botany.Components;
using Content.Server.PowerCell;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

using Content.Shared._Funkystation.Botany.PlantAnalyzer;


namespace Content.Server._Funkystation.Botany.PlantAnalyzer;

public sealed class ReconPlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReconPlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ReconPlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ReconPlantAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<ReconPlantAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ReconPlantAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<ReconPlantAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            //Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not {} plant)
                continue;

            if (Deleted(plant))
            {
                StopAnalyzingEntity((uid, component), plant);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            //Get distance between health analyzer and the scanned entity
            var plantCoords = Transform(plant).Coordinates;
            if (!_transformSystem.InRange(plantCoords, transform.Coordinates, component.MaxScanRange))
            {
                //Range too far, disable updates
                StopAnalyzingEntity((uid, component), plant);
                continue;
            }

            UpdateScannedUser(uid, plant, true);
        }
    }

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<ReconPlantAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantHolderComponent>(args.Target) || !_cell.HasDrawCharge(uid, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new PlantAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });
    }

    private void OnDoAfter(Entity<ReconPlantAnalyzerComponent> uid, ref PlantAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<ReconPlantAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { } plant)
            _toggle.TryDeactivate(uid.Owner);
    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<ReconPlantAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } patient)
            StopAnalyzingEntity(ent, patient);
    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<ReconPlantAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, ReconPlantAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, ReconPlantAnalyzerUiKey.Key, user);
    }

    /// <summary>
    /// Mark the entity as having its health analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that should receive the updates</param>
    /// <param name="target">The entity to start analyzing</param>
    private void BeginAnalyzingEntity(Entity<ReconPlantAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        //Link the health analyzer to the scanned entity
        healthAnalyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(healthAnalyzer.Owner);

        UpdateScannedUser(healthAnalyzer, target, true);
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void StopAnalyzingEntity(Entity<ReconPlantAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        //Unlink the analyzer
        healthAnalyzer.Comp.ScannedEntity = null;
        _toggle.TryDeactivate(healthAnalyzer.Owner);

        UpdateScannedUser(healthAnalyzer, target, false);
    }

    /// <summary>
    /// Send an update for the target to the healthAnalyzer
    /// </summary>
    /// <param name="plantAnalyzer">The health analyzer</param>
    /// <param name="plantHolder">The entity being scanned</param>
    /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
    public void UpdateScannedUser(EntityUid plantAnalyzer, EntityUid plantHolder, bool scanMode)
    {
        if (!_uiSystem.HasUi(plantAnalyzer, ReconPlantAnalyzerUiKey.Key))
            return;

        _uiSystem.ServerSendUiMessage(plantAnalyzer, ReconPlantAnalyzerUiKey.Key, new PlantAnalyzerUserMessage(
            GetNetEntity(plantHolder),
            1,
            3,
            6,
            3,
            "Apple"));
    }
}
