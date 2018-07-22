module BlackFox.MasterOfFoo.Build.Program

open BlackFox.TypedTaskDefinitionHelper
open Fake.Core
open Fake.BuildServer

let setupFakeContext (argv: string list) =
    let argvTweaked =
        match argv with
        | [ singleArg ] when not (singleArg.StartsWith("-")) ->
            [ "--target"; singleArg ]
        | _ -> argv
    let execContext = Context.FakeExecutionContext.Create false "build.fsx" argvTweaked
    Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

[<EntryPoint>]
let main argv =
    setupFakeContext (List.ofArray argv)
    BuildServer.install [ AppVeyor.Installer ]
    
    let defaultTask = Tasks.createAndGetDefault ()
    RunTaskOrDefault defaultTask
    0
