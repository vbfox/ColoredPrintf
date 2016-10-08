#r "../packages/FAKE/tools/FakeLib.dll"

namespace BlackFox

/// Allow to define FAKE tasks with a syntax similar to Gulp tasks
[<AutoOpen>]
module TaskDefinitionHelper =
    open Fake
    open System.Text.RegularExpressions

    type Dependency =
        | Direct of dependsOn : string
        | Soft of dependsOn : string
        | Conditional of dependsOn : string * condition : bool

    let private (|RegExp|_|) pattern input =
        let m = Regex.Match(input, pattern, RegexOptions.Compiled)
        if m.Success && m.Groups.Count > 0 then
            Some (m.Groups.[1].Value)
        else
            None

    let private parseDependency str =
        match str with
        | RegExp @"^\?(.*)$" dep -> Soft dep
        | dep -> Direct dep

    let mutable private tasks : (string * (Dependency list)) list = []

    /// Define a task with it's dependencies
    let TaskEx name dependencies body =
        Target name body
        tasks <- (name, dependencies |> List.ofSeq) :: tasks

    /// Define a task with it's dependencies
    let Task name dependencies body =
        let dependencies = dependencies |> Seq.map parseDependency
        TaskEx name dependencies body

    /// Send all the defined inter task dependencies to FAKE
    let ApplyTasksDependencies () =
         for (targetName, dependencies) in tasks do
            for dependency in dependencies do
                match dependency with
                | Direct dep -> dep ==> targetName |> ignore
                | Soft dep -> dep ?=> targetName |> ignore
                | Conditional (dep, cond) -> dep =?> (targetName, cond) |> ignore

         tasks <- []

    /// Run the task specified on the command line if there was one or the
    /// default one otherwise.
    let RunTaskOrDefault taskName =
        ApplyTasksDependencies ()
        RunTargetOrDefault taskName
