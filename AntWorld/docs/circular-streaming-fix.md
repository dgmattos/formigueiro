# Correção do terreno ausente nas laterais

## Diagnóstico

`WorldView.StreamVisibleTerrain` limitava as colunas gráficas ao intervalo de chunks canônicos do mundo. A câmera podia ultrapassar esse intervalo. `SurfaceObjectsView` já desenhava árvores e arbustos deslocados uma largura de mundo para cada lado, resultando em vegetação suspensa e ausência de terreno na região correspondente. `EntityView`, recursos visuais e fundo também usavam somente as coordenadas canônicas.

A geração dos cinco biomas e o estado físico dos chunks estavam disponíveis. A falha era a seleção/posição das representações visuais. O preset tem largura 240 e chunks de 32 células: o último chunk tem **16 colunas**, então repetir uma grade fictícia de 256 células produziria outra lacuna na emenda.

## Correção

- `CircularTerrainVisibility` calcula regiões visíveis em coordenadas de células, com margem de um chunk, e retorna chunk lógico, índice da cópia e deslocamento horizontal. Cada cópia é deslocada pela largura **real** do mundo.
- `WorldView` identifica uma representação por `(ChunkId, Copy)` e posiciona o Node do chunk no deslocamento apropriado. TerrainLayer, overlay, água e fundo subterrâneo acompanham o mesmo deslocamento. Descarta somente representações fora da área visível; descarregar uma representação não invalida outra cópia ainda visível do mesmo chunk lógico.
- `HorizontalWrap` centraliza normalização, intervalos circulares e seleção de cópias. `WorldViewport` fornece a área visível às camadas Godot. Vegetação, objetos, céu, recursos, agentes, chuva e feromônios usam a mesma projeção circular; deixam de depender de uma lista fixa de três cópias.
- `AgentRenderSelection` reconhece indivíduos do outro lado da emenda sem duplicar a população. O renderer mantém o limite configurado inclusive para desenhos repetidos em zoom distante.
- A câmera normaliza a coordenada horizontal após movimento/arraste, permitindo completar voltas sem acumular coordenadas cada vez maiores. O scroll é atualizado antes de calcular a área visível.

O mundo mantém sua topologia circular: atravessar o último bloco leva ao primeiro. Não há novo planeta lógico a cada volta, novas espécies criadas pelo renderer ou geração ilimitada de chunks. O gerador, seed, cinco biomas, salvamento e limites verticais permanecem preservados. A representação cobre apenas o viewport e a margem; o cache lógico mantém `MaximumLoadedChunks`. O snapshot continua v6 com leitura de v5.

## Validação

Build completo: **zero erros/avisos**. Motor xUnit: **120 testes aprovados**, zero falhas/ignorados/erros, em `artifacts/circular-streaming-verification.log`.

Os novos cenários verificam cobertura única por célula em larguras 240, 256 e 65, chunk final parcial, posições negativas e distantes, viewport maior que uma volta, limites verticais, cinco biomas, escavação preservada após descarte do cache, cache de apenas dois chunks, seleção circular de agentes e limite de renderização sem alterar população.

Godot 4.6.3 .NET headless: **12 cenários**, zoom 0,25 e 1, câmera antes/depois das duas emendas e até quatro larguras distante. Foram verificadas **164.097 células sólidas**, exigindo tile real na TileMapLayer e centro gráfico correto para cada célula visível. Comparação integral do snapshot antes/depois confirmou que mover a câmera não alterou o mundo lógico. Registro: `artifacts/circular-streaming-game-console.log`.

O teste integrado agora inclui essa passagem lateral; `scripts/verify-godot.sh` exige também seu marcador de conclusão. O ambiente ainda registra o erro de certificados do macOS, portanto o script estrito continua rejeitando esse erro ambiental. Não foi realizada inspeção manual da janela nem medição de FPS da renderização. O teste headless verifica as camadas reais e suas coordenadas, não uma imagem de preview separada.

Reinicie na pasta AntWorld com `sh scripts/run-game.sh` para carregar a correção.
