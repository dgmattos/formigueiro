# AntWorld

Simulador 2D de ecossistemas de formigas em C#/.NET, com mundo procedural lateral e observação de colônias. Nenhuma regra escolhe uma espécie vencedora.

A sobrevivência foi corrigida para o dia de três horas: reservas de ovos/pupas, taxas por dia, postura com reserva alimentar, alimentação/hidratação e busca local de água. Configure `survival` no settings; veja [causas, mudanças e validação](docs/survival-fix.md).

A espécie **Superfície** usa o terceiro sprite, ninho raso, soldados reforçados e bônus de 20% de movimento ao caminhar sobre terreno exposto. Veja [regras, configuração e compatibilidade dos saves](docs/surface-species.md).

**Atualização atual:** terreno em TileMapLayer com atlas normalizados, grama por exposição e vegetação determinística nas quatro estações. A espécie Forte usa o novo sprite e ninho profundo; todas as espécies podem escavar rocha, com custos e velocidades próprios. Veja [diagnóstico, arquivos, regras e validação](docs/tilesets-vegetation-strong-species.md). Cogumelos, transporte de ovos, calendário, câmera e controles de tempo foram preservados. Edite [`src/AntWorld.Game/settings.json`](src/AntWorld.Game/settings.json) e reinicie para aplicar.

Veja [agentes e IA](docs/agents-ai.md), [controles e configuração](docs/time-controls.md) e [resultados da validação](docs/validation-agents.md). A janela gráfica não pôde ser validada neste sandbox; o teste de cena usa Godot headless.
## Atualização de blocos e chunks

O mundo usa células físicas compactas de 16 bytes, materiais e água separados, recursos esparsos, depósitos conectados, streaming lógico com cache limitado e vizinhos circulares. O snapshot atual v13 (com leitura de v5/v6/v7/v8/v9/v10/v11/v12) salva seed + alterações e estado biológico/social; rejeita explicitamente v1–v4. Primavera, verão, outono e inverno têm atlas próprios. Veja [modelo, algoritmos e limitações](docs/world-blocks.md) e a [lista de arquivos de blocos](docs/blocks-files.md).

Benchmarks: `sh scripts/benchmark-world.sh`. Testes completos: `sh scripts/verify-full.sh`.

