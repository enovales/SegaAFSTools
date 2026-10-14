#!/usr/bin/env bash

set -eu
set -o pipefail

# Build the solution
dotnet build SegaAFSTools.sln --configuration Release

# Run tests
dotnet test tests/SegaAFSTools.Tests/SegaAFSTools.Tests.fsproj --configuration Release
