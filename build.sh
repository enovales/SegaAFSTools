#!/usr/bin/env bash

set -eu
set -o pipefail

# Build the solution
dotnet build SegaAFSTools.sln --configuration Release

# Run tests (Expecto tests need to be executed directly)
dotnet run --project tests/SegaAFSTools.Tests/SegaAFSTools.Tests.fsproj --configuration Release
