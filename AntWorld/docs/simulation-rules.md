# Regras da simulação

## Mundo

O gerador v3 usa [blocos físicos, recursos esparsos e chunks circulares](world-blocks.md). Seed e configuração definem biomas, relevo e recursos. Os cinco biomas ocupam faixas de largura aproximadamente igual, com ordem embaralhada pela seed. Relevo usa ondas periódicas com amplitudes limitadas; as colunas vizinhas, inclusive na emenda, diferem no máximo uma célula. Cavernas resultam de funções espaciais; profundidade determina solo, argila e rocha. Desertos têm areia superficial. Rocha aparece em proporção crescente nas camadas de transição e forma o substrato dos depósitos minerais. A superfície combina solo sólido e transições parcialmente preenchidas; isso permite cogumelos sobre substrato sólido.

| Bioma | Temperatura-base °C | Umidade inicial | Fertilidade | Fator de chuva | Densidade de recursos | Regeneração base/tick |
|---|---:|---:|---:|---:|---:|---:|
| Floresta | 23 | 0,72 | 0,90 | 0,45 | 0,75 | 0,035 |
| Campo | 21 | 0,50 | 0,80 | 0,28 | 0,60 | 0,030 |
| Deserto | 34 | 0,12 | 0,18 | 0,04 | 0,20 | 0,010 |
| Pântano | 27 | 0,95 | 0,85 | 0,65 | 0,70 | 0,028 |
| Região fria | 5 | 0,40 | 0,30 | 0,18 | 0,32 | 0,014 |

Alimento aparece na superfície e em fundos de cavernas úmidas. A quantidade inicial é 75% da capacidade; regeneração não excede capacidade. Não se cria alimento para evitar extinção.

## Relógio e ambiente

Um tick = 0,05 segundo simulado; um dia = 216.000 ticks (três horas). Cada mês tem 28 dias e uma estação: primavera, verão, outono e inverno. O ano tem quatro meses, totalizando 112 dias. As estações alteram temperatura em +2/+10/−2/−12 °C e regeneração contínua em 1,4/1/0,65/0,25. O ambiente atualiza a cada segundo simulado; a cada 30 segundos pode mudar a chuva. Profundidade reduz extremos térmicos. Cogumelos têm suas próprias probabilidades e ciclos de renovação, descritos em [cogumelos, calendário e ninho](season-nursery-mushrooms.md).

O controle de tempo vem de `src/AntWorld.Game/settings.json`; pausa interrompe todos os ticks, mesmo com backlog. x1/x2/x3 usam os multiplicadores configurados, atualmente 1/25/50. Câmera e desenho não definem tempo biológico.

## Formigas e colônias

Três colônias começam a 1/4 (Normal), 3/4 (Forte) e 1/2 (Superfície) da largura, cada uma com rainha, 21 operárias, 3 soldados, 2 larvas e 150 unidades de alimento. Não há compensação automática por ambiente inicial, vencedor ou população. Relações começam neutras; hostilidade é configurável independentemente da espécie.

O sistema anterior de agentes homogêneos foi substituído pelo modelo de [castas, fases, necessidades, habilidades, ações e IA](agents-ai.md). Esse documento especifica as regras atuais de movimento, alimentação, combate, postura, desenvolvimento, fundação, genética e envelhecimento. Rainhas não têm morte por idade no perfil padrão. Não há nascimento instantâneo de operárias nem geração de adultos para repor perdas.

Regeneração ambiental segue por segundo simulado, limitada à capacidade dos recursos. Feromônios são mapas esparsos com evaporação temporal. Recursos, saúde e genética não são agregados por distância: L2/L3 permanecem não implementados.

As necessidades basais e o dano fisiológico agora usam taxas por dia, editáveis em `survival`. Ovos/pupas usam reservas por desenvolvimento, postura mantém reserva alimentar e indivíduos procuram água localmente. Veja [regras de sobrevivência e resultados dos testes](survival-fix.md).
