using System.Text.Json;
using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class SimulationDiagnosticsTests
{
    [Fact]
    public void TerminalBiologyRecordsContributingCausesBeforeRemovingIndividualExactlyOnce()
    {
        var state = SimulationFactory.Create(new(42, 64, 48));
        var colony = state.Colonies[0];
        var temperature = EnvironmentSystem.TemperatureAt(state, colony.Nest);
        var victim = AntFactory.Create(state, colony, genes: Genome.Ember with { ThermalOptimum = Math.Clamp(temperature, -5, 40), ColdTolerance = 25 });
        victim.Energy = victim.Nutrition = victim.Hydration = 0; victim.Health = .001;
        state.Ants.Add(victim);
        var engine = new SimulationEngine(state); var deaths = new List<AntDeath>();
        engine.AntDied += death => { Assert.Contains(victim, state.Ants); deaths.Add(death); };
        engine.Run(2);
        var death = Assert.Single(deaths);
        Assert.Equal(victim.Id, death.Identity.Id);
        Assert.Equal(DamageCause.EnergyDepletion | DamageCause.Starvation | DamageCause.Dehydration, death.Causes);
        Assert.Equal(.001, death.HealthBeforeDamage);
        Assert.Equal(1, death.Tick); Assert.Equal(1, colony.Deaths);
        Assert.DoesNotContain(victim, state.Ants);
    }
    [Fact]
    public void CombatDeathKeepsAttackerAndCannotBeOverwrittenBySubsequentDamage()
    {
        var state = SimulationFactory.Create(new(42, 64, 48));
        var attacker = state.Ants.First(a => a.Caste == AntCaste.Soldier);
        var victim = state.Ants.First(a => a.ColonyId != attacker.ColonyId && a.Caste == AntCaste.Worker);
        victim.Position = attacker.Position; victim.Health = .001;
        state.Relations.Set(attacker.ColonyId, victim.ColonyId, ColonyRelation.Hostile);
        Assert.True(new CombatSystem().Attack(state, attacker, victim));
        victim.Damage(10, DamageCause.Starvation);
        AntDeath? death = null; var engine = new SimulationEngine(state); engine.AntDied += d => death = d;
        engine.Step();
        Assert.NotNull(death); Assert.Equal(DamageCause.Combat, death.Causes); Assert.Equal(attacker.Id, death.AttackerId);
    }
    [Fact]
    public void AcceleratedAndNormalTimeHaveIdenticalBiologyAndDiagnosticsDoNotChangeSnapshots()
    {
        var normal = SimulationFactory.Create(new(42, 64, 48));
        var codec = new BinarySnapshotCodec(); var fast = codec.Decode(codec.Encode(normal));
        var first = new SimulationEngine(normal); var second = new SimulationEngine(fast);
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            using (var log = new SimulationDiagnostics(directory, fast))
            {
                second.AntDied += log.RecordDeath;
                var slowClock = new SimulationClock(); var fastClock = new SimulationClock { Speed = 50 };
                for (var i = 0; i < 1000; i++) slowClock.Advance(.1, first.Step);
                for (var i = 0; i < 20; i++) fastClock.Advance(.1, () => { second.Step(); log.Observe(fast); });
                Assert.Equal(2000, fast.Tick); Assert.Equal(codec.Encode(normal), codec.Encode(fast));
                fastClock.Speed = 0; fastClock.Advance(100, second.Step); Assert.Equal(2000, fast.Tick);
                log.Flush(); Assert.Null(log.Failure);
            }
            var records = File.ReadLines(Path.Combine(directory, "simulation.jsonl")).Select(line => JsonDocument.Parse(line)).ToArray();
            Assert.Contains(records, r => r.RootElement.GetProperty("type").GetString() == "session_started");
            Assert.Contains(records, r => r.RootElement.GetProperty("type").GetString() == "population_sample");
            Assert.Contains(records, r => r.RootElement.GetProperty("type").GetString() == "session_closed");
            foreach (var record in records) record.Dispose();
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void ExtinctionIsRecordedAndLogRotationBoundsDiskUsage()
    {
        var state = SimulationFactory.Create(new(42, 64, 48));
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            using var log = new SimulationDiagnostics(directory, state, new(MaxFileSizeMb: 1, RetainedFiles: 2));
            var engine = new SimulationEngine(state); engine.AntDied += log.RecordDeath;
            foreach (var ant in state.Ants) ant.Damage(ant.Health, DamageCause.Aging);
            engine.Step(); log.Observe(state); log.Flush();
            var text = File.ReadAllText(log.FilePath);
            Assert.Contains("population_extinct", text); Assert.Contains("ant_died", text); Assert.Contains("Aging", text);
            for (var i = 0; i < 16000; i++)
            {
                log.RecordPlayback(i, 50, i / 2.0, 1000);
                if (i % 100 == 0) log.Flush();
            }
            log.Flush();
            Assert.Null(log.Failure);
            Assert.Equal(2, Directory.GetFiles(directory).Length);
            Assert.All(Directory.GetFiles(directory), path => Assert.InRange(new FileInfo(path).Length, 1, 1024 * 1024));
        }
        finally { Directory.Delete(directory, true); }
    }
    [Theory]
    [InlineData("{\"diagnostics\":null}")]
    [InlineData("{\"diagnostics\":{\"sampleIntervalSeconds\":0}}")]
    [InlineData("{\"diagnostics\":{\"maxFileSizeMb\":0}}")]
    [InlineData("{\"diagnostics\":{\"retainedFiles\":11}}")]
    public void InvalidSettingsAreRejected(string json) => Assert.Throws<ArgumentException>(() => SimulationLogSettings.Parse(json));
}
