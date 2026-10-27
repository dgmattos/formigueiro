# Persistência

## Formato binário

`BinarySnapshotCodec` usa `BinaryWriter/BinaryReader`, com ordem de campos explícita, tipos primitivos e tamanho limitado. É a alternativa binária ao MessagePack: evita depender de pacotes não disponíveis e permite controlar versionamento/validação. Não usa JSON como snapshot nem `BinaryFormatter`.

Cabeçalho: ASCII `ANTWORLD` (8 bytes), versão Int32 (4), tamanho do payload Int32 e SHA-256 (32 bytes). Inteiros e doubles seguem a representação little-endian do BinaryWriter. O payload contém versão do gerador, seed/configuração, extensões do catálogo e alterações de células/recursos/clima, além de PRNG, relógio, IDs, colônias, genes e agentes. Feromônios são serializados esparsamente. Terreno e depósitos base são reconstruídos; a estratégia de blocos continua descrita em [world-blocks.md](world-blocks.md#persistência). Saves v1/v2/v3 são rejeitados explicitamente, sem alterar o arquivo.

A leitura exige integridade, versão conhecida, tamanho consistente, ausência de bytes extras e invariantes válidas de mundo/agentes. O limite é 64 MiB de payload. Contagens e dimensões são verificadas antes das principais alocações. SHA-256 detecta corrupção acidental; não autentica arquivos de terceiros.

Caches de BFS e revisões gráficas são reconstruídos. O relógio de apresentação (velocidade, fração de tick acumulada, backlog), câmera e seleção não são biologia e não são salvos. Carregar reinicia a velocidade em 1× no tick persistido. Para a mesma versão de regras/runtime, uma execução retomada preserva a trajetória em relação à execução contínua; isso é coberto por comparação integral dos bytes após novos ticks. Não há promessa de compatibilidade bit a bit entre versões futuras do runtime/algoritmos matemáticos.

`BinarySnapshotFile` grava arquivo temporário no mesmo diretório e o renomeia sobre o destino. Não substitui o arquivo existente se a serialização falha. A renomeação é atômica em sistemas locais usuais; não há garantia explícita de durabilidade contra perda abrupta de energia/fsync.

## SQLite

`SqliteSnapshotStore` implementa `ISnapshotStore` usando **Microsoft.Data.Sqlite 10.0.5**. Cada operação abre/fecha a conexão; pooling está desativado. Criação de schema e upsert usam transações; comandos usam parâmetros, inclusive o nome do slot. Timeout de contenção: cinco segundos.

Tabela `snapshots`: `slot` (chave primária), `seed`, `tick`, `saved_utc` e `payload` (BLOB). Schema versionado por `PRAGMA user_version = 1`; versões futuras desconhecidas são rejeitadas. Salvar substitui o conteúdo de um slot. O MVP não mantém histórico por tick, queries de agentes ou migração de versões antigas.

No jogo: `user://antworld.db`, resolvido pelo Godot conforme o sistema operacional. A interface atual não expõe save/load; a infraestrutura e o slot `smoke-test` permanecem operacionais. A serialização acontece entre ticks, sem concorrência sobre arrays. O adaptador aceita `ILogger<SqliteSnapshotStore>`; por padrão usa `NullLogger`. Falhas de UI são mostradas no HUD e no log Godot. O CLI usa o armazenamento binário de arquivo e stdout/stderr, sem dependência de SQLite.

Testes xUnit de persistência usam banco temporário real, reabertura, overwrite, slot com caracteres SQL, corrupção e slot inexistente. Esses testes foram executados e passaram usando o motor xUnit dentro do processo. O runner padrão VSTest foi impedido pelo sandbox de abrir seu socket local. O codec e os arquivos também passaram no verificador independente.

A restauração inicialmente resolveu `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, sinalizada pelo NuGet como vulnerável ([GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)). A referência explícita a **3.53.3** atualiza a biblioteca nativa e eliminou o erro de auditoria no build final; a auditoria não foi desativada no projeto.

## Estado de agentes no formato 4

Inclui configurações de espécie/casta/perfil comportamental, limites de desempenho, cursor do escalonador, prioridades coletivas, referência à rainha individual, relações sociais e quatro mapas esparsos de feromônios com data de atualização. Cada agente persiste sexo derivável por casta, fase, identidade familiar, assinatura química, saúde máxima/atual/recuperação, idade, necessidades, progresso de desenvolvimento e skills, carga/transportador, posição anterior, destino, tarefa, alvo, créditos de movimento e cooldowns.

Pais ausentes não precisam ser reconstruídos. Transporte exige referências recíprocas consistentes. Perfis, enums, recursos, capacidades numéricas e relações são validados. Índice espacial, buffers, seleção gráfica e BFS são reconstruídos. O arquivo de velocidades é configuração de apresentação carregada no início, independente do snapshot; ele não altera o estado biológico salvo.

O formato 4 inclui `MushroomSettings` e exige gerador 3: o calendário agora usa dias de três horas e a distribuição base inclui cogumelos. Cogumelos coletados, parcialmente consumidos ou renovados usam o diário esparso de recursos existente. Renovações derivam do tick persistido, seed e coordenadas do chunk; não há relógio oculto de crescimento. Na restauração, remoções de recursos precedem nascimentos para respeitar o limite por chunk durante a aplicação dos deltas. Ovos transportados persistem os IDs recíprocos e a posição do transportador.
