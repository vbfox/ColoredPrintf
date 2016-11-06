module BlackFox.ColoredPrintf.Tests.SimpleTests
open System
open BlackFox.ColoredPrintf.Tests.TestWriter
open NUnit.Framework

[<Test>]
let writeSingleString () =
    verify "Hello world" [Write("Hello world")]

[<Test>]
let escapedRandomChar () =
    verify "He\llo" [Write("He\llo")]

[<Test>]
let unescapedBrackedEnd () =
    verify "Hello[]world" [Write("Hello[]world")]

[<Test>]
let writeNothing () =
    verify "" []

[<Test>]
let writeFgAtEnd () =
    verify
        "Hello $red[world]"
        [
            Write("Hello ")
            SetForeground(ConsoleColor.Red)
            Write("world")
            SetForeground(ConsoleColor.White)
        ]

[<Test>]
let writeFg () =
    verify
        "Hello $red[world]."
        [
            Write("Hello ")
            SetForeground(ConsoleColor.Red)
            Write("world")
            SetForeground(ConsoleColor.White)
            Write(".")
        ]

[<Test>]
let writeBgAtEnd () =
    verify
        "Hello $;red[world]"
        [
            Write("Hello ")
            SetBackground(ConsoleColor.Red)
            Write("world")
            SetBackground(ConsoleColor.Black)
        ]

[<Test>]
let writeBg () =
    verify
        "Hello $;red[world]."
        [
            Write("Hello ")
            SetBackground(ConsoleColor.Red)
            Write("world")
            SetBackground(ConsoleColor.Black)
            Write(".")
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


[<Test>]
let writeFullColor () =
    verify
        "$blue[world]"
        [
            SetForeground(ConsoleColor.Blue)
            Write("world")
            SetForeground(ConsoleColor.White)
        ]

[<Test>]
let escapeStart () =
    verify
        "\$blue[world]"
        [
            Write("$blue[world]")
        ]

[<Test>]
let escapeEnd () =
    verify
        "$blue[world\].]"
        [
            SetForeground(ConsoleColor.Blue)
            Write("world].")
            SetForeground(ConsoleColor.White)
        ]

[<Test>]
let writeUnfinished () =
    verify
        "$blue[world"
        [
            SetForeground(ConsoleColor.Blue)
            Write("world")
            SetForeground(ConsoleColor.White)
        ]

[<Test>]
let writeNested () =
    verify
        "$blue[Hello $red[world].]"
        [
            SetForeground(ConsoleColor.Blue)
            Write("Hello ")
            SetForeground(ConsoleColor.Red)
            Write("world")
            SetForeground(ConsoleColor.Blue)
            Write(".")
            SetForeground(ConsoleColor.White)
        ]

[<Test>]
let verySimplePrintf () =
    verifyprintf
        [
            Write("Hello world")
        ]
        "Hello world"

[<Test>]
let colorPrintF () =
    verifyprintf
        [
            Write("Hello ")
            SetForeground(ConsoleColor.Red)
            Write("world")
            SetForeground(ConsoleColor.White)
        ]
        "Hello $red[world]"
