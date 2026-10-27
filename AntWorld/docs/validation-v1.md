# Registro histórico — gerador/snapshot v1

Este registro descreve a primeira entrega. Os artefatos em `artifacts/` são regenerados pelas verificações atuais e podem não corresponder a estes resultados históricos.

# Validação executada

Data: 26/10/2026. Ambiente: macOS arm64, SDK .NET 10.0.201, runtime 10.0.5. O workspace começou vazio. Godot não estava instalado; a edição oficial **4.6.3.stable.mono.official.7d41c59c4** foi obtida e colocada em `.tools/Godot_mono.app`, sem instalar em `/Applications`.

## Resultados finais

| Verificação | Resultado |
|---|---|
| `sh scripts/verify-core.sh` | Build Release aprovado; quatro grupos de cenários aprovados; CLI executado |
| `sh scripts/verify-full.sh` | Todos os 13 projetos compilados; zero avisos/erros; cinco testes xUnit aprovados |
| SQLite real via Microsoft.Data.Sqlite | Save/load, reabertura, overwrite, slot com caracteres SQL, slot ausente e corrupção aprovados |
| Snapshot binário | Round-trip idêntico, rejeição de corrupção/versão/tamanho e continuação determinística aprovados |
| Geração e genética | Cinco seeds, cinco biomas, camadas, cavernas, recursos e 10.000 mutações validados |
| Simulação | FPS, pausa, backlog, overflow de tempo, reprodução, fundação, clima, combate e extinção sem recursos aprovados |
| Godot, importação | Assets importados; houve erros ambientais de acesso a diretórios/certificados/sockets |
| Godot, cena headless | Cena inicializada; teste integrado de velocidade, pausa, SQLite, restauração, HUD e feromônios aprovado; processo retornou 0, com erros ambientais no início |
| Janela gráfica | Tentativa terminou com código 134 antes de criar log/captura; aparência e interação visual não validadas |
| Windows/Linux e exportação | Não executados |

A verificação gráfica estrita `verify-godot.sh` retorna falha diante de qualquer `ERROR:` da engine. **Ela não foi considerada aprovada nesta sessão**, apesar do sucesso do teste de aplicação em modo headless.

## Evidências de execução

`artifacts/run.json` contém a execução Release da seed 42 por 1.500 ticks: 134 formigas e três colônias históricas, incluindo uma filha Musgo de geração 1. Duração observada: aproximadamente 448 ms / 3.348 ticks/s, sem renderização. Esse valor não garante 1000× com populações maiores ou com janela aberta.

Comparação adicional, em 1.500 ticks e configuração padrão:

| Seed | Formigas | Observação |
|---:|---:|---|
| 1 | 122 | Rainha Brasa morta; Musgo viva |
| 2 | 61 | Brasa viva; rainha Musgo morta |
| 3 | 121 | Fundação de colônia Brasa de geração 1; rainha filha já morta |
| 42 | 134 | Colônia Musgo de geração 1 ativa |
| 12345 | 88 | Rainha Brasa morta; Musgo viva |

Esses resultados demonstram trajetórias diferentes, não equilíbrio estatístico entre espécies. O spawn associa espécie e posição; comparação científica exigiria repetir também com posições trocadas e mais seeds.

`artifacts/world.svg` mostra o estado real da seed 42, em visualização estática independente da engine. Não é uma captura da interface Godot. `artifacts/demo.antworld` guarda o estado completo correspondente.

Teste integrado Godot:

```sh
ANTWORLD_DATA_DIR="$PWD/artifacts/godot-data" \
  .tools/Godot_mono.app/Contents/MacOS/Godot \
  --headless --path src/AntWorld.Game \
  --log-file "$PWD/artifacts/godot-integration.log" \
  --quit-after 120 -- --smoke-test
```

O log contém `AntWorld: smoke test PASS`. Consulta independente ao banco confirmou `smoke-test|12345|100|769860`: seed 12345, tick 100, payload de 769.860 bytes.

## Impedimentos encontrados e tratamento

1. O acesso inicial ao NuGet falhou por DNS e os pacotes não estavam no cache. O núcleo foi validado sem pacotes externos. Depois que as dependências ficaram disponíveis, o build completo e os testes reais foram executados.
2. A biblioteca transitiva SQLitePCLRaw.lib.e_sqlite3 2.1.11 gerou NU1903. A versão nativa foi fixada em 3.53.3; o build final passou sem desativar auditoria.
3. `dotnet test`/VSTest tentou abrir um socket local e recebeu `SocketException (13): Permission denied`. O executável `AntWorld.XunitRunner` usa o motor oficial xUnit no próprio processo, com descoberta e mensagens de resultado reais. Resultado: cinco testes, zero falhas, ignorados ou erros.
4. O editor Godot inseriu `net8.0` no `.csproj` quando o framework estava apenas em `Directory.Build.props`. O projeto gráfico agora declara `net10.0` explicitamente. A recompilação passou.
5. Godot tentou criar dados/configurações/cache em `~/Library` e acessar certificados do sistema, bloqueados pelo sandbox. A comunicação editor–IDE também encontrou a restrição de socket. O SQLite da aplicação foi direcionado a `artifacts/godot-data` por variável de ambiente. Os erros remanescentes da engine foram preservados nos logs.
6. O processo gráfico terminou com código 134; não foi produzida imagem da janela. A revisão visual e testes de input em janela precisam ser feitos no terminal normal do macOS com `sh scripts/run-game.sh`.

Não foram utilizados mocks de Godot, SQLite ou xUnit. Não foram alteradas permissões do sistema, HOME ou diretórios protegidos para contornar o sandbox. Redis, exportação de executáveis e testes nas demais plataformas permanecem fora desta validação.
