namespace AntWorld.Domain;
public sealed class SpeciesDefinition
{
    public double SoilDiggingMultiplier { get; init; } = 1;
    public double MixedDiggingMultiplier { get; init; } = 1;
    public double SolidRockSpeedFactor { get; init; } = .4;
    public double SolidRockEnergyFactor { get; init; } = 1.6;
    public double SurfaceMovementMultiplier { get; init; } = 1;
    public static SpeciesDefinition Strong => new() { SoilDiggingMultiplier = 1.25, MixedDiggingMultiplier = 1.4, SolidRockSpeedFactor = .8, SolidRockEnergyFactor = 1.2 };
    public static SpeciesDefinition Surface
    {
        get
        {
            var species = new SpeciesDefinition { SurfaceMovementMultiplier = 1.2 };
            var soldier = species.For(AntCaste.Soldier);
            species.Castes[(int)AntCaste.Soldier] = soldier with { Attack = soldier.Attack * 1.2, Defense = soldier.Defense * 1.2 };
            return species;
        }
    }
    public BehaviorProfile[] Behaviors { get; init; } = [new(0,.8,0,0,0,1),new(1,.4,1,1,1,0),new(0,1,0,0,.6,0),new(0,.1,0,0,0,0)];
    public double EggDays { get; init; } = 2;
    public double LarvaDays { get; init; } = 15;
    public double PupaDays { get; init; } = 5;
    public int EggIntervalTicks { get; init; } = 480;
    public double EggCost { get; init; } = 3;
    public CasteDefinition[] Castes { get; init; } =
    [
        new(250,.5,15,20,0,0, AntCapability.Move|AntCapability.Defend|AntCapability.Reproduce|Common),
        new(60,1,8,5,20,180,AntCapability.Move|AntCapability.Explore|AntCapability.Collect|AntCapability.Carry|AntCapability.Dig|AntCapability.Build|AntCapability.Defend|AntCapability.BroodCare|Common),
        new(130,.8,30,18,10,240,AntCapability.Move|AntCapability.Explore|AntCapability.Carry|AntCapability.Attack|AntCapability.Defend|Common),
        new(40,.9,4,2,0,90,AntCapability.Move|Common)
    ];
    private const AntCapability Common = AntCapability.Feed|AntCapability.Rest|AntCapability.Recover;
    public CasteDefinition For(AntCaste caste) => Castes[(int)caste];
    public void Validate()
    {
        if (!double.IsFinite(SurfaceMovementMultiplier) || SurfaceMovementMultiplier is <= 0 or > 10)
            throw new ArgumentException("Multiplicador de movimento na superfície inválido.");
        if (new[] { SoilDiggingMultiplier, MixedDiggingMultiplier, SolidRockSpeedFactor, SolidRockEnergyFactor }.Any(v => !double.IsFinite(v) || v <= 0 || v > 10))
            throw new ArgumentException("Coeficientes de escavação inválidos.");
        if (Castes.Length != 4 || EggIntervalTicks < 1 || EggIntervalTicks > 10000000 ||
            new[] {EggDays,LarvaDays,PupaDays,EggCost}.Any(v => !double.IsFinite(v) || v <= 0 || v > 10000)) throw new ArgumentException("Ciclo biológico inválido.");
        if(Behaviors.Length!=4 || Behaviors.Any(b => new[]{b.Foraging,b.Defense,b.Excavation,b.BroodCare,b.Exploration,b.Reproduction}.Any(v=>!double.IsFinite(v)||v<0||v>10))) throw new ArgumentException("Perfil comportamental inválido.");
        foreach (var caste in Castes) caste.Validate();
    }
}
