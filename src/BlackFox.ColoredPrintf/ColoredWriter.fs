module BlackFox.ColoredPrintf.ColoredWriter

open System
open System.Text

type IColoredPrinterEnv =
    abstract Write : string -> unit
    abstract Foreground : ConsoleColor with get,set
    abstract Background : ConsoleColor with get,set

type WriterStatus = | Normal | Foreground | Background | Escaping
type WriterState = {
    mutable Colors: (ConsoleColor * ConsoleColor) list
    mutable Status: WriterStatus
    CurrentText: StringBuilder
    mutable CurrentColor: ConsoleColor option
    mutable WipForeground: ConsoleColor option
}

let getEmptyState (foreground: ConsoleColor) (background: ConsoleColor) = {
    Colors = [foreground, background]
    Status = WriterStatus.Normal
    CurrentColor = None
    CurrentText = StringBuilder()
    WipForeground = None
}

module private StateHelpers =
    open ColorStrings
    let inline clearText (state: WriterState) = ignore(state.CurrentText.Clear())
    let inline appendChar (c: char) (state: WriterState) = ignore(state.CurrentText.Append(c))
    let inline appendString (s: string) (state: WriterState) =
        for i in 0..s.Length-1 do
            state |> appendChar (s.[i])

    /// Write the currently accumulated text if any and clear the text state
    let inline writeCurrentTextToEnv (env: IColoredPrinterEnv) (state: WriterState) =
        if state.CurrentText.Length > 0 then
            env.Write (state.CurrentText.ToString())
            state |> clearText

    let inline getColor (state: WriterState) =
        match state.CurrentColor with
        | Some _ as c ->
            state.CurrentColor <- None
            state |> clearText
            c
        | None ->
            let colorText = state.CurrentText.ToString()
            state |> clearText
            colorNameToColor colorText

open StateHelpers

/// Get if the current state can accept a color via 'writeColor'
let inline canAcceptColor (state: WriterState) =
    match state.Status with
    | WriterStatus.Foreground -> state.CurrentColor.IsNone && state.CurrentText.Length = 0
    | WriterStatus.Background -> state.CurrentColor.IsNone && state.CurrentText.Length = 0
    | _ -> false

/// Set the current (Foreground or Background) color
let inline writeColor (color: ConsoleColor) (state: WriterState) =
    if not (canAcceptColor state) then
        failwith "Can't accept a color specification in the current state"
    
    state.CurrentColor <- Some color

/// Add a character to the current state (Can contain color markers)
let inline writeChar (env: IColoredPrinterEnv) (c: char) (state: WriterState) =
    match state.Status with
    | WriterStatus.Normal when c = '$' ->
        writeCurrentTextToEnv env state
        state.Status <- WriterStatus.Foreground
    | WriterStatus.Normal when c = ']' ->
        match state.Colors with
        | [] -> failwith "Unexpected, no colors in stack"
        | [_] -> state |> appendChar c
        | (currentFg, currentBg) :: (previousFg, previousBg) :: rest ->
            writeCurrentTextToEnv env state
            state.Colors <- (previousFg, previousBg) :: rest
            if currentFg <> previousFg then env.Foreground <- previousFg
            if currentBg <> previousBg then env.Background <- previousBg
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
        | Some c -> state.WipForeground <- Some c
        | None -> ()
        state.Status <- WriterStatus.Background
    | WriterStatus.Foreground when c = '[' ->
        let (currentFg, currentBg) = state.Colors.Head
        match getColor state with
        | Some c ->
            if currentFg <> c then env.Foreground <- c
            state.Colors <- (c, currentBg) :: state.Colors
        | None ->
            state.Colors <- state.Colors.Head :: state.Colors
        state.Status <- WriterStatus.Normal
    | WriterStatus.Foreground -> state |> appendChar c
    | WriterStatus.Background when c = '[' ->
        let (currentFg, currentBg) = state.Colors.Head
            
        let fg = defaultArg state.WipForeground currentFg
        let bg = defaultArg (getColor state) currentBg
            
        state.WipForeground <- None

        if currentFg <> fg then env.Foreground <- fg
        if currentBg <> bg then env.Background <- bg

        state.Colors <- (fg, bg) :: state.Colors
        state.Status <- WriterStatus.Normal            
    | WriterStatus.Background -> state |> appendChar c
       
/// Add a string to the current state (Can contain color markers)
let inline writeString (env: IColoredPrinterEnv) (s: string) (state: WriterState) =
    for i in 0..s.Length-1 do
        state |> writeChar env (s.[i])

/// Add a string to the current state verbatim (Color markers are ignored and will be present in the output)
let inline writeEscapedString (s: string) (state: WriterState) =
    match state.Status with
    | WriterStatus.Normal ->
        state |> appendString s
    | WriterStatus.Escaping -> 
        state |> appendChar '\\'
        state |> appendString s
    | WriterStatus.Foreground -> ()
    | WriterStatus.Background -> ()

let inline finish (env: IColoredPrinterEnv) (state: WriterState) =
    match state.Status with
    | WriterStatus.Normal ->
        writeCurrentTextToEnv env state
    | WriterStatus.Escaping ->
        state |> appendChar '\\'
        writeCurrentTextToEnv env state
    | WriterStatus.Foreground -> ()
    | WriterStatus.Background -> ()

    let (initialFg, initialBg) = state.Colors |> List.last
    if initialFg <> env.Foreground then env.Foreground <- initialFg
    if initialBg <> env.Background then env.Background <- initialBg
            
let writeCompleteString (env: IColoredPrinterEnv) (s: string) =
    let state = getEmptyState (env.Foreground) (env.Background)
    state |> writeString env s
    state |> finish env