@echo off

call ./paket.cmd restore -s
if errorlevel 1 (
  exit /b %errorlevel%
)

dotnet run --project src\BlackFox.ColoredPrintf.Build\BlackFox.ColoredPrintf.Build.fsproj %*
