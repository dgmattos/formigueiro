# Critérios mensuráveis de aceite: formigueiro orgânico

**Referência:** [solicitação técnica](organic-nest-implementation.md).  
**Status:** metas propostas; resultados só podem ser preenchidos por execução real.

## 1. Método e amostragem

- Executar 100 seeds (`10000..10099`) por espécie (normal, forte, superfície): 300 cenários estruturais.
- Cada cenário deve registrar seed, espécie, configuração, commit, resultados, tempos e status.
- Testes estruturais devem utilizar ambiente controlado com espaço, operárias e recursos suficientes. Testes ecológicos são separados.
- Métricas de organicidade exigem >=10 câmaras; métricas de ciclos exigem >=5 câmaras e candidatos viáveis. Registrar cenários inelegíveis e motivos; **não** contabilizá-los como PASS.
- Para avaliar 20 câmaras, usar gerador/planejador determinístico isolado ou executar expansão com orçamento de ticks calculado. Não inventar conclusão se a colônia não conseguir construir.
- Os testes devem distinguir grafo **planejado**, **concluído** e **conectividade física**.

## 2. Estrutura e conectividade

| ID | Métrica | Aceite |
|---|---|---|
| NEST-001 | Câmaras concluídas alcançáveis por BFS/DFS a partir de entrada concluída | 100% |
| NEST-002 | Câmaras concluídas com rota física até câmara da rainha | 100% |
| NEST-003 | Entradas concluídas ligadas à superfície e ao interior | 100% |
| NEST-004 | Entradas iniciais | 2 em >=95% dos cenários **com viabilidade comprovada**; 1 nos demais, com motivo |
| NEST-005 | Câmaras funcionais iniciais | >=4 quando geologia permite; caso contrário diagnóstico explícito |
| NEST-006 | Sobreposições inválidas de câmaras | 0 |
| NEST-007 | Células de estruturas fora dos limites verticais | 0 |
| NEST-008 | Escavações atravessando material proibido | 0 |
| NEST-009 | Estruturas marcadas concluídas antes da passagem física | 0 |
| NEST-010 | Conexões lógicas concluídas sem passagem real | 0 |

Validação deve executar busca em **células caminháveis**, não apenas no grafo abstrato.

## 3. Organicidade e variação

| ID | Métrica | Aceite em ninhos >=10 câmaras |
|---|---|---|
| ORG-001 | Câmaras fora do eixo X da rainha | >=70% |
| ORG-002 | Junções elegíveis com grau >=3 | >=15% |
| ORG-003 | Formatos/dimensões de câmaras distintos | >=3 |
| ORG-004 | Câmaras concentradas em uma única coluna X | <=30% |
| ORG-005 | Comprimento de túneis exclusivamente verticais | <=40% do total |
| ORG-006 | Ramificações laterais independentes | >=3 |

Definir junção elegível no grafo simplificado, excluindo células intermediárias e pontas sem bifurcação. Em 100 seeds por espécie, >=90% dos layouts devem ter assinatura topológica/geométrica diferente. Mesma seed/configuração deve reproduzir exatamente o mesmo layout, independentemente da ordem de geração dos chunks.

## 4. Ciclos e resiliência

No grafo simples conexo: `V` nós, `E` arestas, `E_extra = E - V + 1`, `ratio = E_extra / E`.

| ID | Métrica | Aceite |
|---|---|---|
| LOOP-001 | Proporção de conexões extras em layouts elegíveis e viáveis | 0,20–0,30; alvo 0,25 |
| LOOP-002 | Conexões alternativas que de fato fecham ciclo | 100% |
| LOOP-003 | Arestas duplicadas ou redundância sem benefício | 0 |
| LOOP-004 | Conexões declaradas concluídas sem túnel transitável | 0 |
| LOOP-005 | Câmaras alcançáveis após bloquear conexão secundária não crítica | >=70% nos cenários aplicáveis |

Medir `plannedAlternativeRouteRatio` e `completedAlternativeRouteRatio` separadamente. A meta não é obrigatória antes de construir as conexões, nem quando não houver candidatos viáveis.

## 5. Construção e logística

