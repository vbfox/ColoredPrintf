open Expecto

[<EntryPoint>]
let main args =
    let writeResults = TestResults.writeNUnitSummary ("TestResults.xml", "BlackFox.ColoredPrintf.Tests")
    let config = defaultConfig.appendSummaryHandler writeResults
    runTestsInAssembly config args
