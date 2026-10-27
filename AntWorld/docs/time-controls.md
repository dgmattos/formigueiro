# Controles de tempo e settings

## Mundo e população inicial

O jogo lê o preset de `src/AntWorld.Game/settings.json` ao iniciar, junto com janela e velocidades. Edite o arquivo e reinicie; não é necessário recompilar.

| Seção / campo | Função | Valor inicial |
|---|---|---|
| `world.seed` | Seed determinística | 12345 |
| `world.width`, `world.height` | Dimensões em blocos, independentes do tamanho da janela | 240 × 96 |
| `world.chunkSize` | Lado de cada chunk em blocos | 32 |
| `world.maximumLoadedChunks` | Limite do cache de chunks | 64 |
| `colonies` | Lista de colônias a criar; adicionar/remover itens muda a quantidade | 2 colônias |
| `speciesId` | 0 = Normal/Brasa, 1 = Musgo, 2 = Forte, 3 = Superfície; espécies podem se repetir em colônias diferentes | Normal, Forte e Superfície |
| `nestPositionRatio` | Posição horizontal proporcional à largura, entre 0 e 1 (exclusive) | 0,25 e 0,75 |
| `queens` | Rainhas iniciais; o modelo atual exige exatamente uma por colônia | 1 |
| `workers`, `soldiers`, `larvae` | População inicial de cada colônia | 21, 3 e 2 |
| `initialFood` | Estoque inicial de alimento por colônia | 150 |

O preset incluído cria 54 indivíduos no total. `nestPositionRatio: 0.25` significa um ninho na coluna `floor(largura × 0.25)`; a profundidade acompanha a superfície procedural e a câmara é escavada automaticamente. As posições dos ninhos devem resultar em colunas distintas. Larvas têm fase `Larva`, sem navegação autônoma, e casta futura de operária.

São aceitas 1–12 colônias e até 20.000 indivíduos iniciais, conforme o perfil atual. Populações não podem ser negativas; dimensões/chunks seguem os limites de `WorldConfiguration`. Valores inválidos são rejeitados antes de gerar o mundo. Omitir completamente `world` ou `colonies` em arquivos antigos mantém o preset anterior; `colonies: []` não representa um mundo vazio válido. Campos desconhecidos dentro dessas seções provocam erro para detectar erros de digitação.

Cada execução normal começa um mundo novo. O preset não repõe indivíduos durante a simulação e não é reaplicado ao restaurar snapshots: população, estoques e posições salvos continuam sendo a fonte do estado restaurado.

Validação desta atualização: build completo sem avisos/erros, 68 testes xUnit passando, incluindo preset personalizado com três colônias e retomada determinística. O Godot headless confirmou a leitura do arquivo e a criação de 240 × 96 blocos, duas colônias e 54 indivíduos. Logs em `artifacts/preset-settings-verification.log` e `artifacts/preset-settings-godot.log`. Permanecem os erros ambientais de diretórios/certificados já documentados; isso não valida a janela gráfica.

## Janela

O mesmo `src/AntWorld.Game/settings.json` define o modo e o tamanho inicial da janela:

```json
"window": {
  "mode": "windowed",
  "width": 1440,
  "height": 900
}
```

Use `"windowed"` para modo janela (padrão) ou `"fullscreen"` para tela cheia. `width` e `height` são dimensões da janela em pixels; em fullscreen, a janela ocupa a resolução do monitor, sem alterar o modo de vídeo. Reinicie o jogo após editar. O sistema operacional pode limitar janelas maiores que a área disponível. Para conferir no editor Godot, execute em janela separada; a execução embutida pode ser controlada pelo editor.

`project.godot` mantém os valores de inicialização e o tamanho lógico de referência de 1440 × 900 para escala da interface. Ao carregar a cena, o JSON define o tamanho físico e o modo. Se a seção `window` estiver ausente em um arquivo antigo, o padrão é janela 1440 × 900. Se a seção existir, campos inválidos provocam erro explícito. Limites aceitos: largura 320–16384 e altura 240–16384.

## Velocidade

Edite `src/AntWorld.Game/settings.json`, também listado na pasta **Settings** da solução:

```json
{
  "speedMultipliers": {
    "x1": 1,
    "x2": 25,
    "x3": 50
  }
}
```

Os rótulos x1, x2 e x3 identificam os três botões do atlas. Os números no arquivo são os multiplicadores reais do tempo: no exemplo atual x2 executa 25 vezes e x3 50 vezes o tempo normal. Os atalhos 1, 2 e 3 e as dicas dos botões leem a mesma configuração. x1 é selecionado ao iniciar.

São aceitos inteiros positivos em ordem crescente, até 1.000. Arquivo ausente, JSON inválido ou valores fora da regra interrompem a inicialização com mensagem explícita no log. **Reinicie o jogo depois de editar**; não é necessário alterar ou recompilar C#. Não há atualização automática durante uma execução. Na exportação, inclua `*.json` entre os arquivos não-recursos do pacote Godot; exportação ainda não foi validada neste workspace.

No canto superior direito existem quatro botões: um único pausa/play e os três níveis. Enquanto roda, aparece **pausa**; enquanto pausado, aparece **play**. Trocar de nível durante a pausa preserva a pausa; play retoma no nível selecionado. Espaço alterna pausa/play.

O relógio converte tempo real em ticks fixos e preserva backlog. Aceleração multiplica **todos os processos**, incluindo deslocamento, decisões, necessidades, desenvolvimento e ambiente. Pausa não consome backlog nem atualiza indivíduos; câmera continua disponível. Se o hardware não alcançar a aceleração solicitada, o HUD informa a limitação.

Câmera: WASD e arrasto com botão direito para mover. Use rolagem do mouse ou **+** / **−** para aproximar/afastar; **=** também aproxima sem exigir Shift. As teclas +/− do teclado numérico são aceitas. Cada passo altera o zoom por um fator de 1,15, limitado entre 0,25 e 4; o ponto central da câmera é mantido. O zoom funciona durante a pausa e não altera o tempo da simulação.

Foram removidas da interface e dos atalhos as ações de novo mundo, inspeção/seleção, feromônios e save/load. Persistência continua disponível na infraestrutura, CLI e testes. Não há comandos diretos sobre formigas.

Assets originais preservados em `src/sprites/player-time-controle.png` e `001-formiga-normal.png`. Cópias no projeto Godot são usadas por regiões do atlas, sem criar um Node por agente. Os quatro atlas de terreno acompanham a estação do mês. Um dia dura três horas simuladas; cada estação tem 28 dias. `world.mushrooms` configura chances, renovação e limites dos cogumelos; veja [regras completas](season-nursery-mushrooms.md).

## Terreno e ninhos

`rendering.tileSize` define o tamanho visual de uma célula (padrão 16 pixels; múltiplos de 16 até 64). Com `world.chunkSize: 32`, cada chunk ocupa 512 × 512 pixels. `world.vegetation` contém densidades por bioma, distância entre árvores, limites ambientais e colisão opcional da madeira. `world.nests.normalBaseDepth: 4` e `normalDepthMultiplier: 1.25` resultam em cinco blocos abaixo da superfície. `strongUndergroundRatio: 0.75` posiciona o ninho Forte a 75% do subterrâneo disponível, reservando o espaço da câmara de crias. Veja [regras completas](tilesets-vegetation-strong-species.md).

`world.nests.surfaceDepth: 2` controla o ninho raso da espécie Superfície (ID 3). O preset atual tem três colônias e 81 indivíduos. Consulte [regras da espécie](surface-species.md).
