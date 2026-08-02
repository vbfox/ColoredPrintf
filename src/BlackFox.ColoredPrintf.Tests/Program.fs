open Expecto
open Expecto.Tests

[<EntryPoint>]
let main args =
    runTestsInAssemblyWithCLIArgs [ NUnit_Summary "TestResults.xml" ] args
