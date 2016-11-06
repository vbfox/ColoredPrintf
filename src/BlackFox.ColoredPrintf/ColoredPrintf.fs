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

type internal ColoredConsolePrintEnv<'Result>(env: IColoredPrinterEnv, k) =
    inherit PrintfEnv<unit, string, 'Result>()

    let state = getEmptyState env.Foreground env.Background

    override __.Finalize() : 'Result =
        state |> finish env
        k()

    override __.Write(s : PrintableElement) =
        match s.ElementType with
        | PrintableElementType.FromFormatSpecifier ->
            if typeof<ConsoleColor>.Equals(s.ValueType) && canAcceptColor(state) then
                state |> writeColor (s.Value :?> ConsoleColor)
            else
                state |> writeEscapedString (s.FormatAsPrintF())
        | _ ->
            state |> writeString env (s.FormatAsPrintF())
        
    override __.WriteT(s : string) =
        env.Write(s)

type ColorPrintFormat<'T> = Format<'T, unit, string, unit>

let colorprintf<'T> (format: ColorPrintFormat<'T>) =
    doPrintfFromEnv format (ColoredConsolePrintEnv(ConsoleColoredPrinterEnv(), id))

let colorprintfn<'T> (format: ColorPrintFormat<'T>) =
    let writeLine () = Console.WriteLine()
    doPrintfFromEnv format (ColoredConsolePrintEnv(ConsoleColoredPrinterEnv(), writeLine))
