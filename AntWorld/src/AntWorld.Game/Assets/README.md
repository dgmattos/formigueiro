# Atlas do terreno

`grassland-spring.png` é uma cópia integral do atlas `src/sprites/001-planice-biome-primavera.png`, encontrado no workspace durante a implementação. O arquivo de origem não foi alterado. `TerrainChunkView` seleciona recortes de solo, areia e rocha diretamente da textura, sem gerar imagens intermediárias.

A origem/licença comercial não foi informada. Os arquivos HTML auxiliares encontrados na pasta de origem não são executados nem incluídos no projeto Godot.

`grassland-summer.png`, `grassland-autumn.png` e `grassland-winter.png` são cópias integrais dos arquivos `002-planice-biome-verao.png`, `003-planice-biome-outono.png` e `004-planice-biome-inverno.png` em `src/sprites`. `SeasonTilesets.ForSeason` escolhe o atlas do mês, sem alterar o estado físico. Todos têm 1536 × 1024 pixels. Bordas são calculadas por vizinhança e desenhadas por `TerrainChunkView`; árvores usam a região (1310,48,225,292).

Os dois cogumelos indicados na quinta linha, colunas 8 e 9, usam recortes (910,637,110,78) e (1035,637,101,78). Cada instância lógica corresponde a uma ilustração desse tipo, mesmo que a arte contenha mais de um chapéu. Ambos podem aparecer sobre o mesmo bloco; cada tipo ocupa metade da largura. São recursos alimentares esparsos, desenhados pelo chunk sem Nodes individuais. O fundo original das imagens foi preservado.


`time-controls.png` é uma cópia preservando o atlas fornecido em `src/sprites/player-time-controle.png`. `ObserverHud` seleciona regiões de pausa, play e x1/x2/x3; só quatro controles aparecem simultaneamente. Multiplicadores vêm de `../settings.json`.

`ants.png` é sincronizado de `src/sprites/001-formiga-normal.png` pelo target `SyncAntSpriteAtlas` do projeto C# a cada build. O PNG atualizado tem canal alpha e layout sem rótulos/grade; a cópia antiga sem transparência causava caixas escuras em volta dos indivíduos. Não há remoção de fundo em runtime nem modificação dos pixels fornecidos.

`AntSpriteAtlas` centraliza os recortes do layout atual (1774 × 887), com cinco linhas de rainha, soldado, operária, larva e ovo e seis frames. `EntityView` usa um único Node de desenho; a animação acompanha ticks simulados. Pupa reutiliza larva e macho reutiliza operária até existirem assets próprios. Ao mudar novamente o layout, atualize os limites das linhas/colunas em `AntSpriteAtlas`. Alterações apenas na arte são sincronizadas pelo build; execute `sh scripts/run-game.sh` para compilar, importar e abrir o jogo.

O teste headless `-- --smoke-test` valida o canal alpha da textura importada e que todos os 30 recortes contêm pixels visíveis e transparentes. A arte original pode conter pixels semitransparentes nas bordas; estes são preservados. Essa verificação não substitui a revisão visual em janela.
