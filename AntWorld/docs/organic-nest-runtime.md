# Ninhos orgânicos: implementação

A branch `feature/organic-nest-generation` continha modelos de domínio e critérios de aceite, mas ainda não gerava terreno nem integrava a construção. A implementação mantém as mudanças mais recentes da `main`: navegação com apoio em chão, paredes e teto, busca de alimento e intervalo de escavação de 50 ticks. Não altera os multiplicadores de velocidade do usuário.

## Geração e conectividade

`OrganicNestPlanner` distribui câmaras elípticas com espaçamento e dimensões configuráveis. Um PRNG próprio, derivado da seed, colônia e número da câmara, mantém o planejamento independente do PRNG biológico e da ordem de carregamento dos chunks. Os perfis normal, forte e superfície alteram a distribuição horizontal, profundidade, custo da rocha e quantidade desejada de entradas.

`NestTunnelPlanner` usa A* ponderado, com desempate estável, ruído de custo por coordenada, limite de 8.192 células e respeito à conexão circular. Protege os apoios das câmaras e reserva as estruturas e paredes das outras colônias. Ligações extras conectam pares distintos e acrescentam células novas; a proporção é calculada separadamente para o grafo planejado e o concluído.

`NestFoundation` prepara quatro câmaras funcionais e duas entradas (três preferidas pela espécie de superfície), quando a geologia permite. A fundação pode construir paredes de contenção em cavernas naturais. A prévia física é validada antes de modificar terreno ou recursos. Ajustes ao relevo e entradas inviáveis ficam em `Layout.Diagnostics`.

A passagem física é a autoridade: uma célula aberta precisa de contato com material sólido; diagonais respeitam as mesmas restrições da navegação existente. Uma ligação no grafo não habilita movimento através de blocos. O mundo continua responsável pelas colisões, invalidação da navegação, renderização e deltas persistidos.

## Construção e logística

`OrganicNestConstruction` avalia população, capacidade do berçário, estoque e espaço de armazenamento. Planeja até 60 câmaras, com intervalo configurável e até seis frentes disponíveis/atribuídas simultaneamente. Cada célula possui um único trabalho, com apoio, operária responsável, progresso, prazo de atribuição e motivo de bloqueio. Morte, carga transportada e expiração liberam atribuições.

Depois da fundação, somente `DigAction` remove os blocos. A ação exige posse do trabalho e mantém custos, aprendizado, modificadores de espécie e chance de 5% de entulho ao terminar um bloco. `ExcavationAccessGuard` impede remoções que isolariam indivíduos, considerando também a queda física após perder apoio. Frentes inundadas ou sem acesso recebem diagnóstico. O replanejador tenta substituir túneis bloqueados por um percurso seguro, respeitando o intervalo configurado, sem desfazer blocos já escavados; trabalhos obsoletos são cancelados sem duplicar alvos.

Câmaras e túneis somente são publicados como concluídos quando suas células foram abertas e seus destinos são fisicamente alcançáveis. O berçário designado pode passar para uma câmara mais profunda; o destino da rainha usa a câmara concluída e alcançável mais distante das saídas. A IA move os indivíduos: alterar um destino não altera suas posições.

Coletoras depositam no armazenamento designado. Alimentação na rainha, berçário e depósito usa o estoque agregado existente, sem duplicação de alimento. Transportadoras de entulho escolhem uma entrada concluída, exposta e alcançável. A fundação de colônias filhas usa o mesmo gerador e exige acesso dos fundadores; a mudança de colônia não teleporta agentes.

## Configuração

Os parâmetros ficam em `src/AntWorld.Game/settings.json`, em `world.nestLayout`; as profundidades iniciais continuam em `world.nests`. A alteração vale para partidas novas. Saves restauram sua própria configuração. Presets pequenos ou geologicamente restritos podem ter menos câmaras/entradas, acompanhadas de diagnóstico, em vez de sobrepor colônias.

## Persistência

O formato 10 conserva a estratégia seed + alterações do terreno. Uma seção de metadados JSON, limitada a 4 MiB dentro do envelope binário com SHA-256, armazena layout, designações, perfis e trabalhos. Esta opção usa o serializador do .NET sem dependências adicionais. Não duplica as células físicas do mundo; listas de coordenadas representam apenas as estruturas do ninho.

