module IssuesRepro

open NUnit.Framework
open BlackFox.ColoredPrintf.Tests.TestWriter

module ``Issue #1`` =   
    [<Test>]
    let ``Incorrect  1`` () =
        verifyprintf [ Write("1 2 3") ] "1 2 %d" 3
    
    [<Test>]
    let ``Incorrect  2`` () =
        verifyprintf [ Write("1 2 3") ] "1 %d %d" 2 3
    
    [<Test>]
    let ``Incorrect  3`` () =
        verifyprintf [ Write("1 2 3") ] "%d %d %d" 1 2 3