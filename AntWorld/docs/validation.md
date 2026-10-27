# Validação — atualização de blocos v2

Ambiente: macOS arm64, .NET SDK 10.0.201 e Godot 4.6.3 .NET. Validação realizada em 26/10/2026.

## Resultado

- **Build completo: aprovado, zero erros e zero avisos.** Inclui Godot, SQLite, testes e benchmarks.
- **17 testes xUnit reais aprovados**, zero falhas/ignorados/erros, por `sh scripts/verify-full.sh`.
- `sh scripts/verify-core.sh`: cenários de domínio, geração, genética, simulação, snapshots e novos blocos aprovados; CLI de 1.500 ticks executado.
- Godot headless: cena inicializada e teste integrado de velocidade, pausa, SQLite, restauração, HUD e feromônios aprovado; processo retornou 0.
- O processo Godot ainda emite os erros ambientais de acesso a `~/Library` e certificados documentados na primeira entrega. Portanto não é um teste gráfico estrito limpo; não houve nova validação visual da janela nesta atualização.
- Snapshot v1: rejeição explícita testada. Sem migração aproximada de dados antigos.

A matriz que relaciona os 18 requisitos de teste aos cenários está em [world-blocks.md](world-blocks.md#testes-e-benchmarks). Os testes também verificam ausência de alocações por célula, limite de cache, consulta descarregada sem geração e invalidação de autotile na emenda.

## Benchmarks executados

Comando: `sh scripts/benchmark-world.sh`. Configuração: seed 42, 512×128 células, chunks 16×16, limite de oito chunks residentes; Release, uma rodada de aquecimento e cinco medições. Tamanhos/alocações são medidos no processo; os resultados são sensíveis ao JIT e à carga local.

| Operação | Quantidade por rodada | Média (ms) | Média alocada (bytes) |
|---|---:|---:|---:|
| world_metadata_and_deposit_plan | 1 | 2.731 | 444496 |
| chunk_generation | 256 | 37.162 | 1284488 |
| logical_cell_queries | 100000 | 17.696 | 40 |
| orthogonal_neighbors | 100000 | 11.339 | 40 |
| resource_distribution_into_chunks | 256 | 22.912 | 205192 |
| serialization | 1 | 0.589 | 324120 |
| deserialization | 1 | 2.827 | 1867482 |

Os 40 bytes medidos nas consultas pertencem ao `Stopwatch` da medição, não às células/vizinhos. O teste de alocação aquece e faz 10.000 consultas, aceitando menos de 1 KiB no total; passou. WorldCell tem 16 bytes e não contém referências.

Snapshot-base: **6.052 bytes**. Após 100 ticks: **33.388 bytes**, com 539 células e 322 estoques alterados. Chunks residentes ao final do benchmark: **8**, respeitando o limite. Os arrays do terreno-base não foram gravados. O SQLite continua fazendo upsert do BLOB de deltas acumulados, sem journal SQL por chunk.

Dados completos: `artifacts/world-benchmarks.json`. O relatório JSON da execução de 1.500 ticks fica em `artifacts/run.json`; o resultado mudou em relação ao gerador v1, como esperado após a evolução das regras de geração.

## Godot

```sh
ANTWORLD_DATA_DIR="$PWD/artifacts/blocks-v2-godot" \
  .tools/Godot_mono.app/Contents/MacOS/Godot \
  --headless --path src/AntWorld.Game \
  --log-file "$PWD/artifacts/blocks-v2-godot.log" \
  --quit-after 120 -- --smoke-test
```

O log registra `AntWorld: smoke test PASS`. O atlas de primavera é o único utilizado, por orientação do usuário. O seletor de estações permite futuras texturas. Input e câmera foram preservados; streaming visual e recortes materiais compilaram e executaram em headless, sem revisão visual em janela.

Limitações e melhorias recomendadas estão em [world-blocks.md](world-blocks.md#limitações-e-próximos-passos). [Registro histórico da primeira entrega](validation-v1.md).
