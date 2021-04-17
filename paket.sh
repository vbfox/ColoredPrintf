#!/bin/bash

dotnet tool restore
dotnet paket $@
