#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
antworld_godot="${GODOT4:-}"
if [ -z "$antworld_godot" ] && [ -x .tools/Godot_mono.app/Contents/MacOS/Godot ]; then antworld_godot="$PWD/.tools/Godot_mono.app/Contents/MacOS/Godot"; fi
if [ -z "$antworld_godot" ]; then antworld_godot="$(command -v godot || command -v godot-mono || true)"; fi
if [ -z "$antworld_godot" ]; then
  echo 'Godot .NET não encontrado. Defina GODOT4 com o caminho do executável.' >&2
  exit 2
fi
case "$("$antworld_godot" --version)" in
  *mono*) ;;
  *) echo 'Use a edição .NET/mono do Godot.' >&2; exit 2 ;;
esac
dotnet build src/AntWorld.Game/AntWorld.Game.csproj --disable-build-servers -m:1
"$antworld_godot" --headless --path src/AntWorld.Game -- --prepare-assets
"$antworld_godot" --headless --path src/AntWorld.Game --import
exec "$antworld_godot" --path src/AntWorld.Game
