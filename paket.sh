#!/bin/bash

PAKET_VERSION=3.21.4

function dotnet { if test "$OS" = "Windows_NT"; then $@; else mono $@; fi }

dotnet .paket/paket.bootstrapper.exe -s $PAKET_VERSION  || { exit $?; }
dotnet .paket/paket.exe $@
