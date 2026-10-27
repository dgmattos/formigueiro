# Exploração de alimento e navegação com apoio

## Problemas corrigidos

A exploração anterior escolhia alimento numa janela local de quatro blocos e fazia movimentos ponderados sem memória dos caminhos percorridos. Cheiros e destinos locais podiam atrair a operária de volta, produzindo movimentos repetidos sem progresso. A marca `Excavated` também permitia caminhar em células sem contato com terreno, e vizinhos diagonais eram aceitos como apoio.

## Exploração das coletoras

`ForagingSystem` mantém conhecimento compartilhado em `Colony.Foraging`:

- Células observadas, distinguindo as ainda não visitadas das efetivamente percorridas.
- Tick da última passagem e posições de fontes de alimento observadas.
- Observação através de caminhos navegáveis locais, com alcance de quatro passos; não revela alimento através de paredes ou em todo o mapa.
- Busca de destinos ainda não percorridos pela rede conhecida. Quando todos foram visitados, revisita os caminhos mais antigos, permitindo reencontrar cogumelos renovados e descobrir passagens recém-escavadas.

As coletoras mantêm seu destino entre avaliações de IA. Fontes conhecidas são comparadas pela carga disponível e pelo percurso navegável de ida **e volta** até a rainha. Uma fonte reduzida a pequenas sobras pode provocar reavaliação; a coleta local prefere o recurso de maior quantidade quando há vários tipos no mesmo ponto. Sobras continuam disponíveis para consumo e coleta, sem descarte ou reposição artificial. Retornam à câmara da rainha com a carga; coleta/depósito continuam descontando e transferindo recursos reais. Descanso, alimentação própria, cuidado de crias e emergências continuam podendo interromper a exploração. Cavadoras mantêm sua função de expansão.

Uma rota escolhida exige diminuir a distância navegável ao destino em cada passo. Feromônios podem influenciar alternativas válidas, mas não fazer uma transportadora andar para trás na rota apenas porque há alimento naquela direção. A seleção permanece determinística, incluindo a conexão circular horizontal e as emendas entre chunks.

A memória é esparsa e compartilhada por colônia, limitada a 32.768 células, sem mapa completo por operária. A procura de fronteiras examina até 4.096 células por consulta. O cache de navegação limita o conjunto a 128 destinos e 131.072 entradas de distância, evitando limpar todas as rotas quando uma 33ª operária escolhe um destino diferente. Campos individuais de distância mantêm o orçamento anterior de aproximadamente 32.768 nós. Esses limites podem restringir a cobertura em mundos muito grandes; não há promessa de explorar componentes arbitrariamente extensos numa única busca.

## Chão, parede e teto

`WorldMap.ContactSide` reconhece apoio nos quatro lados ortogonais: baixo, esquerda, acima e direita. Uma célula de caminhada precisa:

1. Estar completamente aberta (`SolidFill == 0`).
2. Ter pelo menos um vizinho ortogonal com material sólido (`SolidFill > 0`).
3. Não estar bloqueada pela configuração de colisão da vegetação.

Contato apenas diagonal e a flag de escavação não tornam o ar transitável. Um bloco parcialmente escavado só abre uma passagem quando sua remoção termina, evitando que a operária entre antes e fique sem posição de trabalho. Movimentos diagonais entre células apoiadas continuam permitidos para contornar cantos; não atravessam um canto fechado por dois sólidos.

`SurfaceAttachmentSystem` atualiza o apoio antes da biologia/ações. A remoção do último apoio provoca queda vertical por células abertas, uma célula por tick, até novo contato ou limite do mundo; isso não é uma ação de caminhada no ar. Não há salto, voo, aceleração física contínua ou dano de queda nesta versão. Transporte de crias continua sendo controlado pela transportadora.

## Câmaras e saídas

O eixo vertical do poço (`ShaftX`) é separado da posição da rainha. Uma nova rainha começa uma célula ao lado do cruzamento, onde há apoio. A construção inicial garante apoio na travessia de cavernas naturais; a expansão posterior continua removendo material exclusivamente por ações de escavação.

O entulho é levado a uma célula apoiada na superfície próxima à abertura do poço. O ar imediatamente sobre uma coluna completamente aberta não serve automaticamente como destino de depósito. A posição preferida é recalculada a partir do terreno quando necessário.

