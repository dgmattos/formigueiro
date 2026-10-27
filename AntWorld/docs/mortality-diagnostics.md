# Diagnóstico de mortalidade e passagem do tempo

**Atualização:** as causas abaixo foram posteriormente tratadas na [correção de sobrevivência](survival-fix.md). Este documento preserva a investigação anterior e as instruções dos logs.

## Resultado da investigação de 26/10/2026

O log da sessão gráfica disponível em `~/Library/Application Support/Godot/app_userdata/AntWorld/logs/godot.log` registrava apenas inicialização, seed 12345, 54 indivíduos e velocidades 1/25/50. Não contém causas de morte nem permite reconstruir retrospectivamente cada evento dessa partida.

Foi reproduzido o **settings atual** no núcleo C#, sem renderização, por 216.000 ticks, com instrumentação que não altera as regras biológicas. Resultado determinístico observado:

- População inicial: 54 indivíduos. Nasceram mais 68 ovos; morreram os 122 indivíduos.
- Primeira morte: tick 20.572, aos 1.028,60 segundos simulados (17min08,6s), de um ovo com energia esgotada.
- Extinção: tick **65.765**, aos **3.288,25 segundos simulados (54min48,25s)**, ou **30,45% do primeiro dia**.
- Mortes por combinação de fatores ativos no dano final: 72 energia esgotada; 36 desidratação; 12 energia + desidratação; 1 energia + fome; 1 energia + fome + desidratação. Não foram registrados combate, envelhecimento ou estresse térmico no dano fatal dessa execução.
- Entre os 54 indivíduos iniciais: 36 desidratação; 12 energia + desidratação; 4 energia; 1 energia + fome; 1 energia + fome + desidratação. Portanto a contagem total inclui os ovos novos, não somente os agentes iniciais.
- Primeiras amostras com alimento zero: colônia Normal aos 780s simulados; Forte aos 900s. São amostras a cada 60s, não o instante exato de esgotamento.
- Aos 216.000 ticks, o calendário alcançou o **dia 2**, mesmo com população zero. O campo antigo `day` da saída CLI é um índice a partir de zero; o HUD e os logs diagnósticos mostram o dia a partir de um.

Evidências: `artifacts/mortality-reproduction/simulation.jsonl`, `artifacts/mortality-reproduction-result.json` e resumo `artifacts/mortality-analysis.txt`. O núcleo completou essa execução em cerca de 47,2s neste ambiente; isso não mede FPS nem desempenho da janela Godot.

## Por que pode haver extinção antes da mudança de dia

`SimulationState` define 20 ticks/s e 216.000 ticks/dia, equivalentes a **três horas simuladas**. `SimulationClock` multiplica o número de ticks, sem um multiplicador biológico separado. O máximo atual do settings é **50×**; o mínimo teórico para completar um dia é **216 segundos reais (3min36s)**. A limitação de processamento por frame pode aumentar esse tempo. A extinção reproduzida corresponderia a aproximadamente 65,8s reais em 50× sustentados.

O HUD agora mostra percentual do dia e segundos simulados. O log `playback` registra fator solicitado, ticks/s efetivos, fator efetivo e backlog, distinguindo calendário lento por processamento de falta de avanço.

A reprodução expõe uma calibração incompleta de necessidades e desenvolvimento:

- `AntBiologySystem` drena energia também de ovos. Com genes base, 80 de energia e metabolismo 1,45, o consumo basal de 0,055 × metabolismo/s esgota a reserva em cerca de 1.003s; o dano subsequente mata o ovo em aproximadamente 25s.
- A incubação está configurada para dois dias, ao menos 21.600s simulados, e desacelera com nutrição/temperatura/saúde. O ovo não se alimenta autonomamente e `FeedLarvaAction` atende larvas, não ovos. A reserva atual não sustenta a incubação.
- Hidratação basal cai 0,0006/s, antes de modificadores ambientais. `DrinkAction` exige água livre na própria célula. A decisão retorna ao ninho quando falta hidratação, mas ainda não procura uma fonte de água distante nem permite transporte de água.
- Alimento inicial, postura e reposição por coleta não se sustentaram neste preset; os estoques esgotaram. `FeedAction` oferece um pouco de hidratação, mas depende de alimento acessível. Bioma/deslocamento/coleta e custos por ação também influenciam o resultado.

Esses achados exigem calibrar reservas/consumo/desenvolvimento e completar busca/abastecimento de água. Esta entrega adiciona diagnóstico e visibilidade do calendário; não recalibra a ecologia nem garante sobrevivência artificialmente. Causas do último dano não são atribuição de todo o histórico de danos acumulados.

