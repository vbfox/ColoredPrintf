module BlackFox.ColoredPrintf.ColorStrings

open System

let private colorIdentifiers =
    [
        for color in Enum.GetValues(typedefof<ConsoleColor>) |> Seq.cast<ConsoleColor> do
            yield color.ToString().ToLowerInvariant(), color
    ]
    |> Map.ofSeq

let colorNameToColor (name: string) = colorIdentifiers |> Map.tryFind(name.ToLowerInvariant())