# Blocos físicos, recursos e chunks — gerador v3 / snapshot v4

## Escopo e compatibilidade

A inspeção encontrou `WorldMap` com arrays globais, `WorldChunk` como região de renderização e bordas horizontais fechadas. Não havia streaming lógico nem `WorldCell`. A atualização mantém os projetos, o motor de simulação, o adaptador SQLite, a cena e os comandos da câmera; introduz armazenamento por chunks e topologia cilíndrica. O algoritmo de relevo foi adaptado para ruído periódico e amostragem por coordenada, pois uma sequência aleatória global dependia da ordem de geração.

Os quatro atlas fornecidos são selecionados por `SeasonTilesets` conforme o mês/estação. Nenhum sprite original foi alterado. Veja [calendário, cogumelos e ninho](season-nursery-mushrooms.md).

## Dados de uma célula

`WorldCell` é um `readonly record struct` de **16 bytes**, sem referências gerenciadas, com layout sequencial compacto. Cada `WorldChunk` mantém um `WorldCell[]` contíguo, exposto como memória somente para leitura.

| Propriedade | Armazenamento | Unidade/significado |
|---|---|---|
| SolidFill | byte | Ocupação sólida 0–255 |
| RockFraction | byte | Proporção de rocha **dentro da parte sólida**, 0–255 |
| SoilMaterial / RockMaterial | ushort + ushort | Identificadores no catálogo de materiais |
| Moisture | byte | Umidade retida 0–100%; valores acima de 100 são rejeitados |
| WaterAmount | ushort | 0–65.535 unidades lógicas de água livre |
| Temperature | short | Décimos de grau Celsius |
| Porosity / Permeability | byte + byte | Coeficientes normalizados 0–255 |
| Nutrients | ushort | Concentração ambiental de nutrientes, em unidades lógicas |
| Flags | byte | Modified, Excavated e EnvironmentChanged |

`BlockType` é calculado, sem ocupar outro byte: preenchimento zero → Air; sem rocha e preenchimento parcial → AirAndSoil; solo cheio → Soil; fração rochosa intermediária → SoilAndRock; fração rochosa 255 → Rock. Assim não pode divergir da composição. Um bloco rochoso parcialmente preenchido continua Rock, com o preenchimento independente. Um tipo misto não implica 50/50.

`MaterialCatalog` separa terra, areia, argila, granito e a definição de gelo (reservada para evolução futura). Novos materiais não exigem novos valores de BlockType nem outro campo em WorldCell. O catálogo de materiais é código versionado; não há editor de materiais em runtime nesta etapa.

## Água, umidade e ambiente

A capacidade de retenção é:

`1000 × preenchimento normalizado × porosidade normalizada × afinidade ponderada dos materiais`.

Água retida = capacidade × Moisture/100. Solo tem maior afinidade, misturas têm retenção proporcional e granito baixa afinidade; areia e argila têm propriedades distintas. Ar mantém umidade de material zero e pode conter água livre. `AddWater` e `AbsorbWater` permitem acumular e transferir água explicitamente, respeitando retenção, saturação e permeabilidade. Umidade tem precisão de um ponto percentual; a transferência arredonda unidades de água para cima, com perda de quantização inferior a uma unidade por chamada. Isso não é um modelo hidráulico conservativo completo.

O clima continua atualizando seu estado por coluna e atualiza também a umidade e temperatura da célula superficial. O subterrâneo conserva condições geradas até ser alterado explicitamente. Bolsões de água em cavernas são máscaras espaciais correlacionadas; pântanos têm água superficial acumulada. Não há fluxo entre células, pressão, difusão térmica, lençol freático dinâmico ou congelamento. As APIs e os campos permitem acrescentá-los sem trocar o layout básico.

## Geração determinística

A distribuição de biomas preserva as cinco faixas embaralhadas por seed. Relevo usa harmônicos inteiros de uma circunferência, fechando suavemente a emenda horizontal. Cavernas usam campos periódicos correlacionados. Variações locais usam hash de `(seed, x, y, stream)`; não dependem da quantidade ou ordem de chunks carregados.

