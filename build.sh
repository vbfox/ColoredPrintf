#!/bin/bash

./paket.sh restore || { exit $?; }

dotnet run --project src/BlackFox.ColoredPrintf.Build/BlackFox.ColoredPrintf.Build.fsproj -- $@
