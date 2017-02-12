module BlackFox.ColoredPrintf.Tests.TestWriter

open System
open System.Collections.Generic
open BlackFox.ColoredPrintf
open BlackFox.MasterOfFoo
open Expecto

type Operation =
    | Write of string
    | SetForeground of ConsoleColor
    | SetBackground of ConsoleColor

type TestWriterImpl(initialFg: ConsoleColor, initialBg: ConsoleColor) =
    let mutable fg = initialFg
    let mutable bg = initialBg
    let operations = List<Operation>()

    interface ColoredWriter.IColoredPrinterEnv with
        member __.Write (s: string) =
            operations.Add(Operation.Write(s))
        member __.Foreground
            with get () = fg
            and set c =
                fg <- c
                operations.Add(Operation.SetForeground(c))
        member __.Background
            with get () = bg
            and set c =
                bg <- c
                operations.Add(Operation.SetBackground(c))

    member __.Verify(expectedOperations:Operation seq) =
        try
            let mutable i = 0
            for expectedOperation in expectedOperations do
                Expect.isLessThanOrEqual (i+1) (operations.Count) (sprintf "%ith operation exists (%A)" (i+1) expectedOperation)
                Expect.equal expectedOperation (operations.[i]) (sprintf "Operation %i: The two operations are equal: %A and %A" (i+1) expectedOperation (operations.[i]))
                i <- i + 1
            Expect.equal i (operations.Count) "The same number of operations should be present"
        with
        | _ ->
            let expected = expectedOperations |> List.ofSeq
            let actual = operations |> List.ofSeq
            printfn "Expected: %A" expected
            printfn "Actual: %A" actual
            reraise()


let verify s ops =
    let writer = TestWriterImpl (ConsoleColor.White, ConsoleColor.Black)
    ColoredWriter.writeCompleteString writer s
    writer.Verify ops

let verifyprintf<'T> ops (format: ColorPrintFormat<'T>) =
    let writer = TestWriterImpl (ConsoleColor.White, ConsoleColor.Black)
    let env = ColoredConsolePrintEnv(writer, fun () -> writer.Verify ops)
    doPrintfFromEnv format env