Profundidade = `(y - superfície[x]) / (altura - superfície[x])`. `GeologySettings` configura nível superficial, espessura relativa de solo, início da rocha, início/limiar de cavernas, profundidade de matéria orgânica/minerais, espaçamento, passos e orçamento dos veios. A planície aprofunda o solo; deserto usa areia seca; pântano adiciona água/matéria orgânica; tundra inicia fria. Misturas e porosidade variam continuamente por profundidade/campos espaciais.

```csharp
var configuration = new WorldConfiguration(42, 512, 128, 16)
{
    MaximumLoadedChunks = 8,
    Geology = new GeologySettings(SoilDepth: .22, RockDepth: .55,
        MineralDepth: .6, VeinSteps: 96, VeinBudget: 1400)
};
var world = new WorldGenerator().Generate(configuration);
```

Preparar o mundo cria metadados por coluna e um plano esparso global de recursos. **Não cria os arrays de células**. O plano de depósitos é limitado pela largura, espaçamento e passos; o plano de cogumelos varre o terreno lógico uma vez e retém apenas recursos esparsos limitados por chunk. Isso impede que veios sejam truncados ou sorteados novamente a cada chunk. Alimentos superficiais e fontes em cavernas continuam disponíveis.

Veios de ferro e depósitos orgânicos usam caminhadas aleatórias ortogonais condicionadas ao substrato. Identificadores, origens e concentrações são determinísticos. Cada caminhada mantém células conectadas, cruza bordas de chunks e normaliza X na emenda. Uma origem geológica da primeira faixa fica em X=0 para incluir a conexão cilíndrica na distribuição; as demais faixas recebem deslocamento pela seed. Por isso a distribuição não é estatisticamente invariante sob qualquer rotação horizontal.

O orçamento inicial é finito e distribuído entre células sem excedê-lo. Concentrações variam de 25% a 100%. Depósitos diferentes do mesmo recurso não se sobrepõem. `DepositId`, origem, orçamento e alocação descrevem o agrupamento; quantidade restante deriva dos estoques atuais. Ferro e depósitos orgânicos finitos não regeneram. Fontes renováveis seguem o catálogo e condições de ocorrência. Não há escolha de colônia favorecida pela geração.

## Recursos esparsos

`ResourceCatalog` registra identificador ushort, nome, categoria, extração, regeneração e condições (máscara física, faixa de profundidade e umidade mínima). `ResourceStack` armazena quantidade, capacidade, concentração em pontos-base e DepositId. Um chunk usa **uma** tabela `Dictionary<ResourceKey, ResourceStack>`, indexada por `(índice local, recurso)`. Células vazias não recebem coleção.

`world.SetResource` permite associar novos recursos registrados; `world.Extract` faz extração parcial e nunca retorna mais do que existe. A identidade e a capacidade original de depósitos são protegidas contra expansão. Fontes de alimento usam esse mesmo armazenamento. As propriedades legadas `Terrain`, `Food` e `FoodCapacity` são fachadas indexadas, sem uma segunda cópia densa; preservam os consumidores existentes. `Array.Clear` foi substituído por `ClearFood` nos testes.

```csharp
world.ResourceCatalog.Register(new(99, "Semente", "seed", true, .01,
    new ResourceOccurrence(31)));
world.SetResource(new CellPosition(-1, 20), 99, new ResourceStack(8, 10, 2500));
var extracted = world.Extract(new CellPosition(-1, 20), 99, 2);
```

## Vizinhos e streaming

`ChunkManager.GetNeighbors` escreve num `Span<CellPosition>` do chamador: esquerda, direita, acima, abaixo; opcionalmente quatro diagonais. Não cria arrays por consulta nem referências permanentes entre células. X usa módulo positivo de largura, inclusive para int.MinValue; Y é limitado ao mundo. Chunks parciais na última coluna/linha continuam válidos.

`GetCell(position, load: false)` combina seed e alterações sem carregar arrays. Portanto vizinhos descarregados continuam consultáveis, sem disparar geração recursiva. `ChunkManager.Load` materializa apenas a região solicitada e aplica suas alterações. O cache é limitado por `MaximumLoadedChunks`, com descarte por recência de carregamento; alterações ficam no diário lógico e sobrevivem ao descarte. Não há consultas SQLite durante ticks.

