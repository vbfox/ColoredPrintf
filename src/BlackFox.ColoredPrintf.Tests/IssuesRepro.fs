module IssuesRepro

open BlackFox.ColoredPrintf.Tests.TestWriter
open Expecto

[<Tests>]
let issue1 =

    testList "Issue #1" [
        testCase "Incorrect  1" <| fun _ ->
            Array.scanBack
            verifyprintf [ Write("1 2 3") ] "1 2 %d" 3

        testCase "Incorrect  2" <| fun _ ->
            verifyprintf [ Write("1 2 3") ] "1 %d %d" 2 3

        testCase "Incorrect  3" <| fun _ ->
            verifyprintf [ Write("1 2 3") ] "%d %d %d" 1 2 3

    ]
