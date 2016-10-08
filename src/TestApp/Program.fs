open System
open BlackFox.ColoredPrintf

[<EntryPoint>]
let main argv = 
    colorprintfn "$white;blue[%s ]$black;white[%s ]$white;red[%s]" "La vie" "est" "belle"
    ignore(Console.ReadLine())
    0 // return an integer exit code
