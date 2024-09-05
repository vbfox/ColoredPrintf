open System
open BlackFox.ColoredPrintf

[<EntryPoint>]
let main argv =
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

    0
