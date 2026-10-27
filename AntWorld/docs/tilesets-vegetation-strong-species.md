# Tilesets, vegetação e espécie Forte

## Diagnóstico da implementação anterior

Os quatro atlas `001-planice-biome-primavera.png`, `002-planice-biome-verao.png`, `003-planice-biome-outono.png` e `004-planice-biome-inverno.png` medem **1536 × 1024**, sem canal alfa. A região superior esquerda contém terreno; a região direita contém objetos com tamanhos irregulares; a faixa inferior contém variações de terreno. Não existe uma grade uniforme válida para a imagem toda.

Antes desta correção:

- `TerrainChunkView.DrawSolid` comprimia recortes retangulares diretamente em células de 10 pixels e reduzia a altura conforme preenchimento, deformando a textura.
- Os recortes eram constantes espalhadas pelo renderer. Misturas rochosas eram representadas como uma faixa vertical de rocha.
- Grama dependia da superfície original, sem teste completo de luz, umidade ou cobertura atual.
- `DrawTree` apenas desenhava a árvore pequena em colunas `x % 13 == 5` da floresta. Não havia entidade vegetal, distribuição ecológica, troncos ou tocos.
- Objetos eram desenhados pelo dono do chunk, sem seleção pelo bounding box global da copa.
- Não havia `TileMapLayer`, `TileSetAtlasSource`, margens, separação ou origens configuradas. O mundo inicial usava chunks de 16 e uma constante gráfica de 10 pixels.
- O novo atlas da formiga Forte estava disponível em `src/sprites/002-formiga-forte.png`, 1774 × 887 com alfa, quando foi integrado.

## Manifesto e preparação dos assets

`src/AntWorld.Game/Assets/sprites.manifest.json` é o manifesto fonte, versão 1. Registra cada recorte medido, categoria, tipo visual/material, condição de vizinhança, variante, peso, origem, dimensão visual, tratamento do fundo e ajustes sazonais de âncora. Coordenadas ficam centralizadas nesse arquivo. O catálogo C# usa enums para terreno e vegetação.

`AssetPreparation` usa `Godot.Image` para recortar, remover o fundo escuro dos objetos, isolar componentes quando recortes grandes incluem outro objeto e normalizar com **nearest-neighbor**. Os PNG originais não são modificados. Tiles de terreno usam recortes quadrados previamente escolhidos; objetos preservam a proporção e são escalados uniformemente para seu envelope visual. Não há interpolação bilinear nem esticamento de árvores para preencher quadrados.

O fundo dos objetos é identificado por limite de intensidade e baixa diferença entre canais, editáveis no manifesto (`backgroundMaximum`, `backgroundChroma`). Árvores e madeira podem selecionar o maior componente conectado para excluir fragmentos de objetos próximos. Flores e cogumelos preservam componentes separados. O ponto inferior de apoio é ajustado aos pixels visíveis na faixa central do objeto já normalizado; `snapAnchorToBase: false` permite usar somente a âncora manual. Variantes sazonais têm offsets próprios quando necessário.

Saída: `Assets/Prepared/terrain-{season}.png`, `objects-{season}.png` e `sprites.generated.json`, com regiões e âncoras normalizadas. A preparação falha explicitamente se o recorte sair da imagem, ficar vazio ou exceder o atlas preparado. Alterações no layout exigem revisar o manifesto. A identificação semântica dos sprites não é automática: o limiar de fundo pode exigir ajuste manual em artes futuras com sombras quase pretas ou objetos encostados.

`sh scripts/run-game.sh` compila, sincroniza os atlas fonte, prepara, importa e executa o jogo. `verify-godot.sh` executa a mesma preparação antes dos testes. Para usar F5 no editor após alterar os atlas, execute a etapa de preparação/importação pelo script primeiro. Os arquivos preparados atuais acompanham o projeto.

## Dimensões, terreno e autotiling

O preset em `settings.json` agora usa `rendering.tileSize: 16` e `world.chunkSize: 32`, ou **512 × 512 pixels por chunk visual**. O renderer lê a configuração; não presume que todos os saves ou testes tenham chunks de 32. A configuração padrão do núcleo permanece compatível com seus cenários de 16 células por chunk. A dimensão gráfica aceita múltiplos inteiros de 16 até 64.

Cada chunk visível usa um `TileMapLayer`, sem Node por tile. `TileSet.TileSize` e `TileSetAtlasSource.TextureRegionSize` são 16 × 16; margens e separação são zero no atlas normalizado; `TextureOrigin` é zero e padding está habilitado. Escalas maiores são inteiras. O posicionamento da layer é compensado pelo centro retornado por `MapToLocal`, evitando deslocamento de meia célula. O teste integrado confere esse alinhamento.

Células parcialmente preenchidas usam 16 variantes discretas de altura: a textura é recortada e alinhada ao fundo da célula, sem compressão vertical. Colisão e navegação continuam nas células do domínio; o TileMap não cria uma segunda simulação física. Máscaras de bordas são calculadas por vizinhos, inclusive entre chunks e na emenda circular.

