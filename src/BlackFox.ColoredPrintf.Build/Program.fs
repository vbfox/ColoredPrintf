module BlackFox.ColoredPrintf.Build.Program

open BlackFox.Fake
open Fake.Core
open Fake.BuildServer

[<EntryPoint>]
let main argv =
    BuildTask.setupContextFromArgv argv
    BuildServer.install [ AppVeyor.Installer; Travis.Installer; TeamFoundation.Installer ]

    let defaultTask = Tasks.createAndGetDefault ()
    BuildTask.runOrDefaultApp defaultTask
