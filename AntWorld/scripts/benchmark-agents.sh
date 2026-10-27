#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
dotnet build tests/AntWorld.Benchmarks -c Release --disable-build-servers -m:1
mkdir -p artifacts
dotnet tests/AntWorld.Benchmarks/bin/Release/net10.0/AntWorld.Benchmarks.dll --agents > artifacts/agent-benchmarks.json
cat artifacts/agent-benchmarks.json
