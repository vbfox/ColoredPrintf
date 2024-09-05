[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BlackFox.ColoredPrintf.ColoredPrintf

open System
open BlackFox.MasterOfFoo
open BlackFox.ColoredPrintf.ColoredWriter

type private ConsoleColoredPrinterEnv() =
    let mutable fg = ConsoleColor.White
    let mutable bg = ConsoleColor.Black
    let mutable colorDisabled = false
    let wrap f =
        if not colorDisabled then
            try f()
            with | _ -> colorDisabled <- true

    do
        wrap (fun _ ->
            fg <- Console.ForegroundColor
            bg <- Console.BackgroundColor)

    interface IColoredPrinterEnv with
        member __.Write (s: string) = Console.Write(s)
        member __.Foreground
            with get () = fg
            and set c =
                fg <- c
                wrap(fun _ -> Console.ForegroundColor <- c)
        member __.Background
            with get () = bg
            and set c =
                bg <- c
                wrap(fun _ -> Console.BackgroundColor <- c)

let consoleColorType = typeof<ConsoleColor>

/// Extracts a ConsoleColor instance from the element
///
/// Handle both sprintf style format where the type is available in ValueType and .NET style format where it isn't
let extractConsoleColor (s : PrintableElement) =
    if consoleColorType.Equals(s.ValueType) then
        // sprintf style: $"%A{ConsoleColor.Red}"
        Some (s.Value :?> ConsoleColor)
    else
        match s.Specifier with
        | Some specifier when specifier.TypeChar = 'P' ->
            if not (obj.ReferenceEquals(s.Value, null)) && consoleColorType.Equals(s.Value.GetType()) then
                // .NET style: $"{ConsoleColor.Red}"
                Some (s.Value :?> ConsoleColor)
            else
                None
        | _ -> None

type internal ColoredConsolePrintEnv<'Result>(env: IColoredPrinterEnv, k) =
    inherit PrintfEnv<unit, string, 'Result>()

    let state = getEmptyState env.Foreground env.Background

    override __.Finalize() : 'Result =
        state |> finish env
        k()

    override __.Write(s : PrintableElement) =
        match s.ElementType with
        | PrintableElementType.FromFormatSpecifier ->
            let consoleColor = if canAcceptColor(state) then extractConsoleColor s else None
            match consoleColor with
            | Some color -> state |> writeColor color
            | None -> state |> writeEscapedString (s.FormatAsPrintF())
        | _ ->
            state |> writeString env (s.FormatAsPrintF())

    override __.WriteT(s : string) =
        env.Write(s)

type ColorPrintFormat<'T> = Format<'T, unit, string, unit>

/// I'm so <c>.Net</c> very using <paramref name="s" />
/// You can also see <see cref="T:ColoredConsolePrintEnv`1" />.
/// <example>
/// <code>
/// override __.Finalize() : 'Result =
///         state |> finish env
///         k()
/// </code>
/// </example>
let foo = ()

let colorprintf<'T> (format: ColorPrintFormat<'T>) =
    doPrintfFromEnv format (ColoredConsolePrintEnv(ConsoleColoredPrinterEnv(), id))

let colorprintfn<'T> (format: ColorPrintFormat<'T>) =
    let writeLine () = Console.WriteLine()
    doPrintfFromEnv format (ColoredConsolePrintEnv(ConsoleColoredPrinterEnv(), writeLine))

