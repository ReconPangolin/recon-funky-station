using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Botany.PlantAnalyzer;

[Serializable, NetSerializable]
public sealed class PlantAnalyzerUserMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public int AnalyzerTier;
    public float Production;
    public float Maturation;
    public int Yield;
    public float Potency;
    public string PlantName;
    public PlantAnalyzerUserMessage(NetEntity? targetEntity, int analyzerTier, float production, float maturation,
        int yield, float potency, string plantName)
    {
        TargetEntity = targetEntity;
        AnalyzerTier = analyzerTier;

        //Tier 1 and above stats
        Production = production;
        Maturation = maturation;
        Yield = yield;
        PlantName = plantName;
        Potency = potency;

        if (analyzerTier > 1)
        {

        }

        if (analyzerTier > 2)
        {

        }
    }
}
