# Ninho, calendário sazonal e cogumelos

## Comportamento implementado

A rainha põe ovos na câmara superior, em `Colony.Nest`. O ninho inicial e os ninhos fundados possuem uma passagem e uma segunda câmara quatro blocos abaixo. A Utility AI das operárias considera a necessidade de levar os ovos à câmara inferior. A coleta da cria exige proximidade, reconhecimento da colônia, capacidade, energia e rota disponível; a posição acompanha a operária até a entrega. Ovos não se locomovem sozinhos. O vínculo transportador/cria é exclusivo, persiste no snapshot e é liberado na entrega, morte ou desenvolvimento até pupa. Larvas continuam podendo ser alimentadas e transportadas.

A câmara inferior pode usar o estoque compartilhado da colônia para alimentação assistida. Não há logística separada de abastecimento interno, dimensionamento automático da câmara ou simulação de espaço individual das crias. O destino precisa estar transitável e ter menos de 200 unidades de água livre. A navegação por BFS mantém o limite de 32.768 células por destino.

## Calendário e aparência

- Tick: 0,05 segundo simulado, independente do FPS.
- Dia: três horas simuladas = 10.800 segundos = 216.000 ticks.
- Mês: 28 dias; ano: quatro meses, 112 dias.
- Meses 1–4: primavera, verão, outono e inverno, respectivamente.
- A pausa interrompe calendário, biologia, movimentação e renovação de cogumelos. A câmera continua disponível.
- O HUD mostra dia do mês, mês, estação e ano. O calendário não reinicia as populações ou o terreno.

`SeasonTilesets` seleciona os quatro atlas fornecidos em `src/sprites`. Os chunks redesenham quando a estação muda; a renderização não modifica o estado lógico. Solo superficial com grama, árvore e cogumelos usam recortes da estação atual. A câmera e os quatro botões de tempo foram preservados.

Com os multiplicadores atuais do arquivo (1, 25 e 50), um dia demanda respectivamente três horas, 7 minutos e 12 segundos, ou 3 minutos e 36 segundos reais, se o hardware acompanhar. Nenhum coeficiente biológico por segundo foi recalibrado automaticamente. Desenvolvimento e longevidade definidos em dias agora usam a nova duração; o equilíbrio ecológico continua sendo um modelo de engenharia sem calibração biológica.

## Cogumelos

Dois recursos alimentares novos, `RedMushroom` (ID 5) e `BrownMushroom` (ID 6), representam os recortes indicados na linha 5, colunas 8 e 9. Uma instância lógica corresponde a uma ilustração de um tipo. Um bloco pode ter ambos, contando **dois** no limite do chunk. Recursos são armazenados na célula sólida de apoio, com o sprite e a posição de coleta imediatamente acima dela.

Habitat: `Soil` ou `SoilAndRock` não transitável, célula acima aberta e menos de 200 unidades de água livre em ambas. Ar, rocha pura e terra totalmente coberta não geram cogumelos. O gerador mantém transições parcialmente preenchidas em aproximadamente 25% das células de superfície e solo sólido nas demais, permitindo ocorrência superficial sem redefinir `AirAndSoil` como `Soil`.

| Tipo | Subterrâneo | Superfície | Bônus com vizinho |
|---|---:|---:|---:|
| Vermelho | 10% | 50% | +8 pontos percentuais |
| Marrom | 20% | 60% | +8 pontos percentuais |

As chances são por tipo e local elegível em cada tentativa. O bônus vale uma vez se qualquer dos quatro blocos ortogonais contiver cogumelo ativo, incluindo chunks vizinhos e a emenda circular; o resultado é limitado a 100%. O teto do chunk limita nascimentos mesmo quando o sorteio passa. O mínimo pode forçar nascimento quando os sorteios falham.

**Mínimo 2 e máximo 30 por chunk**, somando os dois tipos. O mínimo depende da existência de locais elegíveis: chunks só de ar/rocha permanecem sem cogumelos. A coleta pode reduzir temporariamente o total abaixo de 2; a próxima renovação repõe o mínimo. Um cogumelo parcialmente coletado continua contando como um e mantém o alimento restante, sem restauração automática até esgotar. Escavação, cobertura ou inundação do habitat remove o recurso vegetal afetado; não altera o orçamento dos depósitos geológicos.

As operárias detectam o alimento por coordenadas, caminham até o espaço acima do cogumelo e usam `WorldMap.Extract`. Cada instância fornece inicialmente quatro unidades alimentares. Carga, alimentação própria, cuidado de larvas e depósito aceitam os dois tipos. O depósito converte a quantidade em alimento da colônia na proporção 1:1; não duplica recursos. Fontes alimentares anteriores e recursos genéricos do catálogo foram preservados.

