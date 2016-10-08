namespace BlackFox.ColoredPrintf.Tests

open System
open System.Collections.Generic
open BlackFox.ColoredPrintf
open NUnit.Framework

type Operation =
    | Write of string
    | SetForeground of ConsoleColor
    | SetBackground of ConsoleColor

type TestWriter(initialFg: ConsoleColor, initialBg: ConsoleColor) =
    let mutable fg = initialFg
    let mutable bg = initialBg
    let operations = List<Operation>()

    interface IColoredPrinterEnv with
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

module SimpleTests =
    let verify s ops =
        let writer = TestWriter (ConsoleColor.White, ConsoleColor.Black)
        ColoredString.writeCompleteString writer s
        writer.Verify ops

    [<Test>]
    let writeSingleString () =
        verify "Hello world" [Write("Hello world")]

    [<Test>]
    let writeFg () =
        verify
            "Hello $red[world]"
            [
                Write("Hello ")
                SetForeground(ConsoleColor.Red)
                Write("world")
                SetForeground(ConsoleColor.White)
            ]

    [<Test>]
    let writeBg () =
        verify
            "Hello $;red[world]"
            [
                Write("Hello ")
                SetBackground(ConsoleColor.Red)
                Write("world")
                SetBackground(ConsoleColor.Black)
            ]

    [<Test>]
    let writeFgAndBg () =
        verify
            "Hello $blue;red[world]"
            [
                Write("Hello ")
                SetForeground(ConsoleColor.Blue)
                SetBackground(ConsoleColor.Red)
                Write("world")
                SetForeground(ConsoleColor.White)
                SetBackground(ConsoleColor.Black)
            ]