O preset inicial também está em [`src/AntWorld.Game/settings.json`](src/AntWorld.Game/settings.json): `world` define seed, dimensões e chunks; `colonies` define as colônias, espécies, posições dos ninhos, alimento e quantidades iniciais. O preset incluído cria três colônias (Normal, Forte e Superfície) e 81 indivíduos, com tiles de 16 pixels e chunks de 32 células. Veja [campos e limites](docs/time-controls.md#mundo-e-população-inicial). Alterações são aplicadas ao reiniciar o jogo.

## Executar agora, sem dependências externas

Na pasta `AntWorld`:

```sh
dotnet build AntWorld.Core.sln -c Release --disable-build-servers -m:1
dotnet tests/AntWorld.Verification/bin/Release/net10.0/AntWorld.Verification.dll
dotnet src/AntWorld.Cli/bin/Release/net10.0/AntWorld.Cli.dll   --seed 42 --ticks 1500 --save artifacts/demo.antworld --svg artifacts/world.svg
```

Ou execute `sh scripts/verify-core.sh`. O build usa o SDK instalado e bibliotecas da plataforma; não usa Docker. `-m:1` e `--disable-build-servers` evitam os workers MSBuild que ficaram bloqueados no sandbox desta execução.

O CLI imprime população, reservas, saúde das rainhas, nascimentos, mortes e desempenho em JSON. `--load artifacts/demo.antworld --ticks 500` continua um snapshot por mais 500 ticks. `--width` e `--height` configuram as dimensões; `--help` lista os argumentos. O SVG é uma visualização estática do estado real da simulação, não uma captura do Godot.

## Executar o jogo Godot

Neste workspace, uma cópia portátil do Godot 4.6.3 .NET já está em `.tools/Godot_mono.app` (ignorada pelo Git). Para abrir a janela pelo seu terminal:

```sh
sh scripts/run-game.sh
```

Em outro computador, os pré-requisitos são SDK .NET 10 e **Godot 4.6.3 .NET**, incluindo seu diretório GodotSharp; acesso ao NuGet na primeira restauração. O SDK foi escolhido porque é o instalado neste workspace. O Godot usa bibliotecas .NET 8 ou posteriores; projetos podem direcionar uma versão posterior do runtime ([documentação oficial](https://godotengine.org/article/godotsharp-packages-net8/)). Essa combinação compilou e executou o teste headless. O alvo `net10.0` fica explícito também no `.csproj` gráfico para impedir que o editor o substitua pelo padrão `net8.0`.

1. Instale a [edição .NET do Godot 4.6.3](https://godotengine.org/download/archive/4.6.3-stable/).
2. Execute `dotnet restore AntWorld.sln` e `dotnet build AntWorld.sln -m:1`.
3. Abra `src/AntWorld.Game/project.godot` no editor .NET e pressione F6/F5.
4. Alternativamente, configure `GODOT4` com o caminho do executável e execute `sh scripts/run-game.sh`.

Exemplo macOS, ajustando o caminho à instalação:

```sh
export GODOT4="/Applications/Godot_mono.app/Contents/MacOS/Godot"
sh scripts/run-game.sh
```

No Windows/Linux, abra o mesmo `project.godot`; o código usa caminhos portáveis. Exportação de executáveis e templates de exportação não foram validados. O jogo tem sua própria `AntWorld.Game.sln`, pois o editor Godot procura uma solução com o nome da assembly. A solução raiz inclui todos os módulos e testes.

## Observação

| Controle | Ação |
|---|---|
| Botão pausa/play ou espaço | Congela/retoma todos os processos dos indivíduos; câmera permanece livre |
| x1 / tecla 1 | Velocidade padrão (`settings.json`: 1) |
| x2 / tecla 2 | Aceleração (`settings.json`: 25) |
| x3 / tecla 3 | Aceleração (`settings.json`: 50) |
| WASD ou arrastar com botão direito | Mover câmera |
| Roda do mouse ou + / − | Aproximar / afastar a câmera; = também aproxima |

O relógio multiplica ações e movimento pelo mesmo fator, com ticks de 0,05 s e conservação do tempo pendente. Aceleração efetiva depende do hardware; não há garantia de alcançar 100× em qualquer população. Selecionar velocidade durante a pausa mantém a pausa.
## Implementado

- Cinco biomas, altitude, camadas de solo/argila/rocha/areia, cavernas, recursos e chunks.
- Geração determinística com streams aleatórios separados e seed de 32 bits.
- Dia/noite, estações, chuva, seca, temperatura, umidade e regeneração sazonal.
- Quatro definições de espécie (três colônias no preset), alimentação, custo energético, exploração local, retorno ao ninho, feromônios, escavação, combate e morte.
- Rainhas como indivíduos, operárias, soldados, ovos/larvas/pupas, habilidades, genética materna e fundação com transferência de indivíduos existentes.
- Snapshot binário explícito, versionado, com SHA-256 e restauração do PRNG; armazenamento em arquivo e adaptador SQLite transacional.
- Cena C# Godot, terreno com invalidação por chunk, câmera e painel de observação. Os atlas fornecidos em `src/sprites` são preservados; um manifesto versionado prepara terreno e objetos separadamente em `Game/Assets/Prepared`.

## Limites do MVP

A ecologia usa um calendário próprio: um dia tem 216.000 ticks (três horas simuladas), cada estação dura 28 dias e o ano tem 112 dias. Velocidade e pausa afetam o calendário. Genética e reprodução ainda são modelos simplificados de herança materna; não há acasalamento/recombinação sexual, cadáveres, hidráulica completa ou calibração biológica.

O perfil `LowMemory4GB` limita decisões e desenho, preservando indivíduos fora da tela. As metas de 60 FPS/768 MiB não constituem garantia. L2/L3 e pooling de objetos Ant ainda não estão implementados. Navegação local/BFS tem orçamento limitado; câmaras de fundação continuam instantâneas. Detalhes e limitações estão em [agents-ai.md](docs/agents-ai.md).

`sh scripts/benchmark-agents.sh` mede 100, 500, 2.000 e 10.000 indivíduos sem renderer; gera `artifacts/agent-benchmarks.json`. Os sprites fornecidos são usados por regiões do atlas; a qualidade visual em janela ainda precisa ser conferida fora do sandbox.
A renderização lateral acompanha a conexão circular em todas as camadas, inclusive com zoom afastado e chunks finais parciais. Veja [correção e testes do streaming](docs/circular-streaming-fix.md).

## Diagnóstico de mortes

O jogo registra mortes e amostras das colônias em `user://diagnostics/simulation.jsonl`, com rotação limitada. O HUD mostra progresso do dia. Veja [resultado da investigação, configurações e comandos de análise](docs/mortality-diagnostics.md).

## Testes e documentação

`sh scripts/verify-core.sh` executa cenários reais compartilhados com os testes xUnit. Esse executável independente não substitui a execução do runner xUnit nem testa o provedor SQLite.

`sh scripts/verify-full.sh` compila tudo e executa o motor xUnit real dentro do processo, sem o socket de comunicação usado pelo VSTest. Em ambientes sem essa restrição, também é possível usar `dotnet test` nos três projetos `*.Tests`.

`sh scripts/verify-godot.sh` importa assets e executa o teste integrado headless. O script é estrito: retorna falha se houver qualquer erro da engine, inclusive os erros ambientais documentados nesta sessão. Ele usa a cópia em `.tools` ou o executável definido em `GODOT4`. Não transforma esses erros em um resultado verde.

Para direcionar o SQLite a outra pasta, defina `ANTWORLD_DATA_DIR`. O padrão continua sendo `user://antworld.db`.

- [Arquitetura e dependências](docs/architecture.md)
- [Regras da simulação](docs/simulation-rules.md)
- [Genética](docs/genetics.md)
- [Persistência](docs/persistence.md)
- [Validação executada e limitações](docs/validation.md)

Operárias têm funções aleatórias de coleta ou escavação, expandem câmaras para baixo/lados e transportam ovos e entulho. Veja [regras, persistência e validação](docs/worker-expansion.md).

Coletoras agora compartilham memória dos caminhos e procuram alimento além da vizinhança imediata. A caminhada exige apoio no chão, parede ou teto e os sprites acompanham esse contato. [Exploração e navegação com apoio](docs/foraging-and-surface-navigation.md).

Ninhos orgânicos: câmaras, entradas, construção por operárias e snapshots estão descritos em [organic-nest-runtime.md](docs/organic-nest-runtime.md). Os parâmetros ficam em `world.nestLayout` no settings. O relatório estrutural reproduzível fica em [nest-validation-report.json](artifacts/nest-validation-report.json).

Escolta com reposição automática, patrulha e berçário próximo da rainha: [regras e limitações](docs/queen-escort-and-patrol.md).

Coletoras consultam a rainha por contato antes de entregar provisões: [regras de alimentação e transporte](docs/collector-food-delivery.md).

Clique em indivíduos ou blocos para inspecionar propriedades; indivíduos selecionados são acompanhados horizontalmente pela câmera. Esc fecha o card. Veja [inspeção do observador](docs/observer-inspection.md).
