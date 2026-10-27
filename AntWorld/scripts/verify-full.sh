#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
export DOTNET_CLI_USE_MSBUILD_SERVER=0
dotnet build AntWorld.sln --disable-build-servers -m:1
dotnet tests/AntWorld.XunitRunner/bin/Debug/net10.0/AntWorld.XunitRunner.dll