`TerrainAppearanceResolver` escolhe solo, grama, bordas esquerda/direita, canto, areia superficial/interior, transição solo/areia e misturas de rocha conforme composição. Variações são determinísticas e os pesos vêm do manifesto. Inclinações foram identificadas e catalogadas, mas **não são escolhidas**: o mundo físico atual possui degraus, não rampas. Não há inclinações sorteadas que contradigam as colisões.

Grama exige solo compatível, umidade mínima, ausência de inundação, bioma favorável e acesso ao céu. A primeira célula sólida por coluna é cacheada e invalidada por mudanças físicas, permitindo recalcular cobertura após escavação. `CellFlags.VegetationRemoved` suprime cobertura explicitamente. Umidade e bioma afetam cobertura; noite não elimina a grama. Terra coberta não recebe grama ou neve superficial. Areia tem tiles separados para superfície e interior, evitando faixas repetidas. Materiais subterrâneos usam variantes neutras sazonais para não cobrir cavernas de neve.

## Vegetação e madeira

`VegetationInstance` é um valor lógico com ID estável, espécie vegetal derivada da definição, base, bioma, saúde, nascimento, idade inicial e remoção. `AgeAt(tick)` acumula idade sem alterar todas as entidades a cada tick. A definição separa envelope visual, largura de apoio e colisão; o crescimento é identificado como jovem, maduro ou remanescente. Há árvore grande, pequena e jovem; tronco caído, toco e madeira morta; grama, grama alta, arbusto, arbusto de frutos, flores e plantas pequenas.

A geração usa seed/coordenadas, perfil do bioma, umidade, temperatura, fertilidade, apoio e espaço livre. Candidatos têm prioridade determinística global, com distância mínima entre árvores calculada na topologia circular. Flores usam um campo periódico correlacionado para formar agrupamentos. A ordem de carregamento de chunks não altera o plano. Troncos exigem superfície plana sob toda a largura de apoio e espaço aberto acima. Há uma planta/objeto por posição de base; cogumelos continuam sendo recursos independentes e podem coexistir, conforme a regra anterior dos dois tipos por bloco.

`SurfaceObjectsView` desenha vegetação e madeira em lote, selecionando entidades pelo envelope visual que intercepta a câmera. A copa não é recortada na borda do chunk proprietário. A emenda pode desenhar uma cópia visual deslocada, mantendo uma única identidade lógica. Estações trocam somente a referência do sprite; não recriam o plano de vegetação. Posição e âncora inferiores são consistentes entre variantes.

Dano é uma operação real: árvores com saúde esgotada viram tocos com o mesmo ID; outras entidades são removidas. Remover/inundar o apoio ou preencher seu espaço livre invalida a entidade, grava delta e atualiza a renderização. `woodBlocksMovement` habilita uma área de colisão lógica ao nível do chão para madeira; o padrão é `false`. A navegação invalida o cache quando esses obstáculos mudam. Madeira ainda não é extraída como recurso orgânico automaticamente; árvores não têm crescimento, disseminação, queda animada ou mortalidade climática contínua nesta versão. Árvore jovem reutiliza a arte pequena em escala uniforme, e madeira morta reutiliza a arte do tronco; sprites próprios podem substituir essas variantes no manifesto.

As layers existentes foram adaptadas: fundo do mundo e das cavernas, `TerrainLayer`, `TerrainOverlayLayer`, `WaterLayer`, `VegetationLayer`, `ObjectLayer` e `AgentLayer`. Efeitos já existentes continuam no desenho de agentes. Câmera, pausa, velocidade e limite de indivíduos renderizados foram preservados.

## Espécie Forte, escavação e profundidade

A nova espécie usa **ID 2**, preservando os IDs 0 (Normal/Brasa) e 1 (Musgo). O preset gráfico inicia duas colônias: Normal/Brasa e Forte, com 54 indivíduos no total. Musgo continua disponível na configuração. `ants-strong.png` é copiado automaticamente do PNG fornecido, sem alterar seu alfa; os recortes incluem a coroa mais alta. O atlas de superfície adicional não foi integrado, pois não foi solicitado.

A comparação de trabalho de escavação pressupõe mesma genética, skill e umidade:

| Material | Normal | Forte |
|---|---:|---:|
| Terra comum | 1,00× | 1,25× da Normal |
| Mistura solo/rocha | 1,00× | 1,40× da Normal |
| Rocha sólida | 0,40× da própria velocidade na terra | 0,80× da própria velocidade na terra |
| Energia por ação em rocha sólida | 1,60× do custo na terra | 1,20× do custo na terra |

