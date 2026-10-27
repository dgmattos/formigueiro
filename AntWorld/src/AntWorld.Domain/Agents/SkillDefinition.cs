namespace AntWorld.Domain;
public sealed record SkillDefinition(SkillType Type, string Name, string Description, AntCapability RequiredCapability,
    int MaximumLevel = 20, double EnergyCost = .2, double EfficiencyPerLevel = .03);
