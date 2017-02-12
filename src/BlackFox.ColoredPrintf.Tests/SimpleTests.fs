module BlackFox.ColoredPrintf.Tests.SimpleTests
open System
open BlackFox.ColoredPrintf.Tests.TestWriter
open Expecto

[<Tests>]
let simpleTests =
    testList "Simple tests" [

        testCase "Single string" <| fun _ ->
            verify "Hello world" [Write("Hello world")]

        testCase "Escape random char" <| fun _ ->
            verify "He\llo" [Write("He\llo")]

        testCase "Unescaped bracket end" <| fun _ ->
            verify "Hello[]world" [Write("Hello[]world")]

        testCase "Nothing" <| fun _ ->
            verify "" []

        testCase "Fg at end" <| fun _ ->
            verify
                "Hello $red[world]"
                [
                    Write("Hello ")
                    SetForeground(ConsoleColor.Red)
                    Write("world")
                    SetForeground(ConsoleColor.White)
                ]

        testCase "Fg" <| fun _ ->
            verify
                "Hello $red[world]."
                [
                    Write("Hello ")
                    SetForeground(ConsoleColor.Red)
                    Write("world")
                    SetForeground(ConsoleColor.White)
                    Write(".")
                ]

        testCase "Bg at end" <| fun _ ->
            verify
                "Hello $;red[world]"
                [
                    Write("Hello ")
                    SetBackground(ConsoleColor.Red)
                    Write("world")
                    SetBackground(ConsoleColor.Black)
                ]

        testCase "Bg" <| fun _ ->
            verify
                "Hello $;red[world]."
                [
                    Write("Hello ")
                    SetBackground(ConsoleColor.Red)
                    Write("world")
                    SetBackground(ConsoleColor.Black)
                    Write(".")
                ]

        testCase "Fg and Bg" <| fun _ ->
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


        testCase "Full color" <| fun _ ->
            verify
                "$blue[world]"
                [
                    SetForeground(ConsoleColor.Blue)
                    Write("world")
                    SetForeground(ConsoleColor.White)
                ]

        testCase "Escape start" <| fun _ ->
            verify
                "\$blue[world]"
                [
                    Write("$blue[world]")
                ]

        testCase "Escape end" <| fun _ ->
            verify
                "$blue[world\].]"
                [
                    SetForeground(ConsoleColor.Blue)
                    Write("world].")
                    SetForeground(ConsoleColor.White)
                ]

        testCase "Unfinished" <| fun _ ->
            verify
                "$blue[world"
                [
                    SetForeground(ConsoleColor.Blue)
                    Write("world")
                    SetForeground(ConsoleColor.White)
                ]

        testCase "Nested" <| fun _ ->
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

        testCase "Simple printf" <| fun _ ->
            verifyprintf
                [
                    Write("Hello world")
                ]
                "Hello world"

        testCase "color printf" <| fun _ ->
            verifyprintf
                [
                    Write("Hello ")
                    SetForeground(ConsoleColor.Red)
                    Write("world")
                    SetForeground(ConsoleColor.White)
                ]
                "Hello $red[world]"
    ]