Ambas podem escavar rocha. Logo, na mesma condição de referência, Forte em rocha sólida equivale a 1,00× da Normal em terra (1,25 × 0,80), contra 0,40× da Normal em rocha. O prejuízo energético foi interpretado como **aumento do custo por ação**, não redução. O custo base é 0,4 unidade por ação; em rocha fica 0,64 para Normal e 0,48 para Forte. Como a escavação leva mais ações em rocha, o custo total por bloco também reflete o tempo adicional. O custo é calculado antes da remoção, inclusive na última ação que transforma o bloco em ar.

Genes, skill e umidade ainda modificam o trabalho real. O preenchimento do bloco é um byte, portanto o trabalho aplicado é quantizado em unidades inteiras; os multiplicadores são exatos no cálculo contínuo, sem promessa de razão exata no número inteiro de ações para cada bloco.

`world.nests` no settings:

```json
"nests": {
  "normalBaseDepth": 4,
  "normalDepthMultiplier": 1.25,
  "strongUndergroundRatio": 0.75
}
```

O ninho normal passa de 4 para **5 blocos abaixo da superfície**. A rainha Forte fica a **75% da profundidade subterrânea disponível**, com limite que reserva espaço para a câmara de crias quatro blocos abaixo e um piso. No perfil geológico atual, 75% já fica na camada rochosa. A passagem até a superfície e as câmaras são criadas na fundação; a abertura inicial continua instantânea. Colônias-filhas preservam a regra de profundidade da espécie.

## Persistência e arquivos

Snapshot **v5** inclui configuração e deltas de vegetação, profundidade dos ninhos, os novos coeficientes de espécie e o catálogo já existente de agentes/recursos. O gerador físico permanece v3: a geometria base não foi reescrita. Vegetação base é reconstruída da seed; alterações de saúde/tipo/remoção são esparsas. Não há consultas SQLite durante ticks. Formatos v1–v4 são rejeitados explicitamente, sem reinterpretar o payload; não há migração automática.

Arquivos principais:

- Domínio: `VegetationKind`, `VegetationDefinition`, `VegetationInstance`, `VegetationStore`, `VegetationSettings`, `BiomeVegetationSettings`, `NestSettings`, `WorldMap`, `WorldConfiguration`, `CellFlags`, `SpeciesDefinition`, `SimulationState`.
- Geração: `VegetationGenerator`, `TerrainVisualType`, `TerrainAppearance`, `TerrainAppearanceResolver`, `WorldGenerator`.
- Simulação: `ExcavationRules`, `DigAction`, `AntAction`, `SimulationFactory`, `ColonySystem`, `NestNavigation`.
- Persistência: `VegetationSnapshotCodec`, `WorldSnapshotCodec`, `AiConfigurationCodec`, `BinarySnapshotCodec`.
- Godot: manifesto e `Assets/Prepared`, `AssetPreparation`, `PreparedSpriteCatalog`, `SpriteManifest`, `SpriteDefinition`, `PreparedSprite`, `VisualPreviewExporter`, `RenderingSettings`, `TerrainChunkView`, `SurfaceObjectsView`, `WorldView`, `EntityView`, `AntSpriteAtlas`, `Main`, `settings.json` e o target de sincronização do projeto.
- Testes: `VegetationVisualRulesTests`, `StrongSpeciesTests`, `VegetationPersistenceTests` e ajustes das expectativas anteriores de rocha/versão.

## Validação

Build completo: **zero erros e zero avisos**. Motor xUnit real em processo: **96 testes aprovados**, zero falhas/ignorados/erros. Registro: `artifacts/tileset-strong-verification.log`. Os cenários cobrem geração independente da ordem de chunks, distância circular, apoio, remoção, tocos, colisão opcional, grama/luz/umidade/inundação, vizinhos na emenda, identidade sazonal, custos e coeficientes de escavação, ninhos profundos e retomada determinística.

Godot headless: `artifacts/tilemap-game-console.log` confere transparência dos dois atlas de formigas, objetos preparados, controle de tempo, SQLite, criação de TileMapLayers e alinhamento da grid. O preset produziu árvores grandes, pequenas e jovens, três troncos caídos, dois tocos e plantas. Prévias CPU das quatro estações ficam em `artifacts/tileset-smoke/world-*.png`; usam os mesmos recortes, seleção de materiais e âncoras, mas não são capturas de uma janela Godot.

O ambiente ainda registra erro de certificados do macOS e, no editor, restrição de socket. Não foi validada interação gráfica manual nem FPS da janela. O script estrito `verify-godot.sh` continua rejeitando erros ambientais; o PASS da cena não os oculta. A preparação de imagens é síncrona e executada fora dos ticks; o plano vegetal depende da largura do mundo, enquanto recursos/terreno mantêm os limites já existentes.

## Próximas melhorias

Conferir a janela nas resoluções e níveis de zoom desejados; medir FPS e memória com vegetação e agentes juntos; fornecer artes próprias para árvores jovens e madeira morta; implementar crescimento e aproveitamento orgânico da madeira quando essas regras ecológicas forem definidas. Rampas exigem geometria física explícita antes de ativar os sprites de inclinação.
