@echo off
cls
.paket\paket.exe update --force
build.cmd
