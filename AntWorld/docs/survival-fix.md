# Correção da mortalidade precoce

## Evidência da partida do usuário

O log real da partida com três colônias, seed 12345, mostrou **175 mortes** (81 indivíduos iniciais e 94 ovos novos) e extinção no tick **50.608**, aos **2.530,40 segundos simulados**, ou **23,4% do primeiro dia**. As combinações de causas incluem esgotamento de energia, desidratação, fome e estresse térmico. O calendário continuava avançando; o dia de três horas ainda não tinha terminado.

Foi preservada uma cópia em `artifacts/survival-user-before.jsonl`, com resumo em `artifacts/survival-user-before-analysis.txt`. A entrega anterior havia adicionado instrumentação, mas não recalibrado a sobrevivência. Esta entrega altera efetivamente as regras.

## Mudanças

### Taxas por dia biológico

`AntBiologySystem` converte as taxas de `SurvivalSettings` usando `1 / SimulationState.TicksPerDay`. Multiplicadores de reprodução do jogo continuam acelerando os mesmos ticks. Não existe desconto de consumo por FPS ou pela velocidade selecionada.

Configuração inicial em `settings.json`, seção `survival`:

| Campo | Padrão | Unidade/efeito |
|---|---:|---|
| energyPerDay | 12 | Energia basal por dia, modificada por metabolismo e estresse |
| nutritionPerDay | 0,35 | Fração de nutrição por dia |
| hydrationPerDay | 0,6 | Fração de hidratação por dia, modificada pelo estresse |
| thermalDamagePerDegreeDay | 0,5 | HP/dia por grau fora da tolerância genética |
| exhaustionDamagePerDay | 60 | HP/dia sem energia |
| starvationDamagePerDay | 30 | HP/dia sem nutrição |
| dehydrationDamagePerDay | 60 | HP/dia sem hidratação |
| minimumEggIntervalDays | 0,25 | Intervalo mínimo entre posturas: um quarto do dia, ou 45 minutos simulados |
| foodReservePerIndividual | 2 | Alimento reservado por indivíduo vivo antes de permitir postura |
| foodHydration | 0,5 | Hidratação recebida por unidade de alimento consumida |

São parâmetros do simulador, não valores biológicos universais. A postura usa o maior intervalo entre `SpeciesDefinition.EggIntervalTicks` e o mínimo em dias. A primeira postura pode ocorrer no início; as seguintes respeitam o intervalo. Não altera os bônus de escavação/movimento, vida ou combate das espécies.

### Ovos e pupas

Fases fechadas usam reservas internas finitas proporcionalmente ao desenvolvimento: uma fase completa consome 40 unidades de energia, 0,25 de nutrição e 0,25 de hidratação. Há também perda ambiental de água de 2% da taxa diária, com modificador térmico. Não recebem o metabolismo de um adulto que caminha/escava. Não recebem alimento gratuitamente e continuam suscetíveis a temperatura, reservas esgotadas e dano.

Larvas usam metade do consumo basal de energia/nutrição de um adulto e mantêm necessidade de hidratação e alimentação assistida. Desenvolvimento depende de tempo, nutrição, temperatura e saúde. Nenhuma fase avança por FPS ou ganha adultos instantaneamente.

### Alimentação, água e descanso

- Comer atende déficits de energia, nutrição **ou hidratação**. Consome alimento da carga, estoque acessível no ninho/câmara de crias ou recurso alimentar na posição. A extração desconta a quantidade real do recurso.
- Operárias identificam também energia baixa e sede das larvas, mesmo com nutrição alta, e alimentam usando carga ou estoque real.
- Beber permite água livre na célula aberta atual ou em uma das quatro células abertas adjacentes. Não permite beber através de rocha nem confunde umidade retida com água livre. Cada unidade retirada restaura 0,1 de hidratação; não consome unidades inteiras para déficits menores que isso.
- Indivíduos com sede procuram uma rota local até água com BFS limitado a 128 posições e raio 8, respeitando passagens, emenda circular e blocos. A decisão recebe o primeiro passo alcançável. Buffers são reutilizados; não há consultas SQLite nem geração de chunks durante essa busca.
- Energia crítica eleva a prioridade de descansar. Descanso só gasta nutrição proporcional à energia realmente recuperada; não drena nutrição quando a energia está cheia. Recuperação de saúde também cobra pelo ganho efetivo.
- Não envia indivíduos a um estoque vazio somente para tentar comer. Coleta, transporte, depósitos e decisões territoriais continuam usando as regras anteriores.

