#!/bin/bash

./paket.sh restore || { exit $?; }

pushd src/BlackFox.ColoredPrintf.Build/
dotnet run $@
popd
