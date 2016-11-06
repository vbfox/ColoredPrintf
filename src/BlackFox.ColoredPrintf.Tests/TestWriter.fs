module BlackFox.ColoredPrintf.Tests.TestWriter

open System
open System.Collections.Generic
open BlackFox.ColoredPrintf
open NUnit.Framework
open BlackFox.MasterOfFoo

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
                if operations.Count < i + 1 then
                    Assert.Fail (sprintf "Expected %ith operation to be %A but nothing found" (i+1) expectedOperation)
                match expectedOperation, operations.[i] with
                | Write s1, Write s2 ->
                    Assert.AreEqual(s1, s2, sprintf "Operation %i: The two operations didn't write the same text" (i+1))
                | SetForeground c1, SetForeground c2 ->
                    Assert.AreEqual(c1, c2, sprintf "Operation %i: The two operations didn't set the same foreground" (i+1))
                | SetBackground c1, SetBackground c2 ->
                    Assert.AreEqual(c1, c2, sprintf "Operation %i: The two operations didn't set the same background" (i+1))
                | op1, op2 -> 
                    Assert.Fail (sprintf "Operation %i: The two operations aren't of the same type: %A and %A" (i+1) op1 op2)
                i <- i + 1
            if i <> operations.Count  then
                Assert.Fail(sprintf "More operations that expected, for example: %A" operations.[i])
        with
        | :? AssertionException ->
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