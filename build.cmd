@echo off

# Build the solution
dotnet build SegaAFSTools.sln --configuration Release

# Run tests
dotnet test tests/SegaAFSTools.Tests/SegaAFSTools.Tests.fsproj --configuration Release
