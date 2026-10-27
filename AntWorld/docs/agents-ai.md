# Agentes, biologia e IA

A simulação usa três camadas: `ColonyAiSystem` calcula necessidades coletivas; `DecisionSystem` pontua ações válidas; `AntAction` e os sistemas físicos executam mudanças. Nenhuma camada depende de Godot. Espécie, parentesco ou quantidade de indivíduos não concedem vitória automática.

## Dados e ciclo biológico

`Ant` mantém identidade, posição, necessidades, tarefa e contadores no próprio objeto. Saúde usa uma struct; dez progressos de habilidade ficam em um array de structs. Definições de habilidades, castas e espécies são compartilhadas. Não existe Node por indivíduo nem cópia de definições em cada agente.

As castas são rainha, operária, soldado e macho reprodutor. Ovo, larva, pupa e adulto são fases. Sexo deriva da casta; larva não é uma casta. Rainha é uma formiga real, referenciada por `Colony.QueenId`; `QueenHealth` é uma fachada para essa mesma saúde, sem metabolismo duplicado.

Valores-base configuráveis por espécie: rainha 250 HP, operária 60, soldado 130, macho 40; imaturos 20. Atributos efetivos consideram genes, capacidade da casta e habilidades. Saúde máxima pode mudar sem curar, dano e recuperação têm operações separadas. Energia, nutrição, hidratação e condição são limitadas. Fome/desidratação causam deterioração; energia zero não é uma remoção instantânea.

Rainhas usam `NoAgeLimitPolicy` por padrão. Acumulam idade e podem morrer por danos e necessidades. Operárias/soldados usam risco progressivo de envelhecimento, com bases 180/240 dias, modulado por genética, saúde, nutrição, hidratação e condição. Não há uma data fixa de morte. Os sorteios usam o PRNG persistido.

O ciclo padrão é 2 dias de ovo, 15 de larva e 5 de pupa; desenvolvimento depende de temperatura, nutrição e saúde. Postura exige alimento, energia, intervalo e esperma armazenado. Crias herdam genes maternos com mutações; experiência não é herdada. Paternidade pode ser registrada, mas fecundação/recombinação sexual ainda não são simuladas. Rainhas filhas podem emergir de ovos e fundar colônias com **transferência de oito operárias existentes**, preservando IDs/população e transferindo 70 unidades do estoque. Não se fabricam adultos para fundar uma colônia. Transporte de fundação e abertura da câmara ainda são instantâneos.

## Ações e capacidades

Ações concretas: mover, explorar, coletar, carregar, depositar, escavar, construir, atacar, defender, fugir, descansar, alimentar larva, recuperar saúde, pôr ovo, alimentar-se e beber. O catálogo é compartilhado. A seleção consulta `CanExecute`; a execução revalida saúde, fase, capacidade, energia, água e requisitos locais antes de alterar estado. Apenas ações concluídas concedem experiência. Eventos por tick são limitados a 1.024 para não crescer indefinidamente.

Coleta usa `WorldMap.Extract`; alimento e os dois cogumelos podem ser coletados, consumidos e depositados sem duplicação. Carga reduz velocidade. Operárias reconhecem ovos e larvas do ninho antes de transportar; a posição acompanha o transportador, com liberação em destino, morte do transportador ou chegada à fase de pupa. Ovos nascem junto à rainha e são carregados fisicamente até a câmara quatro blocos abaixo. Duas operárias não podem carregar a mesma cria. A rota usa BFS específica da câmara inferior, com as mesmas colisões e limites da navegação do ninho. Alimentação assistida na câmara inferior pode consumir o estoque compartilhado da colônia, sem simular abastecimento interno separado. Construção reforça blocos parcialmente sólidos consumindo matéria orgânica carregada; não existe planejamento arquitetônico de câmaras.

Escavação considera solo/rocha, preenchimento, umidade, água, força e skill. Remove preenchimento gradualmente e atualiza invalidação de chunk/navegação/persistência. Rocha acima do limiar permitido é bloqueada. Recursos geológicos permanecem na coordenada, com orçamento original, até extração. Estabilidade estrutural, desabamento e hidráulica completa ainda não são simulados.

Combate valida relação hostil, proximidade, capacidade e intervalo de um segundo. Defesa/combate, genes, saúde e chuva alteram dano, que nunca é negativo. Companheiros de ninho e colônias neutras/aliadas não iniciam combate entre si. Mortos saem da população ativa; cadáveres orgânicos e recuperação da carga de mortos são evolução futura.

## Reconhecimento e decisões

