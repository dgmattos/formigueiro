# Validação dos agentes, IA e controles

Executada em 26/10/2026, macOS arm64, SDK .NET 10.0.201, runtime 10.0.5, 8 processadores lógicos; Godot 4.6.3 .NET.

## Build e testes

- `sh scripts/verify-full.sh`: solução completa compilada com **zero avisos e zero erros**; **55 testes xUnit passaram**, nenhum ignorado. Log: `artifacts/ai-verification.log`.
- Runner usa xUnit real dentro do processo. VSTest requer socket bloqueado pelo ambiente, conforme validação anterior; os testes não são substituídos por mocks.
- Casos cobrem blocos/geração já existentes, biologia/castas, capacidades, saúde/idade, desenvolvimento, skills, coleta/depósito, escavação, alimentação/transporte de larvas, maturação durante transporte, reconhecimento familiar/químico, relações neutras/hostis, escalada territorial, cooldown, emenda circular, prioridades/Utility AI, histerese e reconhecimento sem alocação por interação, distribuição justa, urgência, limites gráficos, trilhas, snapshots e SQLite real.
- Controles: proporção de ticks para 1/50/100, pausa com snapshot inteiro idêntico, retenção de backlog, orçamento de frame, configuração personalizada 1/7/25 e seis entradas inválidas de settings.
- CLI executou 10.000 ticks na seed 42, com snapshot válido e 96 indivíduos ao final; carregou esse arquivo e avançou mais 1.000 ticks. Arquivos: `artifacts/ai-longrun.json`, `ai-longrun.antworld`, `ai-resume.json`. Testes separados comparam continuação restaurada e contínua byte a byte.

## Godot

Importação dos dois novos atlases concluída. A cena executou o teste headless e saiu com código 0, incluindo sinais reais dos botões, leitura de settings, alternância exclusiva pausa/play, multiplicadores, SQLite e restauração. Teste com configuração temporária 1/7/25 passou; arquivo foi restaurado. A última execução leu os valores editados no workspace **1/25/50**, que foram preservados.

Logs: `artifacts/ai-godot.log` e `artifacts/settings-custom-godot.log`. A engine ainda reporta erros ambientais de diretório `~/Library/Application Support/Godot` e certificados do macOS. Importação pelo editor também reportou socket proibido. Esses erros não são considerados validação gráfica limpa. **Janela, aparência dos recortes, cliques por coordenadas reais e FPS de renderização não foram validados**. O modo headless valida código, sinais e estado, sem produzir imagem da tela. O script Godot estrito continua rejeitando logs com esses erros.

## Medições de agentes

Release; mundo 240×96, seed 42; 200 ticks de aquecimento e 400 medidos (20 segundos simulados) por população. Câmaras inicialmente densas, colônias neutras, alimentação abundante. Não há renderer/GPU nessa medição. População final permaneceu igual à inicial em todos os casos.

| Agentes | Média ms/tick | P95 ms/tick | Working set MiB | Heap vivo MiB | Alocado nos 400 ticks MiB | Decisões |
|---:|---:|---:|---:|---:|---:|---:|
| 100 | 0.464 | 0.916 | 48.34 | 0.53 | 1.30 | 3840 |
| 500 | 0.368 | 0.603 | 55.03 | 1.03 | 2.47 | 19840 |
| 2,000 | 0.750 | 1.076 | 55.83 | 2.84 | 0.26 | 79840 |
| 10,000 | 1.974 | 2.260 | 62.06 | 9.18 | 0.84 | 80000 |

Fonte reproduzível: `sh scripts/benchmark-agents.sh`, JSON completo em `artifacts/agent-benchmarks.json`. Working set é do processo do benchmark, não do jogo completo. As populações compartilham o mesmo processo; JIT, caches e estado ecológico afetam comparações. Com 10.000 indivíduos o orçamento de 200 decisões/tick reduz a frequência média individual para cerca de 0,4 Hz. Idade, necessidades e execução de tarefas continuam atualizadas. As médias não devem ser interpretadas como garantia de 60 FPS ou aceleração 100× num computador com 4 GB.

## Regressão dos benchmarks de mundo

Release, mundo 512×128, cinco repetições por operação após aquecimento. Snapshot inicial: 20808 bytes; após 100 ticks: 47456 bytes. Dados em `artifacts/world-benchmarks-v3.json`.

| Operação | Operações por amostra | Média ms por amostra |
|---|---:|---:|
| world_metadata_and_deposit_plan | 1 | 2.699 |
| chunk_generation | 256 | 37.311 |
| logical_cell_queries | 100000 | 17.970 |
| orthogonal_neighbors | 100000 | 11.440 |
| resource_distribution_into_chunks | 256 | 27.344 |
| serialization | 1 | 0.419 |
| deserialization | 1 | 5.992 |

## Pendências explícitas

L2/L3 agregados e pooling de instâncias Ant não implementados. Orçamento de memória é estimativa determinística para admitir nascimentos, não teto de RAM do SO/GPU. Navegação/percepção têm limites espaciais. Não há planejamento completo de construção, física de estabilidade, cadáveres, recombinação sexual ou calibração científica. Velocidades e câmera são as únicas ações expostas na interface. Exportação de executáveis e inclusão do JSON em pacotes exportados precisam de validação futura.

Próximos passos recomendados: validar janela/recortes fora do sandbox; medir o jogo completo com renderer; adicionar navegação por regiões; só então implementar L2/L3 com testes de conservação; evoluir reprodução, estabilidade e cadáveres sem reciclar identidades observáveis.
