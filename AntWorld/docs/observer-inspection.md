# Inspeção por clique e acompanhamento

Clique esquerdo em um indivíduo abre seu card à esquerda, abaixo do calendário. O card mostra identidade, colônia, espécie, casta, fase, sexo, vida, idade, energia, fome, nutrição, hidratação, condição biológica, função, escolta, ação, carga e posição/destino. Fome é o inverso da nutrição. A idade usa o calendário simulado, com dias de três horas.

Indivíduos têm prioridade sobre o bloco sob seu sprite. A detecção usa os mesmos retângulos, rotação e posição interpolada empregados no desenho, incluindo cópias circulares. Apenas sprites efetivamente desenhados são selecionáveis. Em sobreposições, ganha o último desenhado. O teste usa o retângulo do sprite, não uma máscara de transparência por pixel.

Ao selecionar um indivíduo vivo, a câmera acompanha sua posição visual horizontal. O controle vertical continua manual. A seleção permanece durante o deslocamento e os dados são atualizados quatro vezes por segundo. Se ele morrer, o card mantém seu último estado e o acompanhamento cessa. Não há um Node adicional por indivíduo; somente um histórico de movimento extra pode ser mantido para o selecionado fora da lista visível.

Clique em um bloco mostra tipo físico, bioma, preenchimento sólido, composição, umidade, água livre, temperatura armazenada na célula, porosidade, permeabilidade, nutrientes, água retida, modificações, materiais e recursos. A coluna tem rolagem quando necessário. Os dados são consultados sem carregar chunks adicionais. Blocos negativos em X são normalizados para a topologia circular; cliques fora da altura do mundo limpam a seleção.

Esc fecha o card e libera o acompanhamento. Selecionar um bloco também encerra o acompanhamento. Cliques na interface não atravessam para selecionar o terreno. Seleção, card e câmera não são gravados nos snapshots e não modificam a biologia.

Validação: testes de formatação e leitura sem alteração/carregamento de chunks; smoke test Godot com eventos de clique em sprite e bloco, bloqueio de clique sobre o card, acompanhamento somente horizontal, fechamento por Esc e comparação integral do snapshot. Não houve avaliação visual manual em janela nesta alteração.
