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

    let private colorIdentifiers =
        [
            for color in Enum.GetValues(typedefof<ConsoleColor>) |> Seq.cast<ConsoleColor> do
                yield color.ToString().ToLowerInvariant(), color
        ]
        |> Map.ofSeq

    let inline private colorNameToColor (name: string) = colorIdentifiers |> Map.tryFind(name.ToLowerInvariant())

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

    let writeChar (env: IColoredPrinterEnv) (c: char) (state: WriterState) =
        match state.Status with
        | WriterStatus.Normal when c = '$' ->
            writeCurrentTextToEnv env state
            state.Status <- WriterStatus.Foreground
        | WriterStatus.Normal when c = ']' -> ()
        | WriterStatus.Normal when c = '\\' -> state.Status <- WriterStatus.Escaping
        | WriterStatus.Normal -> state |> appendChar c
        | WriterStatus.Escaping when c = '$' || c = ']' ->
            state |> appendChar c
            state.Status <- WriterStatus.Normal
        | WriterStatus.Escaping ->
            state |> appendChar '\\'
            state |> appendChar c
            state.Status <- WriterStatus.Normal
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
       
    let writeString (env: IColoredPrinterEnv) (s: string) (state: WriterState) =
        for i in 0..s.Length-1 do
            state |> writeChar env (s.[i])

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
        let initialFg = env.Foreground
        let initialBg = env.Background
        
        let state = getEmptyState ()
        state |> writeString env s
        
        state |> finish env
        if initialFg <> env.Foreground then env.Foreground <- initialFg
        if initialBg <> env.Background then env.Background <- initialBg
