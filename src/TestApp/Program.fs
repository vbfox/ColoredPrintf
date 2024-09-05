open System
open BlackFox.ColoredPrintf

[<EntryPoint>]
let main argv =
    colorprintfn "Hello $red[world]."
    colorprintfn "Hello $green[%s]." "user"
    colorprintfn "$white[Progress]: $yellow[%.2f%%] (Eta $yellow[%i] minutes)" 42.33 5
    colorprintfn "$white;blue[%s ]$black;white[%s ]$white;red[%s]" "La vie" "est" "belle"

    let life = "La vie"
    let is_ = "est"
    colorprintfn $"""$white;blue[%s{life} ]$black;white[{is_} ]$white;red[{"belle"}]"""

    colorprintfn "Hello $%A;%A[world] !" ConsoleColor.Magenta ConsoleColor.Yellow

    colorprintfn $"Hello $%A{ConsoleColor.Magenta};{ConsoleColor.Yellow}[world] !"
    ignore(Console.ReadLine())
    0 // return an integer exit code
