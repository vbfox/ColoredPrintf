@echo off

call "paket.cmd" restore
if errorlevel 1 (
  exit /b %errorlevel%
)

packages\FAKE\tools\FAKE.exe build\build.fsx %*
