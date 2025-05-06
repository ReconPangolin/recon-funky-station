using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Botany.PlantAnalyzer;

[Serializable, NetSerializable]
public sealed class PlantAnalyzerUserMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public int AnalyzerTier;
    public int Production;
    public int Maturation;
    public int Yield;
    public string PlantName;
    public PlantAnalyzerUserMessage(NetEntity? targetEntity, int analyzerTier, int production, int maturation,
        int yield, string plantName)
    {
        TargetEntity = targetEntity;
        AnalyzerTier = analyzerTier;

        //Tier 1 and above stats
        Production = production;
        Maturation = maturation;
        Yield = yield;
        PlantName = plantName;

        if (analyzerTier > 1)
        {

        }

        if (analyzerTier > 2)
        {

        }
    }
}
