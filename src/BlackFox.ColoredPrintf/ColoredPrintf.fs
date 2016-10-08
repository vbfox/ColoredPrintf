namespace BlackFox.ColoredPrintf

open System

type ColoredString = { Raw : string }

type ColorIdentifier =
    | NoColor
    | Reset
    | Color of color : ConsoleColor

type IColoredPrinterEnv =
    abstract Write : string -> unit
    abstract Foreground : ConsoleColor with get,set
    abstract Background : ConsoleColor with get,set

open System.Text

// This is $red[%s]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ColoredString =
    type WriterStatus = | Normal | Foreground | Background | Escaping
    type WriterState = {
        mutable Colors: (ConsoleColor option * ConsoleColor option) list
        mutable Status: WriterStatus
        CurrentText: StringBuilder
        mutable CurrentForeground: ConsoleColor option
    }

    let getEmptyState () = {
        Colors = []
        Status = WriterStatus.Normal
        CurrentText = StringBuilder()
        CurrentForeground = None
    }

    let colorNameToColor (name: string) = Some(ConsoleColor.Red)

    module private StateHelpers =
        let inline clearText (state: WriterState) = ignore(state.CurrentText.Clear())
        let inline appendChar (c: char) (state: WriterState) = ignore(state.CurrentText.Append(c))    

        let inline writeCurrentTextToEnv (env: IColoredPrinterEnv) (state: WriterState) =
            if state.CurrentText.Length > 0 then
                env.Write (state.CurrentText.ToString())
                state |> clearText

        let inline getColor (state: WriterState) = 
            let colorText = state.CurrentText.ToString()
            state |> clearText
            colorNameToColor colorText

    open StateHelpers

    let writeChar (env: IColoredPrinterEnv) (state: WriterState) (c: char) =
        match state.Status with
        | WriterStatus.Normal when c = '$' ->
            writeCurrentTextToEnv env state
            state.Status <- WriterStatus.Foreground
        | WriterStatus.Normal when c = ']' -> ()
        | WriterStatus.Normal when c = '\\' -> state.Status <- WriterStatus.Escaping
        | WriterStatus.Normal -> state |> appendChar c
        | WriterStatus.Escaping when c = '$' || c = ']' -> state |> appendChar c
        | WriterStatus.Escaping ->
            state |> appendChar '\\'
            state |> appendChar c
        | WriterStatus.Foreground when c = ';' ->
            match getColor state with
            | Some c -> state.CurrentForeground <- Some c
            | None -> ()
            state.Status <- WriterStatus.Background
        | WriterStatus.Foreground when c = '[' ->
            match getColor state with
            | Some c ->
                env.Foreground <- c
                state.Colors <- (Some c, None) :: state.Colors
            | None -> ()
            state.Status <- WriterStatus.Normal
        | WriterStatus.Foreground -> state |> appendChar c
        | WriterStatus.Background when c = '[' ->
            let fg = state.CurrentForeground
            state.CurrentForeground <- None
            match fg with | Some c -> env.Foreground <- c | None -> ()
            let bg = getColor state
            match bg with | Some c -> env.Background <- c | None -> ()
            state.Status <- WriterStatus.Normal            
        | WriterStatus.Background -> state |> appendChar c
       
    let finish (env: IColoredPrinterEnv) (state: WriterState) =
        match state.Status with
        | WriterStatus.Normal ->
            writeCurrentTextToEnv env state
        | WriterStatus.Escaping ->
            state |> appendChar '\\'
            writeCurrentTextToEnv env state
        | WriterStatus.Foreground -> ()
        | WriterStatus.Background -> ()
            
    let writeCompleteString (env: IColoredPrinterEnv) (s: string) =
        let state = getEmptyState ()
        for i in 0..s.Length-1 do
            writeChar env state (s.[i])
        finish env state
(*
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ColoredString =
    open System
    open System.Text

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
*)