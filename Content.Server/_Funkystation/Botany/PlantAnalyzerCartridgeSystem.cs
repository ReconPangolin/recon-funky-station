using Content.Server.Botany.Components;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;

namespace Content.Server._Funkystation.Botany;

public sealed class PlantAnalyzerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantAnalyzerCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<PlantAnalyzerCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnCartridgeAdded(Entity<PlantAnalyzerCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        EnsureComp<PlantAnalyzerComponent>(args.Loader);
    }

    private void OnCartridgeRemoved(Entity<PlantAnalyzerCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<PlantAnalyzerCartridgeComponent>(args.Loader))
        {
            RemComp<PlantAnalyzerComponent>(args.Loader);
        }
    }
}
