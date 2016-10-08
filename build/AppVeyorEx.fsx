module BlackFox.AppVeyorEx

#r "../packages/FAKE/tools/FakeLib.dll"
#load "./CmdLine.fs"

open BlackFox.CommandLine
open Fake
open System.IO

let private sendToAppVeyor args =
    ExecProcess (fun info ->
        info.FileName <- "appveyor"
        info.Arguments <- args) (System.TimeSpan.MaxValue)
    |> ignore

type BuildInfo = {
    Version: string option
    Message: string option
    CommitId: string option
    Committed: System.DateTimeOffset option
    AuthorName: string option
    AuthorEmail: string option
    CommitterName: string option
    CommitterEmail: string option
}

let defaultBuildInfo = {
    Version = None
    Message = None
    CommitId = None
    Committed = None
    AuthorName = None
    AuthorEmail = None
    CommitterName = None
    CommitterEmail = None
}

let updateBuild (setBuildInfo : BuildInfo -> BuildInfo) =
    let appendAppVeyor opt (name: string) (transform: _ -> string) cmdLine =
        match opt with
        | Some(value) ->
            cmdLine
            |> CmdLine.append name
            |> CmdLine.append (transform value)
        | None -> cmdLine
    if buildServer = BuildServer.AppVeyor then
        let info = setBuildInfo defaultBuildInfo
        let cmdLine =
            CmdLine.empty
            |> CmdLine.append "UpdateBuild"
            |> appendAppVeyor info.Version "-Version" id
            |> appendAppVeyor info.Message "-Message" id
            |> appendAppVeyor info.CommitId "-CommitId" id
            |> appendAppVeyor info.Committed "-Committed" (fun d -> d.ToString("MMddyyyy-HHmm"))
            |> appendAppVeyor info.AuthorName "-AuthorName" id
            |> appendAppVeyor info.AuthorEmail "-AuthorEmail" id
            |> appendAppVeyor info.CommitterName "-CommitterName" id
            |> appendAppVeyor info.CommitterEmail "-CommitterEmail" id
            |> CmdLine.toString

        sendToAppVeyor cmdLine
