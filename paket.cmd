@echo off
setlocal

set PAKET_VERSION=3.21.4

.paket\paket.bootstrapper.exe -s %PAKET_VERSION%
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe %*
if errorlevel 1 (
  exit /b %errorlevel%
)
