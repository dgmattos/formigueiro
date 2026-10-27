namespace AntWorld.Domain;
public static class SkillCatalog
{
    private static readonly SkillDefinition[] Definitions =
    [ new(SkillType.Digging,"Escavação","Remove solo conforme resistência.",AntCapability.Dig),
      new(SkillType.Carrying,"Transporte","Transporta carga sem duplicá-la.",AntCapability.Carry),
      new(SkillType.Combat,"Combate","Ataque com intervalo.",AntCapability.Attack),
      new(SkillType.Exploration,"Exploração","Procura recursos próximos.",AntCapability.Explore),
      new(SkillType.BroodCare,"Cuidado das crias","Alimenta e transporta crias reconhecidas.",AntCapability.BroodCare),
      new(SkillType.Construction,"Construção","Reforça solo parcialmente escavado.",AntCapability.Build),
      new(SkillType.Reproduction,"Reprodução","Produz ovos com custo alimentar.",AntCapability.Reproduce),
      new(SkillType.Navigation,"Navegação","Desloca-se por caminhos válidos.",AntCapability.Move),
      new(SkillType.Defense,"Defesa","Defende contra ameaça reconhecida.",AntCapability.Defend),
      new(SkillType.ResourceDetection,"Detecção","Localiza alimento.",AntCapability.Collect) ];
    public static SkillDefinition Get(SkillType type) => Definitions[(int)type];
}
