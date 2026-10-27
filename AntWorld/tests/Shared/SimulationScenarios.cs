using AntWorld.Domain;
using AntWorld.Simulation;
namespace AntWorld.Specifications;

public static class SimulationScenarios
{
    public static void Run()
    {
        ClockIsIndependentOfFrames();
        var first = new SimulationEngine(SimulationFactory.Create(new(42, 96, 48)));
        var second = new SimulationEngine(SimulationFactory.Create(new(42, 96, 48)));
        first.Run(600); second.Run(600);
        Check.That(first.State.Random.State == second.State.Random.State && first.State.Ants.Select(a => (a.Position, a.Energy, a.Health)).SequenceEqual(second.State.Ants.Select(a => (a.Position, a.Energy, a.Health))), "Simulação não determinística.");
        Check.That(first.State.Ants.All(a => first.State.World.CanWalk(a.Position)), "Agente atravessou rocha/céu.");
        Check.That(first.State.Colonies.Sum(c => c.Births) > 0, "Sem reprodução.");
        Check.That(first.State.Pheromones.Values.Any(p => p.Any(v => v > 0)), "Sem coleta/trilhas.");
        Check.That(first.State.World.Food.Zip(first.State.World.FoodCapacity).All(p => p.First >= 0 && p.First <= p.Second), "Recurso fora da capacidade.");
        var winter = EnvironmentSystem.TemperatureAt(first.State, first.State.Colonies[0].Nest);
        first.State.Tick = SimulationState.TicksPerDay * SimulationState.DaysPerSeason * 3 + 60;
        Check.That(first.State.Season == 3 && EnvironmentSystem.TemperatureAt(first.State, first.State.Colonies[0].Nest) < winter, "Estação não afeta temperatura.");
        StarvationAndCombat();
        ClimateAndFounding();
    }

    private static void ClockIsIndependentOfFrames()
    {
        var a = new SimulationClock(); var b = new SimulationClock();
        var ticksA=0;var ticksB=0;
        for(var i=0;i<600;i++) a.Advance(1.0/60,()=>ticksA++);
        for(var i=0;i<100;i++) b.Advance(.1,()=>ticksB++);
        Check.That(ticksA==200&&ticksA==ticksB,"Relógio depende do FPS.");
        a.Speed=100;a.Advance(1,()=>ticksA++,10);
        Check.That(a.IsBehind&&a.PendingSeconds>=99.49,"Ticks descartados sob carga.");
        a.Speed=0;a.Advance(10,()=>ticksA++);
        Check.That(ticksA==210,"Pausa consumiu backlog.");
        a.Speed=1;a.Advance(0,()=>ticksA++,4000);
        Check.That(ticksA==2200,"Backlog não recuperado.");
        Check.Throws<ArgumentOutOfRangeException>(()=>a.Advance(double.NaN,()=>{}));
        a.Speed=100;Check.Throws<ArgumentOutOfRangeException>(()=>a.Advance(double.MaxValue,()=>{}));
    }

    private static void StarvationAndCombat()
    {
        var state = SimulationFactory.Create(new(7, 64, 48));
        state.World.ClearFood(true);
        foreach (var colony in state.Colonies) colony.Food = 0;
        // Start critically ill to exercise removal for every caste without waiting several biological days.
        foreach(var ant in state.Ants) { ant.Energy=0;ant.Nutrition=0;ant.Hydration=0;ant.Health=1; }
        var engine = new SimulationEngine(state); engine.Run(4000);
        Check.That(state.Ants.Count == 0 && state.Colonies.All(c => !c.IsAlive), "Colônia imortal sem recursos.");
        var battle = SimulationFactory.Create(new(1, 64, 48));
        battle.Relations.Set(1,2,ColonyRelation.Hostile);
        var cell = battle.Ants[0].Position;
        foreach (var ant in battle.Ants) ant.Position = cell;
        var health = battle.Ants.Sum(a => a.Health);
        new CombatSystem().Step(battle);
        Check.That(battle.Ants.Sum(a => a.Health) < health, "Encontro hostil sem combate.");
    }
    private static void ClimateAndFounding()
    {
        var state = SimulationFactory.Create(new(42));
        var environment = new EnvironmentSystem(state.World);
        state.World.ClearFood();
        var humidity = state.World.Moisture.Sum();
        state.Tick = 1; state.RainIntensity = 1; environment.Step(state);
        Check.That(state.World.Food.Sum() > 0 && state.World.Moisture.Sum() > humidity, "Chuva/regeneração não alteraram o mundo.");
        state.RainIntensity = 0; environment.Step(state);
        Check.That(state.DryTicks == 1, "Seca não contabilizada.");
        var colonySystem = new ColonySystem();
        foreach (var c in state.Colonies) c.Food = 1000;
        var founder=AntFactory.Create(state,state.Colonies[0],AntCaste.Queen,motherId:state.Colonies[0].QueenId); state.Ants.Add(founder);
        var population=state.Ants.Count;
        for (var i = 1; i <= 20; i++) { state.Tick = i * 1200; colonySystem.Step(state); }
        Check.That(state.Ants.Count==population,"Fundação criou ou destruiu indivíduos.");
        Check.That(state.Colonies.Any(c => c.Generation == 1), "Colônias bem alimentadas não fundaram descendentes.");
        foreach (var c in state.Colonies) c.QueenGenome.Validate();
    }

}
