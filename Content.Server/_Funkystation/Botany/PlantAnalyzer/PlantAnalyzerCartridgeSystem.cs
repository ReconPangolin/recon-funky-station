using Content.Server.Botany.Components;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;

namespace Content.Server._Funkystation.Botany.PlantAnalyzer;

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
        var plantAnalyzer = EnsureComp<ReconPlantAnalyzerComponent>(args.Loader);

        var version = args.Version;

        switch (version)
        {
            case 2:
                plantAnalyzer.Version = 2;
                plantAnalyzer.ScanDelay = TimeSpan.FromSeconds(1);
                break;
            case 3:
                plantAnalyzer.Version = 3;
                plantAnalyzer.ScanDelay = TimeSpan.FromSeconds(0.1);
                break;
            default:
                plantAnalyzer.Version = 1;
                plantAnalyzer.ScanDelay = TimeSpan.FromSeconds(5);
                break;
        }


    }

    private void OnCartridgeRemoved(Entity<PlantAnalyzerCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<PlantAnalyzerCartridgeComponent>(args.Loader))
        {
            RemComp<ReconPlantAnalyzerComponent>(args.Loader);
        }
    }
}
