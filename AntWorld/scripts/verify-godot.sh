#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
GODOT4="${GODOT4:-$PWD/.tools/Godot_mono.app/Contents/MacOS/Godot}"
export ANTWORLD_DATA_DIR="$PWD/artifacts/godot-data"
dotnet build src/AntWorld.Game/AntWorld.Game.csproj --disable-build-servers -m:1
mkdir -p artifacts
"$GODOT4" --headless --path src/AntWorld.Game -- --prepare-assets > artifacts/godot-prepare.log 2>&1
"$GODOT4" --headless --path src/AntWorld.Game --log-file "$PWD/artifacts/godot-editor.log" --editor --import --quit > artifacts/godot-import.log 2>&1
"$GODOT4" --headless --path src/AntWorld.Game --log-file "$PWD/artifacts/godot-runtime.log" --quit-after 120 -- --smoke-test > artifacts/godot-smoke.log 2>&1
cat artifacts/godot-prepare.log artifacts/godot-import.log artifacts/godot-smoke.log
if grep -E 'ERROR:|SCRIPT ERROR:' artifacts/godot-prepare.log artifacts/godot-import.log artifacts/godot-smoke.log; then exit 1; fi
grep 'AntWorld: smoke test PASS' artifacts/godot-smoke.log
