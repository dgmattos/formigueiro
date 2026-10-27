# Escolta, berçário e patrulha

A rainha principal de cada colônia recebe até dois soldados adultos e uma operária adulta. `Ant.QueenEscort` identifica os acompanhantes; a escolha preserva os membros vivos atuais e preenche vagas por identidade estável. Não cria indivíduos para preencher vagas. A morte libera a vaga e seleciona um substituto disponível ao final do mesmo tick. Novos adultos e mudanças de colônia são reavaliados no ciclo coletivo. Sem rainha viva, a escolta é liberada.

Os acompanhantes continuam executando atividades permitidas. A patrulha local fica até 10 blocos da posição física da rainha. Acima de 10, retornam até ficar a 5 blocos; movimentos voluntários não ultrapassam 15. A distância é Manhattan e respeita a conexão circular horizontal. As rotas respeitam blocos, apoio e colisões. Se a rainha se afastar ou um substituto estiver longe, ele volta por caminhos existentes, sem teletransporte; obstáculos ou inundação podem impedir temporariamente a reunião. A restrição não afeta os demais indivíduos.

Soldados livres percorrem uma rota de câmaras, células, túneis e entradas concluídas. Cada indivíduo tem cursor próprio, com defasagem por identidade. Pontos bloqueados, inundados ou inacessíveis são ignorados. Necessidades biológicas e ameaças continuam competindo por prioridade. A rota compartilhada é reconstruída quando a estrutura muda; seu cache não é persistido.

O berçário é a câmara concluída, seca e alcançável mais próxima da posição atual da rainha por distância de caminhada. Pode coincidir com a câmara real. Empates preservam a designação atual. Essa regra substitui a preferência anterior por maior profundidade. Operárias buscam ovos e larvas deslocados, inclusive fora do alcance sensorial imediato, e os transportam fisicamente. Ovos e larvas não integram a escolta. Enquanto o transporte ocorre, podem estar fora do destino; não há teletransporte nem garantia de transporte sem uma operária ou caminho disponível.

Snapshots v12 persistem vínculo de escolta, retorno e cursor. Os formatos 5–11 continuam legíveis; no carregamento antigo, os novos campos iniciam desativados e a simulação organiza a escolta no próximo ciclo coletivo. O fixture v11 é um snapshot real, não um cabeçalho adulterado.

Validação: testes de cotas, estabilidade, reposição, vagas sem adultos, fronteira circular, retorno, patrulha, berçário e continuidade de snapshots. Não foi executada uma simulação de três dias nem inspeção visual interativa nesta alteração.
