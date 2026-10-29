# Exportação para Windows x64

O preset `Windows Desktop` gera um executável Windows x64, um pacote `.pck` e os assemblies/runtime .NET necessários. Distribuir a pasta inteira, não somente o `.exe`. O modo janela segue `settings.json`.

## Pré-requisitos

- SDK .NET 10 e Godot **4.6.3 .NET**.
- Export templates **4.6.3 .NET**, instalados pelo menu **Editor → Manage Export Templates**, usando o pacote oficial `Godot_v4.6.3-stable_mono_export_templates.tpz`.
- Acesso ao NuGet para restaurar dependências e runtime `win-x64`, se não estiverem no cache.
- Python 3 para empacotar pelo script; a exportação pelo editor não exige Python.

O editor de `.tools` usa modo autocontido. Seus templates ficam em `.tools/editor_data/export_templates/4.6.3.stable.mono/`. Templates instalados em outro editor/perfil não ficam automaticamente disponíveis nele.

## Gerar pacote portátil

Na raiz do repositório:

```sh
python3 AntWorld/scripts/export-windows.py
```

No Windows, informe o executável do editor .NET instalado:

```powershell
py AntWorld/scripts/export-windows.py --godot 'C:\Godot\Godot_v4.6.3-stable_mono_win64.exe'
```

Alternativa: abrir `AntWorld/src/AntWorld.Game/project.godot` no editor .NET e usar **Project → Export → Windows Desktop → Export Project**, em modo release.

O script compila, importa e exporta numa pasta temporária. Verifica `.exe`, `.pck`, assemblies, runtime .NET e biblioteca SQLite Windows antes de criar `AntWorld/artifacts/AntWorld-Windows-x64.zip`. Só substitui um ZIP anterior após sucesso. O log fica em `AntWorld/artifacts/windows-export.log`.

Configuração e manifestos JSON são incluídos no pacote. Os leitores usam `Godot.FileAccess`, compatível com PCK. O `settings.json` exportado fica interno ao pacote; editar a configuração na solução exige nova exportação.

## Validar no Windows

Extrair toda a pasta e executar `AntWorld.exe`. Conferir terreno, sprites, colônias, pausa, velocidades, câmera, inspeção e logs. Para o teste automatizado:

```powershell
.\AntWorld.exe --headless --quit-after 150 -- --smoke-test
```

Conferir todos os resultados PASS no log. O banco é salvo no `user://` do Godot, fora da pasta de instalação.

## Limitações verificadas nesta entrega

O build e a importação foram executados. A tentativa real de exportação reconheceu o preset, mas falhou porque faltam os templates `windows_debug_x86_64.exe` e `windows_release_x86_64.exe`. O terminal não resolveu `github.com` para baixá-los. Não foi gerado nem testado um executável Windows, nem compilado um instalador.

Após gerar e validar o pacote portátil, é possível usar Inno Setup no Windows para criar um instalador com todos os arquivos exportados. O preset não configura assinatura digital.

Referências: [exportação Windows](https://docs.godotengine.org/en/4.6/tutorials/export/exporting_for_windows.html) e [exportação de projetos](https://docs.godotengine.org/en/4.6/tutorials/export/exporting_projects.html).
