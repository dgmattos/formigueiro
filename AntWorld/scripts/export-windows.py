#!/usr/bin/env python3
"""Exporta o jogo completo para Windows x64; requer Godot .NET e templates da mesma versão."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tempfile


def main():
    root = Path(__file__).resolve().parents[1]
    default = root / ".tools/Godot_mono.app/Contents/MacOS/Godot"
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--godot", default=os.environ.get("GODOT4", str(default)))
    args = parser.parse_args()
    godot = shutil.which(args.godot)
    if not godot:
        parser.error("Godot .NET não encontrado. Informe --godot CAMINHO_DO_EXECUTAVEL.")
    version = subprocess.check_output([godot, "--version"], text=True).strip()
    if not version.startswith("4.6.3.") or ".mono." not in version:
        parser.error(f"Requer Godot 4.6.3 .NET e templates 4.6.3 .NET; encontrado: {version}")
    if not shutil.which("dotnet"):
        parser.error("Instale o SDK .NET 10 antes de exportar.")
    project = root / "src/AntWorld.Game"
    artifacts = root / "artifacts"
    artifacts.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="windows-export-", dir=artifacts) as temporary:
        temporary = Path(temporary)
        output = temporary / "AntWorld-Windows-x64"
        output.mkdir()
        with (artifacts / "windows-export.log").open("w") as log:
            commands = [
                ["dotnet", "build", str(project / "AntWorld.Game.csproj"), "--disable-build-servers", "-m:1"],
                [godot, "--headless", "--path", str(project), "--editor", "--import", "--quit"],
                [godot, "--headless", "--path", str(project), "--export-release", "Windows Desktop", str(output / "AntWorld.exe")],
            ]
            for command in commands:
                subprocess.run(command, cwd=root, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=300)
        required = ["AntWorld.exe", "AntWorld.pck", "AntWorld.Game.dll", "GodotSharp.dll", "coreclr.dll", "e_sqlite3.dll"]
        for name in required:
            if not any(output.rglob(name)):
                raise RuntimeError(f"Exportação incompleta: {name} ausente. Consulte artifacts/windows-export.log.")
        (output / "LEIA-ME.txt").write_text(
            "AntWorld - Windows x64\n\nExtraia toda a pasta e execute AntWorld.exe.\n"
            "Mantenha o arquivo .pck e a pasta data_* junto ao executável.\n"
            "O runtime .NET está incluído no pacote.\n"
            "Câmera: WASD / botão direito. Zoom: rolagem. Tempo: espaço e 1-3.\n"
            "Clique para inspecionar; Esc fecha o card.\n",
            encoding="utf-8",
        )
        archive = shutil.make_archive(str(temporary / "AntWorld-Windows-x64"), "zip", temporary, output.name)
        destination = artifacts / "AntWorld-Windows-x64.zip"
        Path(archive).replace(destination)
        print(f"Pacote gerado: {destination}\nAinda requer validação de execução em Windows.")


if __name__ == "__main__":
    try:
        main()
    except (subprocess.CalledProcessError, subprocess.TimeoutExpired, RuntimeError) as error:
        raise SystemExit(f"Exportação não concluída: {error}\nConsulte AntWorld/artifacts/windows-export.log.") from error
