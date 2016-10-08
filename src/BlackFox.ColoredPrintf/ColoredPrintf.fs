namespace BlackFox.ColoredPrintf

type coloredString = { Raw : string }

[<RequireQualifiedAccess>]
module ColoredString =
    open System
    open System.Text

    type private ColorIdentifier =
        | NoColor
        | Reset
        | Color of color : ConsoleColor

    type private onCharParsed<'st> = 'st -> char -> 'st
    type private onColorParsed<'st> = 'st -> ColorIdentifier * ColorIdentifier -> 'st

    module Option =
        let orDefault default' option = defaultArg option default'

    let private colorIdentifiers =
        [
            yield "reset", Reset
            for color in Enum.GetValues(typedefof<ConsoleColor>) |> Seq.cast<ConsoleColor> do
                yield color.ToString().ToLowerInvariant(), Color(color)
        ]
        |> Map.ofSeq

    let private tryParseColor (s:string) =
        colorIdentifiers |> Map.tryFind (s.ToLowerInvariant()) |> Option.orDefault NoColor

    let private parseColorCodes (s:string) =
        let split = s.Split(';')

        let foreground = if split.Length >= 1 then tryParseColor split.[0] else NoColor
        let background = if split.Length >= 2 then tryParseColor split.[1] else NoColor

        (foreground, background)

    
    let private fold (onChar: onCharParsed<'st>) (onColor: onColorParsed<'st>) (st:'st) coloredString = 
        let foldFunc (st, escCount, content) c =
            match escCount with
            | 0 ->
                match c with
                | '^' -> (st, 1, "")
                | _ -> (onChar st c, 0, "")
            | 1 ->
                match c with
                | '[' -> (st, 2, "")
                | _ -> (onChar st c, 0, "")
            | 2 ->
                match c with
                | ']' -> (onColor st (parseColorCodes content), 0, "")
                | _ -> (st, 2, content + (string)c)
            | _ ->
                failwith "Impossible escape count"

        let (newSt, _, _) = coloredString.Raw |> Seq.fold foldFunc (st, 0, "")
        newSt

    
    let length = fold (fun x _ -> x + 1) (fun x _ -> x) 0

    let create raw = { Raw = raw }

    let toString coloredString = 
        let builder = coloredString |> fold (fun (b:StringBuilder) c -> b.Append c) (fun b _ -> b) (new StringBuilder())
        builder.ToString()

    let private writeCore (inner: (ColorIdentifier -> ColorIdentifier -> unit) -> unit) =
        let initalForeground = Console.ForegroundColor
        let initialBackground = Console.BackgroundColor

        let setForeground = function
            | NoColor -> ()
            | Reset -> Console.ForegroundColor <- initalForeground
            | Color color -> Console.ForegroundColor <- color

        let setBackground = function
            | NoColor -> ()
            | Reset -> Console.BackgroundColor <- initialBackground
            | Color color -> Console.BackgroundColor <- color

        let setColors foreground background =
            setForeground foreground
            setBackground background
                        
        inner setColors

        setColors Reset Reset

    let writeToConsole coloredString = 
        let doWrite setColors =
            fold
                (fun _ c -> Console.Write(c))
                (fun _ (foreground, background) -> setColors foreground background)
                ()
                coloredString

        writeCore doWrite

    let writeConsoleStartingAtPosition position coloredString =
        let doWrite setColors =
            let writeChar i (c:char) =
                if i < position then
                    i + 1
                else
                    Console.Write c
                    i + 1

            let setColors' i (foreground, background) =
                setColors foreground background
                i

            fold writeChar setColors' 0 coloredString |> ignore

        writeCore doWrite

[<AutoOpen>]
module ColoredStringAutoOpen =
    open System

    let coloredWrite = ColoredString.create >> ColoredString.writeToConsole

    let coloredWriteLine s =
        coloredWrite s
        Console.WriteLine()