Formatos 5–9 continuam legíveis. A migração identifica os destinos antigos e reconstrói somente rotas fisicamente existentes, sem escavar nem consumir aleatoriedade. Depósito e rainha compartilham o destino original nos saves antigos. Se a geometria antiga não permitir reconstruir o grafo com segurança, ela permanece explicitamente no modo legado (`Layout == null`), preservando a expansão anterior. Formatos 1–4 são rejeitados com mensagem, sem sobrescrever o arquivo.

## Validação reproduzível

```sh
cd AntWorld
sh scripts/verify-full.sh
dotnet tests/AntWorld.Benchmarks/bin/Debug/net10.0/AntWorld.Benchmarks.dll --nests artifacts/nest-validation-report.json
```

O relatório usa 100 seeds por espécie (300 cenários), fundação real seguida de planejamento isolado até 20 câmaras. Para testar a geometria planejada, materializa o terreno do cenário controlado; isso não representa trabalho realizado por operárias. O relatório separa essas fases, mede conectividade, organicidade, ciclos, bloqueio de conexão secundária, tempos e memória residente. O teste integrado de construção executa movimento, escavação, alimentação e depósito reais, com um orçamento limitado de trabalho. Testes de snapshots salvam durante escavação parcial e comparam a continuação completa, incluindo o PRNG.

Não foi executada simulação ecológica de três dias. As medições do relatório descrevem o ambiente de execução, não garantem a mesma taxa de quadros em qualquer computador de 4 GB.

## Limitações

- Túneis gerados têm largura de uma célula; o modelo permite duas. Não há simulação de desabamento estrutural.
- Não há drenagem automática: o replanejador desvia túneis quando existe alternativa, mas uma câmara inteiramente inundada pode continuar bloqueada até o ambiente mudar.
- O estoque alimentar permanece agregado por colônia, acessível nos destinos funcionais; não há inventário físico individual por câmara.
- Não foi executado ensaio prolongado de vazamento de memória. O relatório marca essa verificação como não executada.
- A inspeção do Godot é headless; a aparência final não foi avaliada manualmente numa janela interativa.

## Resultado da validação desta implementação

Build da solução: zero avisos e zero erros. xUnit: 165 testes, zero falhas. Godot 4.6.3 .NET headless: aplicação, SQLite/HUD, TileMapLayer e streaming circular aprovados. O ambiente emite um aviso de certificados CA do macOS, sem impedir os testes.

Os 300 cenários estruturais foram aprovados, com 300 assinaturas distintas. Planejamento incremental p95: 1.622 ms; conectividade p95: 3.743 ms; consulta de trabalho p95: 0.0036 ms. Maior memória residente amostrada no benchmark do motor: 69.2 MiB (não inclui renderização interativa). As amostras e a identificação da árvore de trabalho estão no relatório JSON.

## Escolha da câmara da rainha

A cada avaliação coletiva (tempo simulado), `QueenChamberSelection` compara a distância real de navegação das câmaras concluídas até a saída utilizável mais próxima. Com várias entradas, escolhe a maior dessas distâncias mínimas. Usa percursos com apoio e wrap horizontal; altura e distância em linha reta não substituem o caminho real.

Todas as funções de câmara podem receber a rainha, inclusive berçário ou armazenamento. Esta regra substitui a preferência anterior por um nível acima do berçário, preservando os destinos próprios de crias e alimento. Câmaras inundadas (água >=200), inacabadas ou inalcançáveis da posição atual da rainha não concorrem. Saídas bloqueadas, não concluídas ou não expostas são ignoradas. Sem saída utilizável, mantém o destino atual. Em empate, mantém a câmara atual; depois usa o menor identificador para garantir determinismo.

A IA existente executa a caminhada, sem teleporte. Fuga de inundação e necessidades críticas continuam podendo interromper a viagem. A escolha é reavaliada após expansão/alterações de acesso; campos de distância usam o cache limitado de navegação. `QueenChamberId` já é persistido no formato 10, portanto não foi necessário mudar o formato de save.

Validação da seleção da rainha: build sem avisos/erros, 169 testes xUnit aprovados e smoke test Godot headless aprovado. Casos específicos cobrem múltiplas saídas, wrap, empate, câmaras indisponíveis, caminhada e retomada determinística de snapshots. Logs em `artifacts/queen-build.log`, `queen-tests.log` e `queen-smoke.log`.
