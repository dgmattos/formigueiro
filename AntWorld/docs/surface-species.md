# Espécie Superfície

A terceira espécie visual usa `src/sprites/003-formiga-superficie.png`, encontrado no workspace, com 1774 × 887 pixels RGBA. O build copia o original para `Assets/ants-surface.png`, sem alterar seus pixels. `AntSpriteAtlas` mantém limites próprios por linha/coluna para esse layout: a primeira coluna começa à esquerda dos atlas anteriores. `EntityView` escolhe a textura pela espécie e mantém desenho em lote, sem Node por formiga.

O preset agora inicia três colônias, totalizando 81 indivíduos: Normal (ID 0), Forte (ID 2) e Superfície (ID 3). O ID 1, Musgo, foi preservado para compatibilidade e continua disponível como configuração alternativa. As posições anteriores permanecem em 25% e 75% da largura; a Superfície inicia em 50%.

## Regras

- A rainha Superfície nasce **dois blocos abaixo da superfície**, configurável em `world.nests.surfaceDepth`. A câmara de crias permanece quatro blocos abaixo da rainha. A mesma regra vale na fundação de colônias-filhas, respeitando o piso do mundo.
- Os soldados têm **20% a mais de ataque e defesa** que os soldados normais: ataque-base **36** (antes 30) e defesa-base **21,6** (antes 18). Foram escolhidos esses valores iniciais porque o pedido não especificava a intensidade do reforço. A vida-base permanece 130; genes, habilidade e condição continuam influenciando o combate.
- Adultos da espécie recebem multiplicador de movimento **1,20** quando estão na célula aberta imediatamente acima do primeiro bloco sólido da coluna, exposto ao céu. O teste usa a geometria lógica atual, não a textura/grama ou a altitude original. Vale em solo, areia e rocha; cavernas, tetos e células sem apoio logo abaixo não recebem bônus. Colocar/remover blocos atualiza a elegibilidade pelo cache existente de superfície. Coordenadas negativas e a emenda horizontal usam a normalização circular do mundo.
- O multiplicador combina com saúde, energia, carga, skill, chuva e água existentes. Larvas e ovos continuam sem movimentação própria. O bônus não ignora colisões.
- A espécie pode escavar rocha e herda os coeficientes da Normal: 60% de redução de velocidade em rocha sólida e 60% de acréscimo no custo por ação. O bônus de movimento não é bônus de escavação.
- Rainhas/operárias não recebem os atributos militares dos soldados. O comportamento social/territorial existente determina hostilidade; não foi introduzida uma vencedora predeterminada.

O bônus fica em `SpeciesDefinition.SurfaceMovementMultiplier`; os atributos militares usam as definições de casta compartilhadas. Esses valores podem ser editados no catálogo C# em `src/AntWorld.Domain/Agents/SpeciesDefinition.cs`. Profundidade e população ficam no settings JSON.

## Persistência

Novos snapshots usam **v6** e incluem `NestSettings.SurfaceDepth` e `SpeciesDefinition.SurfaceMovementMultiplier`. O leitor aceita **v5**, atribuindo movimento 1,0 às espécies antigas e profundidade superficial padrão 2, sem inserir colônia ou espécie nova no save. Os atributos de casta já eram serializados. v1–v4 e versões futuras desconhecidas continuam rejeitadas explicitamente.

Os fixtures `tests/AntWorld.Persistence.Tests/Fixtures/format5-tick20.antworld` e `format5-tick80.antworld` foram produzidos pelo executável anterior, antes de alterar o codec. A comparação da continuação de 60 ticks valida que a leitura de v5 preserva a trajetória do cenário antigo. Carregar um save antigo não substitui suas colônias pelo novo preset; a terceira colônia aparece ao iniciar um mundo novo.

## Validação e limites

Build completo sem avisos/erros; **111 testes xUnit aprovados**, incluindo ninho/fundação, combate real, bônus por casta adulta, ausência do bônus sob teto/sem apoio, remoção de cobertura, coordenada circular negativa, imobilidade de larvas, persistência v6 e leitura/continuação de v5. Registro: `artifacts/surface-species-verification.log`.

O teste Godot headless valida os 90 recortes dos três atlas, transparência, preset e execução da cena (`artifacts/surface-species-game-console.log`). A inspeção manual da janela não foi executada; o ambiente mantém o erro de certificados do macOS documentado nas entregas anteriores. Não houve recalibração dos problemas de sobrevivência identificados no relatório de mortalidade. O sprite mantém o alfa e os contornos fornecidos no arquivo original.

Para iniciar, na pasta AntWorld: `sh scripts/run-game.sh`.

Atualização posterior: snapshots v7 mantêm leitura de v5/v6. As [regras de sobrevivência](survival-fix.md) foram corrigidas; a comparação de continuação histórica v5 descrita acima se refere à versão anterior a essa alteração biológica.
