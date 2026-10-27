using AntWorld.Specifications;
if (args.Contains("--blocks-only"))
{
    BlockScenarios.Run(); Console.WriteLine("PASS Blocks: composição, água, vizinhos, streaming, depósitos, extração e biomas."); return;
}
DomainScenarios.Run();
Console.WriteLine("PASS Domain: validação, PRNG, chunks, colisão e escavação.");
GenerationScenarios.Run();
Console.WriteLine("PASS WorldGeneration / Genetics: 5 seeds, camadas, recursos, biomas e 10.000 mutações.");
SimulationScenarios.Run();
Console.WriteLine("PASS Simulation: FPS, pausa, backlog, determinismo, ecologia, reprodução, combate e extinção.");
SnapshotScenarios.Run();
Console.WriteLine("PASS Infrastructure: snapshot binário, integridade, arquivo atômico e continuação determinística.");
BlockScenarios.Run();
BlockPersistenceScenarios.Run();
Console.WriteLine("PASS Blocks v2: modelo compacto, streaming, depósitos, extração e persistência incremental.");