## Logs das próximas partidas

O diagnóstico vem ativado em `src/AntWorld.Game/settings.json`:

```json
"diagnostics": {
  "enabled": true,
  "sampleIntervalSeconds": 60,
  "maxFileSizeMb": 8,
  "retainedFiles": 3
}
```

Reinicie usando `sh scripts/run-game.sh` para carregar o código novo. A localização é impressa no console. No macOS, por padrão:

```text
~/Library/Application Support/Godot/app_userdata/AntWorld/diagnostics/simulation.jsonl
```

Com `ANTWORLD_DATA_DIR`, usa `<ANTWORLD_DATA_DIR>/diagnostics`. Há até três arquivos na retenção padrão: `simulation.jsonl`, `.1` e `.2`, cada um limitado a 8 MiB. A abertura de outra sessão também rotaciona o anterior. O histórico mais antigo pode deixar a retenção; copie os arquivos se quiser preservar uma investigação. Logs novos não reconstituem sessões anteriores sem instrumentação.

Eventos:

- `session_started`: seed, configuração física, parâmetros de espécie, performance, runtime e versão do snapshot.
- `ant_died`: tick, identidade, espécie/casta/fase, idade, posição/destino/tarefa, fatores do dano fatal, atacante quando combate, saúde antes do dano, energia, nutrição, hidratação, temperatura/tolerância genética, água local, carga e alimento da colônia.
- `population_sample`: população/estoque/nascimentos/mortes por colônia, médias das necessidades, contagens por tarefa e fase.
- `population_extinct`: instante de extinção e totais de mortalidade.
- `playback`: velocidade solicitada, velocidade efetiva medida e tempo pendente; também registra pausa/play. Medição zero em uma troca de controle não significa travamento.
- `session_closed`: totais ao fechamento normal. Encerramento abrupto pode perder o buffer mais recente.
- `records_dropped`: aviso se o limite de 4.096 registros pendentes foi excedido.

O motor emite evidência antes de retirar o agente. Arquivo/JSON ficam em Infrastructure; o domínio não depende do Godot nem de um logger externo. O Game descarrega o buffer fora dos ticks, e o CLI a cada 1.000 ticks. Falhas de I/O desativam o diagnóstico e são mostradas no console, preservando a simulação. Não há SQLite por decisão nem alterações de formato do snapshot v5. O diagnóstico não é um replay completo: caminhos/caches e toda a sequência de ações não são gravados.

## Analisar e reproduzir

Na pasta `AntWorld`, para resumir a última sessão dos arquivos retidos:

```sh
python3 scripts/analyze-diagnostics.py "$HOME/Library/Application Support/Godot/app_userdata/AntWorld/diagnostics"
```

O script usa somente a biblioteca padrão do Python. Também aceita um único JSONL; sinaliza histórico parcial ou linhas incompletas. Para acompanhar ao vivo:

```sh
tail -f "$HOME/Library/Application Support/Godot/app_userdata/AntWorld/diagnostics/simulation.jsonl"
```

Reprodução independente da janela com o mesmo preset:

```sh
dotnet build src/AntWorld.Cli/AntWorld.Cli.csproj --disable-build-servers -m:1
dotnet src/AntWorld.Cli/bin/Debug/net10.0/AntWorld.Cli.dll \
  --settings src/AntWorld.Game/settings.json --ticks 216000 \
  --diagnostics artifacts/mortality-reproduction
python3 scripts/analyze-diagnostics.py artifacts/mortality-reproduction
```

`--diagnostics` ativa explicitamente o registro no CLI com os limites padrão; o JSON de apresentação controla o logger do jogo. Para investigar snapshots use `--load arquivo.antworld` no lugar de `--settings`. Não combine `--settings` com `--load`, `--seed`, `--width` ou `--height`.

## Validação

Testes verificam fatores biológicos combinados, atacante no combate, emissão única antes da remoção, rotação/limite de arquivos, extinção, configuração inválida, igualdade dos snapshots com instrumentação e equivalência de 1×/50× para a mesma duração simulada. Pausa não avança ticks. Build completo: zero avisos e zero erros. **104 testes xUnit aprovados**, zero falhas/ignorados/erros (`artifacts/mortality-verification.log`). O analisador Python foi executado sobre a reprodução e sobre os logs da cena.

Godot 4.6.3 .NET headless criou e descarregou os logs automaticamente, executou o smoke test e validou TileMapLayer (`artifacts/mortality-game-console.log`). Permanece o erro ambiental de certificados do macOS; não é um resultado verde do script estrito nem uma validação manual da janela. SDK utilizado: .NET 10.0.201.
