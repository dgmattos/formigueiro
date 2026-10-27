# Funções de operárias e expansão do ninho

Cada operária adulta recebe uma função fixa, `Collector` ou `Digger`, com probabilidade de 50% para cada uma. O sorteio usa o PRNG persistido: a mesma seed e a mesma sequência de ações produzem as mesmas funções. A atribuição acontece no spawn adulto e na emergência da pupa; ovos/larvas não recebem uma profissão nem herdam a profissão da mãe. Rainhas e soldados não recebem funções de operária.

Coletoras buscam e transportam alimento. Cavadoras executam o plano de expansão. Ambas continuam atendendo necessidades próprias, perigos e transporte/alimentação das crias. A função é uma especialização da seleção de tarefas, sem duplicar o sistema de capacidades ou ações. As regras de força, velocidade e custo por espécie/material permanecem em `ExcavationRules`.

## Construção e mudança de andar

`ColonyExpansionSystem` calcula frentes de trabalho com acesso conectado ao ninho, usando coordenadas globais circulares:

1. Alarga a câmara inferior até nove células (quatro para cada lado do eixo).
2. Abre um poço de quatro células para baixo.
3. Abre a nova câmara inferior, também de nove células.
4. Quando o novo piso está aberto, a rainha está na câmara atual e todos os ovos foram entregues à câmara inferior atual, publica o novo andar. A antiga câmara de ovos passa a ser o destino da rainha; a nova câmara mais baixa passa a ser o destino dos ovos.

A abertura acontece exclusivamente por ações de escavação, com trabalho parcial, custo energético e experiência. O planejamento não remove blocos. A rainha anda até sua câmara e as operárias carregam os ovos; a mudança não teleporta indivíduos. Durante o transporte há um intervalo de transição até que todos ocupem suas novas posições. “Andar inferior” significa a câmara concluída e designada, não uma célula isolada do poço ainda em construção. A rainha fica quatro células acima dessa câmara após a mudança.

A expansão respeita o fundo do mundo e continua utilizando chunks, colisões e conexão circular. Câmaras iniciais/fundações ainda são abertas pelo mecanismo existente. Largura nove e distância quatro são parâmetros do código (`RoomHalfWidth` e `BroodChamberDepth`), não novos controles da interface. Não há simulação de desabamentos ou escolha de outras formas de câmara nesta etapa.

## Entulho

A remoção completa de cada bloco tem chance de **5%** de produzir uma unidade abstrata de entulho; o sorteio não se repete a cada golpe parcial. Nos outros 95%, o material removido desaparece, conforme a regra solicitada. A operária só inicia escavação com carga livre e capacidade para essa unidade.

O entulho ocupa a carga existente e reduz a velocidade por peso. A operária interrompe a escavação, percorre os túneis até a entrada original na superfície e deposita o material em um recurso esparso. Não é alimento e não cria trilhas de alimento. Pilhas na superfície não são recolhidas repetidamente. Se a transportadora morre, o material fica no local e outra cavadora próxima pode recuperá-lo. Não há transferência automática de minerais existentes: seus depósitos finitos permanecem nas células.

O catálogo registra o tipo extensível `excavation_spoil` com um ID livre, evitando conflito com recursos customizados de saves existentes. A carga e as pilhas são exibidas em cinza/marrom; pilhas são decorativas e não fecham a entrada do ninho.

## Metabolismo

- Ovos não gastam energia, inclusive durante o desenvolvimento. Continuam sujeitos a necessidades de água/nutrição, temperatura e dano.
- Larvas gastam **50% menos energia e nutrição que na versão anterior**: a taxa passou de 50% para 25% da taxa basal adulta. Hidratação não foi alterada.
- Pupas mantêm seu custo de desenvolvimento anterior.

## Persistência e diagnóstico

O formato **v8** persiste a função individual e o nível de expansão. Posição do ninho, transporte, tarefas, entulho e alterações físicas usam os campos e deltas existentes. A entrada deriva da superfície original; rotas e frentes de trabalho são reconstruídas. A navegação invalida caches por alteração da conectividade, sem refazer BFS a cada golpe que apenas reduz preenchimento sólido.

Saves v5/v6/v7 continuam legíveis. Operárias adultas antigas recebem funções por hash determinístico do ID, sem consumir o PRNG salvo; andares antigos começam com nível de expansão zero e mantêm a posição salva do ninho. A continuação segue as regras novas. Há fixture real v7 e comparação integral de snapshots após retomada.

O CLI informa coletores, cavadores, nível de expansão, profundidades e entulho na superfície. Amostras de diagnóstico incluem funções e câmara de crias. Os testes específicos estão em `WorkerExpansionTests.cs` e `WorkerExpansionPersistenceTests.cs`.

## Validação executada

- .NET SDK 10.0.201 e Godot 4.6.3 .NET. Build da solução: zero avisos e erros. **141 testes xUnit aprovados** (`artifacts/worker-expansion-validation.log`).
- Testes incluem funções determinísticas, emergência de adultos, escolha por função, escavação lateral/inferior, mudança física da rainha e ovos, caverna desconectada, chance por bloco concluído, conservação/recuperação de entulho, navegação circular entre chunks, fundo do mapa, metabolismo e migração/continuação de snapshots.
- Preset real das três espécies, seed 12345: 12000 ticks / dez minutos simulados, 84 indivíduos, 0 mortes. Tempo do ensaio CLI: 23.71 s. Resultado: `artifacts/worker-expansion-run.json`; snapshot: `artifacts/worker-expansion.antworld`. Essa medição curta não garante sobrevivência por um dia inteiro ou desempenho gráfico.
- Godot headless: verificações de cena, UI, sprites, SQLite/restauração, tiles e streaming circular aprovadas (`artifacts/worker-godot-smoke.log`). O ambiente ainda registra o erro de certificados macOS `get_system_ca_certificates`, já existente; não foi feita inspeção gráfica interativa.

| Colônia | Andares adicionais | Coletoras | Cavadoras | Entulho na superfície |
|---|---:|---:|---:|---:|
| Normal/Brasa | 14 | 12 | 9 | 4 |
| Forte | 3 | 11 | 10 | 0 |
| Superfície | 15 | 10 | 11 | 5 |

## Arquivos principais

- Domain: `Ant`, `WorkerRole`, `Colony`, `ExcavationSpoil` e `WorldMap`.
- Simulation: `AntFactory`, `AntBiologySystem`, `DecisionSystem`, `ColonyExpansionSystem`, `DroppedSpoil`, `DigAction`, `CollectAction`, `DepositAction`, `MovementSystem`, `NestNavigation` e `SimulationEngine`.
- Infrastructure: codecs de agentes/snapshot, validador e amostras de diagnóstico.
- Game: `EntityView` distingue visualmente entulho de alimento; CLI expõe contagens e profundidades.
- Testes novos: `WorkerExpansionTests`, `WorkerExpansionPersistenceTests` e fixture real `format7-tick20.antworld`. Testes existentes de coleta/histerese agora indicam explicitamente a função coletora.
