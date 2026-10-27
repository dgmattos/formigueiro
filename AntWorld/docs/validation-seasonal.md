# Validação: calendário, câmara inferior e cogumelos

Ambiente: macOS arm64, SDK .NET 10.0.201, runtime 10.0.5, Godot 4.6.3 .NET portátil. Validação realizada em 26/10/2026. Detalhes funcionais e arquivos estão em [season-nursery-mushrooms.md](season-nursery-mushrooms.md).

## Build e testes

`sh scripts/verify-full.sh`: solução completa compilada, **zero avisos e zero erros**; **85 testes xUnit, zero falhas/ignorados/erros**. Log: `artifacts/season-nursery-mushrooms-verification.log`. O runner usa o motor xUnit real em processo, necessário pela restrição de sockets do ambiente.

Novos cenários cobrem:

- Chances de 10%/20% e 50%/60%, bônus de oito pontos percentuais limitado a 100%.
- Dois tipos no mesmo bloco, mínimo de 2 e teto combinado de 30, inclusive na API de mutação.
- Extração parcial, esgotamento, reposição, substrato compatível, cobertura, escavação e inundação.
- Vizinhos entre chunks e na emenda circular, geração independente da ordem de carregamento, presença superficial nos cinco biomas.
- Renovação por tempo simulado sem carregar chunks, configuração editável e rejeição de parâmetros inválidos.
- Decisão de coleta, transporte/deposição e consumo de cogumelos sem duplicação; coleta de material preservada e alimento distinguido de carga não comestível.
- Postura na câmara da rainha, entrega autônoma do ovo abaixo dela, vínculo exclusivo, rejeição de estrangeiros/soldados/destino inundado e liberação após morte do transportador.
- Calendário de quatro meses/estações de 28 dias, virada de ano e dia de três horas.
- Save/load de ovo transportado, cogumelo parcialmente coletado, configuração e retomada determinística atravessando ciclos de renovação. Saves v3 recebem rejeição explícita.

Os testes anteriores de biologia, IA, relógio/pausa, blocos, depósitos e SQLite real também passaram.

## Godot

Importação dos três novos atlas concluída. Execução com `--headless --quit-after 30 -- --smoke-test` registrou **PASS** e saída 0 para quatro atlas distintos de 1536 × 1024, limites iniciais de cogumelos, leitura de settings, preset de 240 × 96 com 54 indivíduos, controles de tempo, pausa/play exclusivos e round-trip pelo SQLite. Logs: `artifacts/seasonal-import-console.log`, `artifacts/seasonal-game-console.log` e `artifacts/seasonal-game.log`.

Limites reais: a importação registra `SocketException (13): Permission denied` do editor; importação e execução registram erro de acesso aos certificados do sistema macOS. Esses erros ambientais não foram ocultados. A cena executou o teste headless, mas a janela gráfica, interação manual e qualidade dos recortes não foram validadas. `verify-godot.sh`, que rejeita qualquer erro da engine, não deve ser considerado aprovado neste ambiente.

## Medições locais em Release

Sem renderer. São medições de execução, não garantia de FPS, 100× ou uso de memória em todos os mundos. Aquecimento reduz, mas não elimina efeitos de JIT, carga e escalonamento; especialmente o resultado de 100 agentes não permite inferir monotonicidade em relação a 500.

| População inicial/final | Média por tick | p95 por tick | Working set do processo |
|---|---:|---:|---:|
| 100 / 100 | 0,679 ms | 0,937 ms | 47,9 MiB |
| 500 / 500 | 0,242 ms | 0,342 ms | 53,6 MiB |
| 2.000 / 2.000 | 1,041 ms | 1,442 ms | 54,7 MiB |
| 10.000 / 10.000 | 2,259 ms | 2,809 ms | 63,4 MiB |

Fonte: `artifacts/seasonal-agent-benchmarks.json`. Cada população teve 200 ticks de aquecimento e 400 ticks medidos; cenário de câmaras densas, relações neutras e limite de 200 decisões por tick. Essa medição de 20 segundos simulados não inclui o ciclo horário completo de renovação dos cogumelos.

Mundo de 512 × 128, chunks de 16, cache de oito chunks, cinco repetições:

| Operação | Média |
|---|---:|
| Metadados e plano inicial completo de recursos, incluindo cogumelos | 35,93 ms |
| Gerar 256 chunks | 11,34 ms |
| 100.000 consultas de célula lógica | 3,85 ms |
| 100.000 consultas de vizinhos | 11,14 ms |
| Distribuir recursos em 256 chunks | 6,91 ms |
| Serializar snapshot | 0,48 ms |
| Desserializar e reconstruir o mundo | 11,14 ms |

Fonte: `artifacts/seasonal-world-benchmarks.json`. Célula: 16 bytes; snapshot inicial: 21.636 bytes; após 100 ticks: 48.372 bytes. Cache permaneceu em oito chunks. As medidas de memória são do processo sem Godot; não comprovam desempenho em um computador físico de 4 GB.
