open Expecto

[<EntryPoint>]
let main args =
    let writeResults = TestResults.writeNUnitSummary "TestResults.xml"
    let config = defaultConfig.appendSummaryHandler writeResults
    runTestsInAssembly config args
