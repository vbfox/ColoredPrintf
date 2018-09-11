# Colored Printf

![color drops Logo](https://raw.githubusercontent.com/vbfox/ColoredPrintf/master/src/BlackFox.ColoredPrintf/Icon.png)

[![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/19hodvli3yq1andd/branch/master?svg=true)](https://ci.appveyor.com/project/vbfox/coloredprintf/branch/master)
[![Travis-CI Build status](https://travis-ci.org/vbfox/ColoredPrintf.svg?branch=master)](https://travis-ci.org/vbfox/ColoredPrintf)
[![VSTS Status](https://vbfox.visualstudio.com/ColoredPrintf/_apis/build/status/ColoredPrintf%20CI?branchName=master)](https://vbfox.visualstudio.com/ColoredPrintf/_build/latest?definitionId=5&branchName=master)
[![Nuget Package](https://img.shields.io/nuget/v/BlackFox.ColoredPrintf.svg)](https://www.nuget.org/packages/BlackFox.ColoredPrintf)

This library provide a replacement to F# `printf` function with color support.

The syntax to set the color inside the string is `$foreground;background[text]` where both foreground and background are optional.

## Examples

```fsharp
colorprintfn "Hello $red[world]."
colorprintfn "Hello $green[%s]." "user"
colorprintfn "$white[Progress]: $yellow[%.2f%%] (Eta $yellow[%i] minutes)" 42.33 5
colorprintfn "$white;blue[%s ]$black;white[%s ]$white;red[%s]" "La vie" "est" "belle"
```

Displays :

![Demo](doc/demo.png)

## Thanks

* [Newaita icon pack](https://github.com/cbrnix/Newaita) for the base of the icon (License: [CC BY-NC-SA 3.0]
