# Genética

O genoma contém sete traços contínuos. Todos os sistemas usam os traços; a identidade da espécie serve para linhagem e apresentação.

| Gene | Brasa (A) | Musgo (B) | Limites |
|---|---:|---:|---|
| Agressividade | 0,75 | 0,25 | 0–1 |
| Força | 1,65 | 0,80 | 0,2–3 |
| Metabolismo | 1,45 | 0,70 | 0,3–3 |
| Exploração | 0,65 | 1,40 | 0,1–2 |
| Temperatura ótima | 28 °C | 18 °C | −5–40 °C |
| Tolerância térmica | 10 °C | 15 °C | 3–25 °C |
| Escavação | 1,30 | 0,70 | 0,1–2 |

Brasa combina mais força/escavação e adaptação ao calor com maior consumo; Musgo explora mais, consome menos e tolera melhor frio, mas tem menor força individual. Não existe vantagem absoluta codificada para todos os ambientes.

Cada descendente herda genes da rainha. Cada gene tem chance independente de mutar: 12% nos ovos. A perturbação uniforme varia em ±4% da amplitude permitida e é limitada aos extremos fisiológicos. Mutações podem melhorar ou piorar o desempenho. Os sorteios usam o PRNG da simulação, também salvo nos snapshots.

Reprodução custosa, morte por fome/estresse e competição alteram quais linhagens persistem. Não se calcula uma fitness global e não se seleciona artificialmente uma espécie vencedora. Só mutações de rainhas fundadoras se propagam a futuras gerações de colônias. Operárias mutadas não são reprodutoras.

Este é um modelo haploide simplificado de herança materna/brotamento: não implementa reprodução sexuada, recombinação, diploidia, deriva de identidade de espécie ou inferência evolutiva cientificamente calibrada. Os sete genes não têm todas as correlações fisiológicas reais; evolução prolongada pode favorecer extremos, o que requer futura calibração experimental.

Predisposição de exploração modula aprendizado e velocidade; força modula combate/carga; escavação modula trabalho e tolerância térmica participa do potencial de longevidade. Skills acumulam experiência apenas por ações válidas e têm nível máximo 20, com custo crescente. Descendentes começam sem experiência adquirida. Castas e fases são configuradas separadamente do genoma; identidade de parentesco não transmite níveis ou memórias.