### Reprodução e reserva da colônia

A postura exige alimento suficiente para o custo do ovo **mais** a reserva por população. Só o custo do ovo é descontado, e uma ação recusada não gasta alimento ou energia. Assim, a rainha não converte imediatamente o estoque inicial inteiro em ovos incapazes de sobreviver. A regra é igual para todas as espécies e não garante sucesso de nenhuma colônia.

## Persistência

Snapshots novos usam **v7**, incluindo as dez taxas de sobrevivência. v5 e v6 continuam legíveis, preservando os dados dos agentes, recursos, colônias e configurações existentes; recebem explicitamente os padrões novos de sobrevivência. **A trajetória futura muda**, pois os sistemas biológicos e de decisão foram corrigidos. Isso não é uma promessa de reproduzir os resultados antigos defeituosos.

O teste de compatibilidade usa arquivos reais gerados pelos executáveis v5/v6. A continuação determinística é comparada entre duas execuções das regras atuais. v1–v4 continuam rejeitados. Mortalidade já ocorrida em um save não é desfeita: para começar novamente, reinicie o jogo com o preset.

## Validação e limites

Build completo sem avisos/erros; **129 testes xUnit aprovados** (`artifacts/survival-verification.log`). Incluem incubação por tempo real de simulação, morte por privação, controle da postura/reserva, alimento e água sem duplicação, sede das larvas, busca circular de água, descanso sem desperdício, configuração, persistência e igualdade entre acelerações para a mesma duração simulada.

O teste Godot headless passou para a cena, os três atlas, SQLite, controles, terreno e streaming circular. O ambiente ainda registra erro de certificados do macOS; não foi validada manualmente a janela. Registro: `artifacts/survival-game-console.log`.

### Resultado da versão final no preset atual

Após **432.000 ticks**, o calendário alcançou o início do **dia 3**: dois dias completos, **81 → 99 indivíduos, zero mortes e três rainhas vivas**. Foram produzidos 18 ovos (Normal: 2; Forte: 8; Superfície: 8).

| Colônia | População final | Alimento final | Saúde da rainha |
|---|---:|---:|---:|
| Normal | 29 | 0,01 | 249,39/250 |
| Forte | 35 | 827,08 | 250/250 |
| Superfície | 35 | 242,56 | 238,01/250 |

A colônia Normal praticamente esgotou o alimento, portanto continua exposta à escassez em prazos maiores. A simulação não recebeu reposição artificial para manter a população. O ensaio sem renderer levou aproximadamente **179,9 segundos** neste ambiente; não é uma medição de FPS do jogo. A incubação até larva foi validada separadamente por ticks biológicos reais no teste automatizado; não foi inferida somente da contagem de ovos no ensaio.

Resultados da execução prolongada da versão final estão em `artifacts/survival-final-two-days-result.json`, `artifacts/survival-final-two-days/simulation.jsonl` e `artifacts/survival-final-two-days.antworld`. Esta validação usa o preset atual por 432.000 ticks (dois dias completos). Não significa sobrevivência garantida em todas as seeds ou indefinidamente: alimento pode acabar, biomas podem ser inadequados e danos/privação continuam letais. Não foi implementado transporte de água para o ninho, busca global de fontes, hidráulica completa ou calibração ecológica de longo prazo.

Para reproduzir, na pasta AntWorld:

```sh
dotnet build src/AntWorld.Cli/AntWorld.Cli.csproj --disable-build-servers -m:1
dotnet src/AntWorld.Cli/bin/Debug/net10.0/AntWorld.Cli.dll \
  --settings src/AntWorld.Game/settings.json --ticks 432000 \
  --diagnostics artifacts/survival-final-two-days \
  --save artifacts/survival-final-two-days.antworld
python3 scripts/analyze-diagnostics.py artifacts/survival-final-two-days
```

Para jogar com a correção: feche a janela atual e execute `sh scripts/run-game.sh`. O processo já aberto continua usando a versão que carregou anteriormente.
