#!/bin/bash

function dotnet { if test "$OS" = "Windows_NT"; then $@; else mono $@; fi }

./paket.sh restore || { exit $?; }
dotnet packages/FAKE/tools/FAKE.exe $@ --fsiargs build/build.fsx
