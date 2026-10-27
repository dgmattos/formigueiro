# Solicitação técnica: formigueiros orgânicos

**Status:** especificação; implementação parcial.  
**Branch:** `feature/organic-nest-generation`  
**PR:** [#1](https://github.com/dgmattos/formigueiro/pull/1)  
**Escopo:** substituir expansão linear por rede orgânica de câmaras, túneis e entradas, mantendo as regras biológicas, navegação, persistência e desempenho.

## 1. Contexto e problema

O formigueiro atual cresce em um eixo vertical, com galerias horizontais repetidas. O código analisado contém:

- `AntWorld/src/AntWorld.Simulation/ColonyExpansionSystem.cs`: `RoomHalfWidth=4`, `PendingRoom`, `PendingTargets`, `HasLowerFloor`, `FindJob` e `Step` conduzem a expansão por andares.
- `AntWorld/src/AntWorld.Simulation/SimulationFactory.cs`: `ExcavateNest` cria poço central e duas salas.
- `AntWorld/src/AntWorld.Domain/Colony.cs`: `BroodChamber` é derivada de `Nest.Y + BroodChamberDepth`; `AdvanceFloor` muda o destino da rainha.
- `NestNavigation.cs`, `MovementSystem.cs`, `DecisionSystem.cs`, `LayEggAction.cs` e `ColonySystem.cs` dependem de destinos atuais.
- `DigAction.cs` já implementa escavação parcial; preservar.
- `docs/worker-expansion.md` documenta operárias coletoras/cavadoras, entulho e saves v8; preservar esses comportamentos.

Inspecionar novamente o workspace antes de editar, pois a branch pode evoluir.

## 2. Resultado desejado

Cada colônia deve construir uma **rede espacial conexa** com galerias orgânicas, câmaras funcionais, ramificações e entradas alternativas. A estrutura deve ser diferente entre seeds e respeitar a espécie, a geologia e as necessidades da colônia.

**Princípios:**

1. O grafo planejado não substitui o estado físico: um túnel só é transitável após escavação.
2. Fundação inicial pode abrir a infraestrutura mínima diretamente; toda expansão posterior usa operárias e `DigAction`.
3. Todas as câmaras **concluídas** devem ser alcançáveis a partir de pelo menos uma entrada, com rotas até rainha e berçário.
4. Crescimento depende de recursos, população, crias, capacidade e segurança, não apenas de tempo.
5. O mundo é horizontalmente circular; túneis, entradas e busca de caminhos devem respeitar o wrap.
6. O simulador deve permanecer funcional em perfil de 4 GB RAM.

## 3. Modelo de domínio

A branch já introduziu `NestLayout.cs` e `NestLayoutSettings.cs` com nós, arestas, entradas, estados e métricas iniciais. Revisar e evoluir esses arquivos, sem duplicar modelos.

Estruturas propostas:

- `NestChamber`: ID, tipo, centro, geometria, capacidade, status e posição de acesso.
- `NestTunnel`: ID, origem/destino, sequência de células, largura, custo e status.
- `NestEntrance`: ID, coordenada superficial, conexão, segurança e status.
- `NestConstructionPlan`: objetivos, dependências, custos, revisões e jobs.
- `NestConstructionJob`: alvo, célula de apoio, prioridade, responsável e status.
- `NestLayout`: coleção e conectividade do grafo; referências a câmaras funcionais.
- `NestLayoutSettings`: parâmetros globais e substituições por espécie.

Tipos de câmara: `Queen`, `Brood`, `FoodStorage`, `Rest`, `Fungus`, `Waste`, `General`. Inicialmente garantir Queen, Brood, FoodStorage e General; não fingir suporte funcional aos demais.

Estados de construção: Planned, WaitingDependencies, Available, InProgress, Completed, Blocked, Cancelled. Concluir somente após verificar as células e a navegabilidade real.

## 4. Algoritmo de geração

### Fundação inicial

Criar um ninho compacto com uma entrada principal, uma secundária se viável, câmara da rainha, berçário, depósito e câmara geral. As câmaras não podem ficar todas no mesmo X nem em um único poço. Caso a segunda entrada seja inviável, registrar motivo e planejá-la para expansão.

### Planejamento espacial

1. Obter necessidades da colônia e escolher estrutura funcional.
2. Amostrar candidatos em espaço livre e escavável, com distâncias mínimas, sem sobreposição, inundação ou invasão de outra colônia.
3. Aplicar viés horizontal/vertical por espécie e custo geológico.
4. Criar câmara elíptica/irregular em grid (largura inicial 4–11; altura 2–5; espaçamento mínimo 3), sempre com conectividade interna.
5. Planejar conexão por caminho de custo, considerando resistência, água, geologia, túneis existentes e distâncias.
6. Construir grafo conexo base (árvore geradora ou expansão incremental).
7. Planejar conexões extras que criem ciclos úteis, sem arestas duplicadas ou túneis paralelos inúteis.
8. Publicar jobs em ordem de dependência, com frentes de escavação alcançáveis.
9. Validar conclusão física antes de publicar estruturas como utilizáveis.

**Rotas alternativas:** para o grafo simples de câmaras conexas, `V = câmaras`, `E = túneis`, `E_extra = E - (V-1)` e `ratio = E_extra/E`. Alvo 25%, faixa 20%–30%, apenas com >=5 câmaras e candidatos viáveis suficientes. Medir separadamente planejadas e concluídas. Não forçar a faixa em ninhos pequenos ou geologicamente inviáveis.

**Túneis:** misturar segmentos horizontais e verticais, curvas discretizadas e bifurcações. Garantir adjacência transitável real (sem diagonais que `GetWalkableNeighbors` não suporte). Usar galerias existentes quando fizer sentido.

### Execução por operárias

- Preservar `WorkerRole.Collector` e `WorkerRole.Digger`.
- `ColonyExpansionSystem` deve planejar e distribuir jobs, nunca remover material.
- `DigAction` remove blocos progressivamente e cobra energia/skill.
- Manter 5% de chance de entulho por bloco totalmente removido, conforme regra atual, sem reamostragem a cada golpe.
- Carregar entulho até a entrada acessível adequada; não duplicar nem destruir cargas.
- Limitar trabalhos simultâneos (padrão 6), evitar jobs duplicados e detectar bloqueios/replanejar.
- Nunca teleportar rainha, ovos, larvas ou operárias.

## 5. Integrações obrigatórias

- **Colony:** trocar destinos implícitos `Nest`/`BroodChamber` por consultas ao layout, mantendo compatibilidade temporária apenas onde necessário; não manter duas fontes de verdade.
- **Queen e reprodução:** câmara designada e postura válida, sem alterar longevidade ilimitada por idade.
- **BroodTransport:** levar ovos/larvas para câmaras concluídas e alcançáveis.
- **Food/Feed/Deposit:** usar câmaras de armazenamento; preservar conservação de recursos.
- **Movement/NestNavigation:** múltiplas entradas, caminhos alternativos, revisões de conectividade e wrap horizontal; evitar BFS global por formiga a cada frame.
- **AI/DecisionSystem:** prioridades de escavação, escolha de frente, fuga de inundação, defesa de entradas.
- **ColonySystem:** fundação de colônias filhas deve utilizar novo layout sem sobrepor colônias.
- **Persistência:** snapshot versionado de grafo, jobs, status, entradas, posições e deltas de terreno; preservar saves anteriores (v8 e fixtures existentes). Se migração não for segura, falhar explicitamente sem sobrescrever o save.
- **Visualização:** opcional debug de câmaras, conexões planejadas/concluídas, entradas, frentes e bloqueios; sem criar Node por célula.

## 6. Parâmetros iniciais

| Parâmetro | Valor |
|---|---:|
| Entradas iniciais desejadas | 2 |
| Entradas máximas | 5 |
| Câmaras iniciais | 4 |
| Câmaras máximas | 60 |
| Largura de câmara | 4–11 células |
| Altura de câmara | 2–5 células |
| Distância mínima entre câmaras | 3 células |
| Largura de túnel | 1–2 células |
| Probabilidade de ramificação (referência) | 0,65 |
| Proporção de rotas alternativas | 0,20–0,30 (alvo 0,25) |
| Mínimo de câmaras para avaliar ciclos | 5 |
| Jobs simultâneos por colônia | 6 |

**Espécies:** normal com expansão equilibrada; forte com maior profundidade e geologia resistente; superfície com ninho raso, expansão lateral e preferência por mais entradas. Configurar por perfil de espécie, sem dispersar `SpeciesId` hardcoded.

## 7. Sequência de implementação

1. Finalizar modelo de domínio, parâmetros e grafo com testes puros.
2. Criar gerador de fundação orgânica e testes de conectividade física.
3. Criar planejador incremental, jobs, dependências e loops.
4. Integrar escavação progressiva e transporte de entulho.
5. Integrar destinos de IA, reprodução, transporte e navegação.
6. Implementar persistência versionada e migração.
7. Implementar diagnósticos e benchmarks.
8. Executar regressão e validar no Godot.

## 8. Critérios de validação

Consultar **[nest-acceptance-criteria.md](nest-acceptance-criteria.md)**. Critérios críticos: 100% de conectividade das estruturas concluídas, zero teleportes, zero duplicação de jobs/recursos, 20%–30% de conexões alternativas nos cenários elegíveis, saves determinísticos e limites de desempenho mensurados.

## 9. Entregáveis

- Código C# funcional integrado à solução atual.
- Testes automatizados unitários e de integração.
- Migração/versionamento de snapshots.
- Relatório `artifacts/nest-validation-report.json` gerado por execução real.
- Resumo de decisões arquiteturais, limitações e evidências de build/testes.
- Capturas/debug de seeds representativas, se o ambiente permitir.

**Não declarar implementação concluída apenas com o modelo de domínio.** A branch atual contém somente parte da infraestrutura de topologia.