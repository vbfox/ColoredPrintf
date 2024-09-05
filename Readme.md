# Colored Printf

![color drops Logo](https://raw.githubusercontent.com/vbfox/ColoredPrintf/master/src/BlackFox.ColoredPrintf/Icon.png)

[![Github Actions Status](https://github.com/vbfox/ColoredPrintf/actions/workflows/main.yml/badge.svg)](https://github.com/vbfox/ColoredPrintf/actions/workflows/main.yml?query=branch%3Amaster)
[![Nuget Package](https://img.shields.io/nuget/v/BlackFox.ColoredPrintf.svg)](https://www.nuget.org/packages/BlackFox.ColoredPrintf)

This library provide a replacement to F# `printf` function with color support.

The syntax to set the color inside the string is `$foreground;background[text]` where both foreground and background
are optional.

The supported colors are the one from the [System.ConsoleColor](https://docs.microsoft.com/en-us/dotnet/api/system.consolecolor) enum.

## Examples

```fsharp
// Change the color of part of a string
colorprintfn "$red[Hello] $white;red[world]."

// Use sprintf syntax
colorprintfn "Hello $green[%s]: %s" "user" "Welcome to color!"
colorprintfn "$white[Progress]: $yellow[%.2f%%] (Eta $yellow[%i] minutes)" 42.33 5

// Use interpolated strings
let life = "La vie"
let is_ = "est"
colorprintfn $"""$white;blue[%s{life} ]$black;white[{is_} ]$white;red[{"belle"}]"""

// Specify the colors from variables
let logColor = ConsoleColor.Yellow
colorprintfn "result: $%A[Hello world]" logColor
colorprintfn $"result: ${logColor}[Hello world]"
```

Displays :

![Demo](doc/demo.png)

## Thanks

* [Newaita icon pack](https://github.com/cbrnix/Newaita) for the base of the icon (License: [CC BY-NC-SA 3.0]