| ID | Métrica | Aceite |
|---|---|---|
| BUILD-001 | Blocos removidos diretamente pelo planejador após fundação | 0 |
| BUILD-002 | Remoções via ações válidas de escavação | 100% |
| BUILD-003 | Jobs ativos duplicados para mesma célula | 0 |
| BUILD-004 | Jobs atribuídos sem frente acessível | 0 |
| BUILD-005 | Câmaras/túneis concluídos com blocos impeditivos | 0 |
| BUILD-006 | Jobs bloqueados indefinidamente sem diagnóstico | 0 |
| BUILD-007 | Teleportes de formigas, rainha ou crias | 0 |
| BUILD-008 | Entulho/recursos duplicados | 0 |

Teste integrado deve planejar 1 câmara, 1 túnel e 1 conexão alternativa; acompanhar planejamento, atribuição, escavação parcial, consumo energético, skill, entulho, passagem e publicação. O orçamento de ticks deve ser calculado a partir do trabalho e resistência dos blocos.

## 6. Biologia e navegação

| ID | Métrica | Aceite |
|---|---|---|
| BIO-001 | Rainha alcança câmara designada em cenário navegável | 100% |
| BIO-002 | Ovos/larvas alcançam berçário com cuidadoras e rota | 100% |
| BIO-003 | Coletoras alcançam armazenamento em cenário navegável | 100% |
| BIO-004 | Entulho alcança entrada válida em cenário navegável | 100% |
| BIO-005 | Travessia indevida de bloco sólido | 0 |
| BIO-006 | Recursos duplicados em transporte | 0 |
| BIO-007 | Rotas entre entradas e através de chunks/wrap | 100% dos cenários válidos |

Controlar fome, energia, clima e mortalidade nos testes de navegação para não confundir falhas biológicas com falhas estruturais.

## 7. Persistência

| ID | Métrica | Aceite |
|---|---|---|
| SAVE-001 | Câmaras, túneis e entradas preservados no reload | 100% |
| SAVE-002 | Jobs e estados de construção preservados | 100% |
| SAVE-003 | Posições e estados dos agentes preservados | 100% |
| SAVE-004 | Recursos e deltas de terreno preservados | 100% |
| SAVE-005 | Divergência após reload vs execução contínua determinística | 0 |
| SAVE-006 | Corrupção silenciosa de saves antigos | 0 |

Salvar durante escavação parcial, recarregar, continuar e comparar estado final completo, inclusive PRNG. Usar fixtures antigas existentes, inclusive v8 quando disponíveis.

## 8. Desempenho (perfil LowMemory4GB)

Metas de benchmark controlado, não garantias universais:

| ID | Métrica | Meta |
|---|---|---|
| PERF-001 | Memória residente do processo | <=768 MB |
| PERF-002 | Planejamento de expansão por colônia | p95 <=20 ms |
| PERF-003 | Consulta de job disponível | p95 <=2 ms |
| PERF-004 | Validação de conectividade por ninho | p95 <=10 ms |
| PERF-005 | Jobs simultâneos por colônia | <=6 |
| PERF-006 | Bloqueios da thread principal atribuídos ao planejamento | 0 acima de 50 ms |
| PERF-007 | Vazamento de memória após ciclos de expansão/reload | nenhum crescimento não explicado |

Registrar hardware, OS, versão .NET/Godot, aquecimento, cenário, número de amostras, mediana, p95, máximo e memória. Testes de desempenho não devem ser declarados PASS sem execução no ambiente-alvo.

## 9. Entregáveis e condição de aprovação

- Testes xUnit, integração headless e smoke test Godot.
- Relatório real `AntWorld/artifacts/nest-validation-report.json` com `status` PASS/FAIL/NOT_RUN, cenários, métricas, falhas e razões de inelegibilidade.
- Documentação de algoritmos, migração de saves e limitações.
- Todos os testes críticos NEST, BUILD, BIO e SAVE aprovados.
- Métricas ORG/LOOP aprovadas em cenários elegíveis; desempenho aferido no hardware-alvo.
- Regressões existentes aprovadas.

**Não modificar os limites para obter aprovação, não preencher resultados fictícios e não declarar a feature concluída enquanto a geração física, integração e testes não estiverem implementados.**
