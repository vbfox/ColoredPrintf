# Colored Printf

![color drops Logo](https://raw.githubusercontent.com/vbfox/ColoredPrintf/master/src/BlackFox.ColoredPrintf/Icon.png)

[![Github Actions Status](https://github.com/vbfox/ColoredPrintf/actions/workflows/main.yml/badge.svg)](https://github.com/vbfox/ColoredPrintf/actions/workflows/main.yml?query=branch%3Amaster)
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
