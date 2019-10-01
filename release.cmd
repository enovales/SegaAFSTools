@echo off
cls

.paket\paket.bootstrapper.exe prerelease
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe restore -v
if errorlevel 1 (
  exit /b %errorlevel%
)

packages\FAKE\tools\FAKE.exe build.fsx "target=Release" "NugetKey=%NUGETKEY%" "github-user=%GITHUBUSER%"  "github-pw=%GITHUBPW%"

rem packages\build\FAKE\tools\FAKE.exe build.fsx "target=Release" "NugetKey=%NUGETKEY%"