Identidade inclui indivíduo, colônia, espécie, linhagem e pais opcionais. Parentesco funciona sem manter pais vivos na memória. Assinaturas químicas têm dois `ulong`: membros começam próximos do perfil da colônia e assimilam mudanças gradualmente. Reconhecimento retorna familiaridade, compatibilidade, confiança, pertencimento conhecido, classificação e ameaça. Parentes podem ser estrangeiros; integrantes do mesmo ninho não precisam ter genes idênticos.

Relações simétricas configuráveis: desconhecida, neutra, tolerada, aliada e hostil. Colônias iniciais são neutras. Fundação gera tolerância entre mãe/filha. Uma relação explicitamente desconhecida pode escalar após ataque territorial: exige proximidade do ninho, marca de território, baixa compatibilidade química e agressividade elevada. Relações neutras/aliadas/toleradas impedem essa escalada. Não há declaração automática de guerra por espécie; a API permite cenários hostis. Infiltração, adoção, diplomacia evolutiva e erros probabilísticos de reconhecimento não estão implementados.

Utility AI combina necessidades, distância, risco, genes, capacidades, perfis de casta e prioridades coletivas. Histerese adiciona bônus de continuidade; variação estável por ID reduz sincronização. Fome crítica, saúde baixa, ameaças e inundação têm pontuação emergencial. Rainhas priorizam sobrevivência/reprodução, operárias alimento/crias e soldados defesa. Ovos e larvas não fazem decisões de navegação.

## Tempo, navegação e feromônios

Tick fixo de 0,05 s: 20 Hz de movimento e 2 decisões/s como metas padrão. Ações não locomotoras são executadas em fases distribuídas a 2 Hz; combate tem cooldown adicional. Ambiente e prioridades coletivas atualizam a 1 Hz; idade e necessidades acumulam tempo em todos os ticks. Longevidade usa intervalo biológico configurável.

`AgentScheduler` mantém cursor persistido e limita decisões/tick. Com o perfil padrão, até 200 decisões/tick; agentes acima de 2.000 recebem avaliações menos frequentes, com rotação justa. Necessidades críticas podem antecipar a decisão. Esse limite não mata, oculta da lógica ou congela agentes. Execução também alterna a ordem para reduzir prioridade permanente na disputa por recursos.

Movimento usa crédito fracionário de velocidade. Colisões, diagonais bloqueadas e emenda circular são respeitadas. Retorno usa BFS esparsa por ninho, invalidada por mudanças físicas, limitada a 32.768 células; exploração/perseguição usam avaliação local. Não há garantia de caminho ótimo para destinos arbitrários ou além do limite da BFS.

Índice espacial agrupa indivíduos por célula e reutiliza buckets. Consulta circular é limitada a um raio local e até 48 indivíduos para decisão: percepção limitada é uma escolha explícita em câmaras muito densas. Recursos são consultados por coordenadas no armazenamento esparso; não há SQL durante ticks.

Quatro camadas esparsas de feromônios por colônia: alimento, alarme, casa e território. Concentrações limitadas a 15; valores abaixo de 0,001 são removidos. Evaporação depende do tempo simulado e da chuva; alarme evapora mais rápido. Trilhas influenciam exploração/retorno, evitamento de perigo e patrulhamento. Não existem partículas individuais. Temperatura/vento na evaporação ficam para evolução futura.

## Desempenho e limites reais

`LowMemory4GB`: 60 FPS como meta, até 300 indivíduos desenhados, orçamento de 2.000 indivíduos detalhados, 768 MiB como orçamento estimado e limite padrão de 20.000 indivíduos. Admissão de nascimentos usa estimativa determinística de memória e limite populacional; não é um limitador rígido de RAM do processo/GPU. O limite gráfico nunca altera a população. O jogo dedica até 45% do frame-alvo à simulação, verificando o orçamento entre ticks; um tick isolado ainda pode excedê-lo. Tempo pendente é preservado.

L0 e L1 executam os mesmos processos físicos/biológicos; a visibilidade só determina desenho. `SimulationLevel` reserva L2/L3, mas **simulação agregada L2/L3 não está implementada**. Não se inventam indivíduos, alimento ou genes para simular distância.

Buckets espaciais, buffers e listas de trabalho são reutilizados. Pooling de objetos `Ant` não está implementado (`EnableAgentPooling=false`), para não reciclar identidades/referências ainda observáveis. Não há threading, Redis ou IA generativa por agente. Ainda há alocações pequenas na avaliação e invalidação da navegação.

Veja [medições e validação](validation-agents.md), [persistência](persistence.md) e [configuração dos controles](time-controls.md).
