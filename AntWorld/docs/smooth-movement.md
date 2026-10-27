# Movimento contínuo e aceleração

A simulação continua em células com apoio em chão, paredes ou teto. `MovementSystem.AdvanceKinematics` acelera até a velocidade efetiva em aproximadamente 0,5 segundo simulado; o estado de velocidade desacelera em 0,2 segundo ao parar. A pausa não executa ticks. Parar não acumula créditos que permitiriam saltos imediatos na próxima caminhada.

Passos que mudam Y, incluindo diagonais, custam `1 / 0.65` créditos. A velocidade vertical fica em 65% da horizontal, preservando o fator global 0,8 aplicado anteriormente, os modificadores de espécie, carga, energia e saúde. Esses parâmetros ficam centralizados em `MovementSystem`. As rotas e colisões continuam sendo verificadas pelo mundo lógico.

`AgentMotionTrack` interpola os passos realmente executados, em ordem, incluindo as mudanças de apoio e orientação dos pés. O desenho usa posições fracionárias, mantendo o filtro nearest dos sprites. `Main` captura as mudanças após cada tick e informa o tempo de apresentação com a fração restante do relógio; pausar congela as transições. Larvas transportadas usam a pose visual da transportadora quando ela está visível.

O histórico é limitado a 32 segmentos por indivíduo visível, respeitando `MaxRenderedAgents`. Não há Node por agente nem alteração do estado lógico pela renderização. O desenho acompanha a simulação com um pequeno atraso, de até aproximadamente um passo em condições normais. Ao entrar na tela, em reposicionamentos não adjacentes ou ao exceder o histórico durante acelerações extremas, a apresentação se realinha ao estado atual; não inventa uma reta que atravesse o terreno. A junção circular usa o deslocamento curto entre as bordas.

Snapshots no formato 11 persistem velocidade e crédito de movimento, garantindo retomada determinística. Versões anteriores compatíveis continuam legíveis. O histórico visual não faz parte do save.

Validação: build sem avisos/erros; 180 testes xUnit aprovados, incluindo aceleração, custo vertical, interpolação, pausa, wrap, fila limitada e persistência; Godot headless aprovado. A fluidez não foi inspecionada manualmente em janela interativa. Logs: `artifacts/motion-build.log`, `motion-tests.log` e `motion-smoke.log`.
