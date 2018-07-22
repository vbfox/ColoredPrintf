module BlackFox.ExpectoDotNetCli

open System
open Fake.Core
open Fake.DotNet.Testing
open Fake.Testing.Common

let run (setParams : Expecto.Params -> Expecto.Params) (assemblies : string seq) =
    let details = assemblies |> String.separated ", "
    use __ = Trace.traceTask "Expecto" details

    let runAssembly testAssembly =
        let exitCode =
            let fakeStartInfo testAssembly (args: Expecto.Params) =
                let workingDir =
                    if String.isNotNullOrEmpty args.WorkingDirectory
                    then args.WorkingDirectory else Fake.IO.Path.getDirectory testAssembly
                let argsString = sprintf "\"%s\" %O" testAssembly args
                (fun (info: ProcStartInfo) ->
                    { info with 
                        FileName = "dotnet"
                        Arguments = argsString
                        WorkingDirectory = workingDir } )

            let execWithExitCode testAssembly argsString timeout = 
                Process.execSimple (fakeStartInfo testAssembly argsString) timeout

            let p = setParams Expecto.Params.DefaultParams
            execWithExitCode testAssembly p TimeSpan.MaxValue

        testAssembly, exitCode

    let res =
        assemblies
        |> Seq.map runAssembly
        |> Seq.filter( snd >> (<>) 0)
        |> Seq.toList

    match res with
    | [] -> ()
    | failedAssemblies ->
        failedAssemblies
        |> List.map (fun (testAssembly,exitCode) -> 
            sprintf "Expecto test of assembly '%s' failed. Process finished with exit code %d." testAssembly exitCode )
        |> String.concat System.Environment.NewLine
        |> FailedTestsException |> raise
    __.MarkSuccess()
