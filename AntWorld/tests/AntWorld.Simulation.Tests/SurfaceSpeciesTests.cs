using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class SurfaceSpeciesTests
{
    [Fact]
    public void SurfaceSpeciesSpawnsShallowWithNurseryAndKeepsOtherNests()
    {
        var state = SimulationFactory.Create(new(42, 96, 96), [new(0, .25), new(2, .75), new(3, .5)]);
        var colony = state.Colonies[2];
        Assert.Equal(state.World.Surface[colony.Nest.X] + 2, colony.Nest.Y);
        Assert.True(state.World.CanWalk(colony.BroodChamber));
        Assert.Equal(colony.Nest.Y + 4, colony.BroodChamber.Y);
        Assert.All(state.Ants.Where(a => a.ColonyId == colony.Id), a => Assert.Equal(3, a.SpeciesId));
        Assert.Equal(state.World.Surface[state.Colonies[0].Nest.X] + 5, state.Colonies[0].Nest.Y);
        var founded = SimulationFactory.ExcavateNest(state.World, 5, 3);
        Assert.Equal(state.World.Surface[5] + 2, founded.Y);
        var custom = SimulationFactory.Create(new(42, 96, 96) { Nests = new(SurfaceDepth: 3) }, [new(3, .5)]);
        Assert.Equal(custom.World.Surface[48] + 3, custom.Colonies[0].Nest.Y);
        Assert.Throws<ArgumentException>(() => new NestSettings(SurfaceDepth: 0).Validate());
    }
    [Theory]
    [InlineData(AntCaste.Worker)]
    [InlineData(AntCaste.Soldier)]
    [InlineData(AntCaste.Queen)]
    public void ExposedSurfaceGrantsTwentyPercentWithoutChangingUndergroundSpeed(AntCaste caste)
    {
        var state = SimulationFactory.Create(new(42, 96, 64), [new(0, .25), new(3, .5)]);
        var normal = AntFactory.Create(state, state.Colonies[0], caste, genes: Genome.Ember);
        var surface = AntFactory.Create(state, state.Colonies[1], caste, genes: Genome.Ember);
        var world = state.World; const int x = 95, y = 12;
        for (var row = 0; row < y; row++) world.SetTerrain(new(x, row), TerrainKind.Air);
        world.SetTerrain(new(x, y), TerrainKind.Soil);
        normal.Position = surface.Position = new(x, y - 1);
        normal.Energy = surface.Energy = 50;
        normal.Health = normal.MaxHealth / 2; surface.Health = surface.MaxHealth / 2;
        var movement = new MovementSystem();
        Assert.True(world.IsOnExposedSurface(new(-1, y - 1)));
        Assert.Equal(1.2, movement.EffectiveSpeed(state, surface) / movement.EffectiveSpeed(state, normal), 10);
        world.SetTerrain(new(x, y - 3), TerrainKind.Rock); // Roof removes exposure immediately.
        Assert.False(world.IsOnExposedSurface(surface.Position));
        Assert.Equal(movement.EffectiveSpeed(state, normal), movement.EffectiveSpeed(state, surface), 10);
        world.Dig(new(x, y - 3));
        Assert.Equal(1.2, movement.EffectiveSpeed(state, surface) / movement.EffectiveSpeed(state, normal), 10);
        world.Dig(new(x, y)); // No ground below: being in open air alone is not sufficient.
        Assert.False(world.IsOnExposedSurface(surface.Position));
        surface.Stage = LifeStage.Larva;
        Assert.Equal(0, movement.EffectiveSpeed(state, surface));
        Assert.Equal(0, world.ChunkManager.LoadedCount);
    }
    [Fact]
    public void SoldiersHaveStrongerActualAttacksAndDefenseWhileWorkersKeepTheirStats()
    {
        var normal = new SpeciesDefinition(); var surface = SpeciesDefinition.Surface;
        Assert.Equal(36, surface.For(AntCaste.Soldier).Attack);
        Assert.Equal(21.6, surface.For(AntCaste.Soldier).Defense, 8);
        Assert.Equal(normal.For(AntCaste.Worker), surface.For(AntCaste.Worker));
        Assert.Equal(normal.For(AntCaste.Queen), surface.For(AntCaste.Queen));
        Assert.Equal(1.2, Damage(3, 0) / Damage(0, 0), 8);
        Assert.True(Damage(0, 3) < Damage(0, 0));
        Assert.Throws<ArgumentException>(() => new SpeciesDefinition { SurfaceMovementMultiplier = double.NaN }.Validate());
    }
    private static double Damage(int attackingSpecies, int defendingSpecies)
    {
        var state = SimulationFactory.Create(new(42, 64, 48), [new(attackingSpecies, .25), new(defendingSpecies, .75)]);
        var attacker = AntFactory.Create(state, state.Colonies[0], AntCaste.Soldier, genes: Genome.Ember);
        var target = AntFactory.Create(state, state.Colonies[1], AntCaste.Soldier, genes: Genome.Ember);
        target.Position = attacker.Position;
        state.Relations.Set(attacker.ColonyId, target.ColonyId, ColonyRelation.Hostile);
        var before = target.Health;
        Assert.True(new CombatSystem().Attack(state, attacker, target));
        return before - target.Health;
    }
}
