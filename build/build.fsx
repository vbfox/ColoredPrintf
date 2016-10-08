// include Fake libs
#r "../packages/FAKE/tools/FakeLib.dll"
#load "./TaskDefinitionHelper.fsx"
#load "./AppVeyorEx.fsx"

open System
open Fake
open Fake.ReleaseNotesHelper
open Fake.Testing.NUnit3
open BlackFox

let configuration = environVarOrDefault "configuration" "Release"

let from s = { BaseDirectory = s; Includes = []; Excludes = [] }

let rootDir = System.IO.Path.GetFullPath(__SOURCE_DIRECTORY__ </> "..")
let srcDir = rootDir </> "src"
let artifactsDir = rootDir </> "artifacts"
let binDir = artifactsDir </> "bin"
let librarySrcDir = srcDir </> "BlackFox.ColoredPrintf"
let libraryBinDir = binDir </> "BlackFox.ColoredPrintf" </> configuration
let projects = from srcDir ++ "**/*.*proj"

/// The profile where the project is posted
let gitOwner = "vbfox"
let gitHome = "https://github.com/" + gitOwner

/// The name of the project on GitHub
let gitName = "ColoredPrintf"

/// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" ("https://raw.github.com/" + gitOwner)

let release =
    let fromFile = LoadReleaseNotes (rootDir </> "Release Notes.md")
    if buildServer = AppVeyor then
        let appVeyorBuildVersion = int appVeyorBuildVersion
        let nugetVer = sprintf "%s-appveyor%04i" fromFile.NugetVersion appVeyorBuildVersion
        let asmVer = System.Version.Parse(fromFile.AssemblyVersion)
        let asmVer = System.Version(asmVer.Major, asmVer.Minor, asmVer.Build, appVeyorBuildVersion)
        ReleaseNotes.New(asmVer.ToString(), nugetVer, fromFile.Date, fromFile.Notes)
    else
        fromFile

AppVeyorEx.updateBuild (fun info -> { info with Version = Some release.AssemblyVersion })

Task "Init" [] <| fun _ ->
    CreateDir artifactsDir

// Targets
Task "Clean" ["Init"] <| fun _ ->
    CleanDirs [artifactsDir]

Task "Build" ["Init"; "?Clean"] <| fun _ ->
    MSBuild null "Build" ["Configuration", configuration] projects
        |> ignore

Task "RunTests" [ "Build"] <| fun _ ->
    let nunitPath = rootDir </> @"packages" </> "NUnit.ConsoleRunner" </> "tools" </> "nunit3-console.exe"
    let testAssemblies = artifactsDir </> "bin" </> "*.Tests" </> configuration </> "*.Tests.dll"
    let testResults = artifactsDir </> "TestResults.xml"

    try
        !! testAssemblies
        |> NUnit3 (fun p ->
            {p with
                ToolPath = nunitPath
                TimeOut = TimeSpan.FromMinutes 20.
                DisposeRunners = true
                ResultSpecs = [testResults] })
    finally
        AppVeyor.UploadTestResultsFile AppVeyor.NUnit3 testResults

Task "NuGet" ["Build"] <| fun _ ->
    Paket.Pack <| fun p ->
        { p with
            OutputPath = artifactsDir
            Version = release.NugetVersion
            ReleaseNotes = toLines release.Notes
            WorkingDir = librarySrcDir
            BuildConfig = configuration
            BuildPlatform = "AnyCPU"}
    AppVeyor.PushArtifacts (from artifactsDir ++ "*.nupkg")

Task "PublishNuget" ["NuGet"] <| fun _ ->
    let key =
        match environVarOrNone "nuget-key" with
        | Some(key) -> key
        | None -> getUserPassword "NuGet key: "

    Paket.Push <| fun p ->  { p with WorkingDir = artifactsDir; ApiKey = key }

let zipFile = artifactsDir </> (sprintf "BlackFox.ColoredPrintf-%s.zip" release.NugetVersion)

Task "Zip" ["Build"] <| fun _ ->
    from libraryBinDir
        ++ "**/*.dll"
        ++ "**/*.xml"
        -- "**/FSharp.Core.*"
        |> Zip libraryBinDir zipFile
    AppVeyor.PushArtifacts [zipFile]

#load "../paket-files/fsharp/FAKE/modules/Octokit/Octokit.fsx"

Task "GitHubRelease" ["Zip"] <| fun _ ->
    let user =
        match getBuildParam "github-user" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserInput "GitHub Username: "
    let pw =
        match getBuildParam "github-pw" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserPassword "GitHub Password or Token: "

    // release on github
    Octokit.createClient user pw
    |> Octokit.createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    |> Octokit.uploadFile zipFile
    |> Octokit.releaseDraft
    |> Async.RunSynchronously

Task "GitRelease" [] <| fun _ ->
    let remote =
        Git.CommandHelper.getGitResult "" "remote -v"
        |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
        |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
        |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

    Git.Branches.tag "" release.NugetVersion
    Git.Branches.pushTag "" remote release.NugetVersion

Task "Default" ["RunTests"] DoNothing
Task "Release" ["Clean"; "GitRelease"; "GitHubRelease"; "PublishNuget"] DoNothing
Task "CI" ["Clean"; "RunTests"; "Zip"; "NuGet"] DoNothing

RunTaskOrDefault "Default"