O renderer cria/remove `TerrainChunkView` para os chunks intersectados pelo viewport, com margem de um chunk. A câmera e seus comandos permanecem os mesmos. A conectividade circular é lógica: o renderer não desenha cópias infinitas do mundo além dos limites da câmera.

Máscaras ortogonais de exposição produzem bordas e superfícies, recortes seguem material/preenchimento e mistura de rocha, água tem overlay e ferro tem marcadores. Escavar a borda invalida também chunks adjacentes, inclusive na emenda. Consultas visuais não modificam o modelo. A aparência acompanha os atlas de primavera, verão, outono e inverno.

## Persistência

Snapshot **v4** inclui versão do gerador (3), seed/configurações geológica e de cogumelos, extensões do catálogo, alterações de clima por coluna, células modificadas, estoques modificados e agentes/colônias. Tipos físicos são reconstruídos das proporções salvas. Feromônios são serializados apenas onde não são zero. Cabeçalho, limite de tamanho e SHA-256 permanecem.

Arrays base, depósitos base e chunks carregados não são gravados. Ao carregar, reconstrói-se o mesmo plano e reaplicam-se os deltas; nenhum chunk precisa ser materializado. Uma extração não desaparece após unload/load ou save/load. Catálogos adicionais também são restaurados.

Saves v1/v2/v3 e geradores desconhecidos são rejeitados explicitamente. Não há migração aproximada que invente propriedades. Os arquivos antigos são preservados pelo carregamento. SQLite mantém schema 1 e faz upsert transacional do BLOB: os deltas são acumulados relativamente à seed; **não há journal incremental entre saves nem atualização SQL individual por chunk**.

## Testes e benchmarks

`sh scripts/verify-full.sh` executa xUnit real em processo. `sh scripts/verify-core.sh` executa os cenários compartilhados e um CLI. `sh scripts/benchmark-world.sh` mede planejamento, geração de chunks, consulta de células/vizinhos, distribuição de recursos, serialização e desserialização em Release, com aquecimento e cinco repetições. Não depende de BenchmarkDotNet nem de serviços.

| Requisito obrigatório | Cenário |
|---|---|
| 1–2: cinco tipos/composição | PhysicalTypes |
| 3–6: umidade, Air, água livre, retenção | MoistureAndWater + BiomesAndDepth |
| 7–9: vizinhos internos, entre chunks e circulares | Neighbors |
| 10: recursos determinísticos | GenerationOrder |
| 11–12: continuidade de veios e emenda | Deposits (conectividade verificada por BFS) |
| 13–14: orçamento e extração parcial | Deposits + ExtractionAndCatalog |
| 15–16: persistência e recuperação | ModifiedCellsAndResources + SnapshotScenarios + SQLite real |
| 17: ordem de chunks | GenerationOrder |
| 18: biomas | BiomesAndDepth |
| Adicionais | NoPerCellAllocation, UnloadedQueries, AutotileBoundaries, SeedBasedSizeAndVersion |

Consulte `artifacts/world-benchmarks.json` e [validação](validation.md) para os resultados medidos. São microbenchmarks locais, sujeitos a JIT, carga e hardware; não garantem FPS.

## Limitações e próximos passos

- Modelo de água quantizado, sem hidráulica completa; próxima etapa pode acrescentar fluxo conservativo por faces.
- Plano base de recursos e diário de alterações ficam em memória; mundos muito alterados podem precisar de armazenamento de deltas por chunk no SQLite.
- Feromônios e BFS são esparsos. A navegação de cada destino de ninho tem limite de 32.768 células; mundos grandes e muito alterados exigem medir memória e tempo de planejamento.
- Biomas ainda são faixas; interfaces de bioma podem ter transições abruptas de material/umidade. O relevo e as consultas fecham o cilindro.
- Geração/cache são síncronos na thread dona do estado. Trabalho em background requer entrega de chunks sem concorrência sobre mutações.
- Os quatro atlas estão integrados. Transições artísticas mais elaboradas ficam para depois. Renderização em janela não foi revisada neste sandbox; o teste headless valida integração, não qualidade visual.
