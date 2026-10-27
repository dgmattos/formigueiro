#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
export DOTNET_CLI_USE_MSBUILD_SERVER=0
dotnet build AntWorld.Core.sln -c Release --disable-build-servers -m:1
dotnet tests/AntWorld.Verification/bin/Release/net10.0/AntWorld.Verification.dll
mkdir -p artifacts
dotnet src/AntWorld.Cli/bin/Release/net10.0/AntWorld.Cli.dll --seed 42 --ticks 1500 --save artifacts/demo.antworld --svg artifacts/world.svg > artifacts/run.json
cat artifacts/run.json