## Apresentação

`SupportSide` e `SurfaceOrientation` não dependem do Godot. O renderer utiliza a direção do apoio para girar o sprite: chão 0°, parede esquerda +90°, teto 180° e parede direita −90°. O pivô fica na borda em contato e a parte inferior do recorte (pés) se ancora nesse pivô. O espelhamento horizontal considera o sentido de deslocamento ao longo da superfície. A transformação é aplicada no desenho em lote, sem criar Nodes por formiga e sem modificar o mundo lógico.

## Persistência

O formato v9 inclui o lado de apoio individual, eixo do poço e memória de exploração da colônia. Destino e tarefa usam os campos já existentes; caches de BFS são reconstruídos. A continuidade é testada por comparação integral dos snapshots após novas etapas de simulação.

Saves v5–v8 continuam legíveis. Conservam a coluna original do poço; quando a posição antiga da câmara não tem apoio, o leitor procura uma posição apoiada na mesma câmara, até duas células ao lado, com berçário válido. Os indivíduos mantêm suas posições e se deslocam/caem pelos sistemas normais após a retomada. Se não existir alternativa segura, a câmara não é deslocada artificialmente. Estados transitórios de queda em ar aberto são válidos nos snapshots. A trajetória futura obedece à física e à IA novas; não reproduz a caminhada sem apoio das versões anteriores.

## Validação

Build completo e testes reais xUnit: `artifacts/foraging-validation.log`. Os cenários novos cobrem exploração dos quatro extremos de túneis ramificados, cogumelos além do alcance local e depósito na câmara da rainha, progresso de rota na emenda circular, quatro direções de apoio, rejeição de contato apenas diagonal, escavação parcial, queda, orientação, memória limitada e retomada determinística, incluindo um fixture real do formato v8.

As verificações automatizadas do Godot estão em `artifacts/foraging-godot-smoke.log`. O ambiente registra o erro preexistente de certificados macOS `get_system_ca_certificates`; as verificações da aplicação são avaliadas separadamente. A orientação foi validada matematicamente e integrada ao renderer; não houve inspeção gráfica interativa da animação.

## Redução global da escavação

A velocidade de escavação de todas as espécies foi reduzida em **60%**. `ExcavationRules.GlobalSpeedMultiplier = 0.4`: cada ação de trabalho ocorre a cada 25 ticks (1,25 segundo simulado), em vez de 10 ticks (0,5 segundo). O teste mede 40 ações em 1.000 ticks para cada uma das quatro definições de espécie, contra 100 ações no ritmo anterior.

A redução foi aplicada ao intervalo, mantendo trabalho e custo energético por ação e preservando os coeficientes relativos de solo/rocha das espécies. Isso também evita que o arredondamento para pelo menos uma unidade de material anule a redução em indivíduos fracos. Movimento, calendário e demais ações mantêm suas próprias frequências; pausa e aceleração continuam operando sobre os ticks simulados.

## Resultados finais

- Build da solução: zero avisos/erros. **151 testes xUnit aprovados**, incluindo os quatro tipos de espécie com 40 ações de escavação por 1.000 ticks e escolha de carga útil em vez de sobras próximas.
- Godot headless: verificações de cena, sprites, UI, SQLite/restauração e streaming circular aprovadas. Permanece o erro de certificados do ambiente descrito acima.
- O ensaio prolongado final foi encerrado por solicitação do usuário. Última amostra persistida: **548,400 ticks**, aproximadamente **2.54 dias simulados**, mundo **2400 × 96**, seed **120207**. Não se declara execução completa de três dias para esta versão final.
- Na última amostra: **114 indivíduos vivos e 0 mortes**. Registros em `artifacts/foraging-final-three-days-diagnostics`; resumo em `artifacts/foraging-final-validation-summary.json`.
- O ensaio anterior de três dias (`foraging-three-days.json`) usava um mundo menor e precedia a melhoria de seleção por carga/viagem e a redução de 60% da escavação; seus resultados não substituem a medição da versão final.

| Colônia | Células percorridas | Alimento em estoque | Mortes |
|---|---:|---:|---:|
| 1 | 114 | 9538.01 | 0 |
| 2 | 206 | 4036.27 | 0 |
| 3 | 136 | 7462.09 | 0 |
