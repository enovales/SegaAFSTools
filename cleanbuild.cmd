@echo off
cls
.paket\paket.bootstrapper.exe
.paket\paket.exe update --force
build.cmd