## Configuração editável

Em `src/AntWorld.Game/settings.json`, dentro de `world`:

```json
"mushrooms": {
  "undergroundRedChance": 0.1,
  "undergroundBrownChance": 0.2,
  "surfaceRedChance": 0.5,
  "surfaceBrownChance": 0.6,
  "adjacentBonus": 0.08,
  "renewalIntervalSeconds": 3600,
  "minimumPerChunk": 2,
  "maximumPerChunk": 30,
  "foodPerMushroom": 4
}
```

Reinicie o jogo para aplicar. Chances e bônus aceitam 0–1; intervalo, 1–86.400 segundos; mínimo não negativo, máximo entre mínimo e 8.192; alimento maior que zero e até 10.000. Limites são parâmetros ajustáveis, com os padrões solicitados 2/30. O calendário está centralizado nas constantes de `SimulationState`.

A renovação padrão tem uma tentativa por chunk por hora simulada. O primeiro ciclo começa após uma hora, distribuindo chunks ao longo da hora seguinte; depois mantém a mesma cadência por chunk. Assim os chunks não são todos processados no mesmo tick. Reduzir muito o intervalo em mundos grandes aumenta trabalho por tick. Não há dependência de câmera, FPS, SQLite ou chunks carregados para renovar.

## Algoritmo, memória e persistência

O plano inicial é calculado uma vez em ordem canônica de chunks, consultando o terreno lógico sem carregar arrays. Dentro de cada chunk, locais/tipos vazios são embaralhados por PRNG derivado de seed, coordenada e ciclo. Crescimento reutiliza um buffer de candidatos; não existe coleção por célula nem objeto por partícula. A ordem canônica permite que nascimentos influenciem vizinhos já avaliados, com viés sequencial conhecido, mas independe da ordem de carregamento dos chunks.

A varredura inicial e os ciclos completos têm custo proporcional às células do mundo. Apenas os recursos e alterações ficam no armazenamento esparso existente. A primeira geração de um mundo muito grande ainda é síncrona; não há promessa de latência constante de inicialização. A célula permanece com 16 bytes.

Snapshot **v4**, gerador **v3**: persistem configuração dos cogumelos, tick, deltas de recursos e transporte de crias. O próximo crescimento deriva do tick salvo; caches de navegação continuam reconstruíveis. Remoções são restauradas antes dos nascimentos para não exceder temporariamente o teto do chunk. Saves v1/v2/v3 são rejeitados com mensagem explícita; não há migração automática do calendário antigo. A estratégia continua seed + alterações, usando SQLite existente e sem Redis.

## Arquivos principais

| Área | Arquivos criados ou alterados |
|---|---|
| Domínio | `MushroomSettings`, `MushroomHabitat`, `ResourceIds`, `ResourceCatalog`, `WorldConfiguration`, `WorldMap`, `SimulationState`, `Colony`, `Ant` |
| Geração | `MushroomGrowth`, `ResourceDistributor`, `ProceduralChunkSource`, `TerrainGenerator`, `WorldGenerator` |
| Simulação | `MushroomSystem`, `FoodSources`, `SimulationFactory`, `SimulationEngine`, `BroodTransport`, `MovementSystem`, `NestNavigation`, `DecisionSystem`, `ColonyAiSystem`, `PopulationAdmission`, ações de transporte/coleta/depósito/alimentação |
| Persistência | `MushroomSettingsCodec`, `WorldSnapshotCodec`, `BinarySnapshotCodec`, `SnapshotValidator` |
| Godot | `settings.json`, `SeasonTilesets`, `TerrainChunkView`, `ObserverHud`, `Main`; atlas de verão/outono/inverno e arquivos de importação |
| Testes | `MushroomTests`, `NurseryCalendarTests`, `MushroomNurseryPersistenceTests`; ajustes de duração em `AgentAiTests` e de versão em `BlockPersistenceScenarios` |

## Validação e limites

Resultados e arquivos de log desta atualização são registrados em `validation-seasonal.md`. A execução headless verifica carregamento de atlas, configuração, limites iniciais, controles e persistência real. Não equivale a revisão gráfica em janela. Os fundos originais dos atlas permanecem, e não há shader de remoção de fundo, animação de crescimento ou influência sazonal adicional nas chances dos cogumelos.

Próximas melhorias: validar composição visual em janela, calibrar metabolismo/reprodução para dias de três horas e avaliar planejamento de recursos sob demanda para mundos grandes. Isso não impede executar a versão atual.
