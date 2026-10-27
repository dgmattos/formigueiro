# Atlas e manifesto

Os quatro arquivos `grassland-{season}.png` são cópias integrais dos atlas sazonais em `src/sprites`, com 1536 × 1024 pixels e sem alfa. Os originais são preservados e sincronizados no build. HTMLs auxiliares não são executados nem incluídos no jogo.

`sprites.manifest.json` versão 1 centraliza recortes medidos, tipos visuais, materiais, variantes, pesos, dimensões, âncoras e ajustes por estação. Terreno e objetos usam regiões diferentes: não há uma grade uniforme sobre o atlas original. `AssetPreparation` recorta e normaliza com nearest-neighbor; os objetos e a cobertura de grama recebem remoção configurável do fundo escuro. Componentes conectados removem fragmentos de objetos vizinhos onde configurado. O fundo dos interiores minerais é preservado.

`Prepared/terrain-{season}.png`, `objects-{season}.png` e `sprites.generated.json` são a saída reproduzível. `PreparedSpriteCatalog` cria TileSet/TileSetAtlasSource de 16 pixels; `TerrainChunkView` usa TileMapLayer. Objetos são desenhados em lote por `SurfaceObjectsView`, com âncora inferior e seleção pelo bounding box da câmera. Materiais subterrâneos usam variantes neutras, evitando neve repetida sob o solo no inverno.

Os cogumelos indicados na quinta linha, colunas 8 e 9, estão no manifesto como `mushroom_red` e `mushroom_brown`. Cada instância lógica corresponde à ilustração desse tipo, mesmo que contenha vários chapéus. Ambos podem aparecer sobre o mesmo bloco; cada tipo usa metade da largura. Continuam recursos alimentares esparsos, sem Node por cogumelo.

`time-controls.png` preserva o atlas `src/sprites/player-time-controle.png`. `ObserverHud` seleciona pausa, play e x1/x2/x3; quatro controles aparecem simultaneamente. Multiplicadores vêm de `../settings.json`.

`ants.png` e `ants-strong.png` são sincronizados de `001-formiga-normal.png` e `002-formiga-forte.png` a cada build. Ambos têm 1774 × 887 pixels com alfa. Seus pixels não são modificados. `AntSpriteAtlas` centraliza cinco linhas (rainha, soldado, operária, larva e ovo) e seis frames, incluindo a coroa da Forte. `EntityView` usa um único Node de desenho, com animação baseada em ticks simulados. Pupa reutiliza larva e macho reutiliza operária enquanto faltarem assets próprios.

Execute `sh scripts/run-game.sh` na raiz AntWorld para compilar, preparar, importar e abrir. Depois de alterar layouts, revise o manifesto ou os recortes de `AntSpriteAtlas`. Para usar F5 após mudanças nos atlas, prepare/importe primeiro pelo script. O teste `-- --smoke-test` verifica alfa dos dois atlas de formigas, recortes preparados, TileMapLayers e alinhamento; não substitui inspeção de uma janela real.

Diagnóstico, arquivos, decisões e limitações estão em `docs/tilesets-vegetation-strong-species.md`. Preparação não infere automaticamente o significado de novos sprites; artes futuras podem exigir ajuste manual de recorte/fundo/âncora. Os arquivos fonte não informam licença comercial.
