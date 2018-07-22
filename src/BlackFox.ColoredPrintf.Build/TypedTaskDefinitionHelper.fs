/// Allow to define FAKE tasks with a syntax similar to Gulp tasks
/// From https://gist.github.com/vbfox/e3e22d9ffff9b9de7f51
module BlackFox.TypedTaskDefinitionHelper

open Fake.Core
open Fake.Core.TargetOperators
open System

/// What FAKE name a target
type TaskMetadata = {
    name: string
    dependencies: TaskInfo list
}
/// Dependency (Soft or Hard) to a target (That can be a null object)
and TaskInfo = {
    metadata: TaskMetadata option
    isSoft: bool
}
with
    static member NoTask =
        { metadata = None; isSoft = false }

    member this.Always
        with get() = { this with isSoft = false }

    member this.IfNeeded
        with get() = { this with isSoft = true }

    member this.If(condition: bool) =
        if condition then this else TaskInfo.NoTask

/// Register dependencies of the passed Task in FAKE
let inline private applyTaskDependecies meta =
    for dependency in meta.dependencies do
        match dependency.metadata with
        | Some dependencyMetadata ->
            if dependency.isSoft then
                dependencyMetadata.name ?=> meta.name |> ignore
            else
                dependencyMetadata.name ==> meta.name |> ignore
        | None -> ()

/// Register the Task for FAKE with all it's dependencies
let inline private registerTask meta body =
    Target.create meta.name body
    applyTaskDependecies meta

let inline private infoFromMeta meta =
    { metadata = Some meta; isSoft = false }

type TaskBuilder(metadata: TaskMetadata) =
    member __.TryFinally(f, compensation) =
        try
            f()
        finally
            compensation()
    member __.TryWith(f, catchHandler) =
        try
            f()
        with e -> catchHandler e
    member __.Using(disposable: #IDisposable, f) =
        try
            f disposable
        finally
            match disposable with
            | null -> ()
            | disp -> disp.Dispose()
    member __.For(sequence, f) =
        for i in sequence do f i
    member __.Combine(f1, f2) = f2(); f1
    member __.Zero() = ()
    member __.Delay f = f
    member __.Run f =
        registerTask metadata f
        infoFromMeta metadata

/// Define a Task with it's dependencies
let Task name dependencies body =
    let metadata = {name = name; dependencies = dependencies }
    registerTask metadata body
    infoFromMeta metadata

/// Define a Task with it's dependencies
let task name dependencies =
    let metadata = {name = name; dependencies = dependencies }
    TaskBuilder(metadata)

/// Define a Task without any body, only dependencies
let EmptyTask name dependencies =
    let metadata = {name = name; dependencies = dependencies }
    registerTask metadata (fun _ -> ())
    infoFromMeta metadata

/// Run the task specified on the command line if there was one or the
/// default one otherwise.
let RunTaskOrDefault (taskInfo: TaskInfo) =
    match taskInfo.metadata with
    | Some metadata -> Target.runOrDefault metadata.name
    | None -> failwith "No default task specified."
