module BlackFox.ColoredPrintf.Build.Tasks

open Fake.Api
open Fake.BuildServer
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.Tools

open BlackFox
open BlackFox.Fake
open System.Xml.Linq

let createAndGetDefault () =
    let configuration = Environment.environVarOrDefault "configuration" "Release"
    let fakeConfiguration =
        match configuration.Trim().ToLowerInvariant() with
        | "release" -> DotNet.BuildConfiguration.Release
        | "debug" -> DotNet.BuildConfiguration.Debug
        | _ -> DotNet.BuildConfiguration.Custom configuration

    let from s =
        { LazyGlobbingPattern.BaseDirectory = s; Includes = []; Excludes = [] }
        :> IGlobbingPattern

    let rootDir = System.IO.Path.GetFullPath(__SOURCE_DIRECTORY__ </> ".." </> "..")
    let srcDir = rootDir </> "src"
    let artifactsDir = rootDir </> "artifacts"
    let libraryProjectFile = srcDir </> "BlackFox.ColoredPrintf" </> "BlackFox.ColoredPrintf.fsproj"
    let libraryBinDir = artifactsDir </> "BlackFox.ColoredPrintf" </> configuration
    let solutionFile = rootDir </> "BlackFox.ColoredPrintf.sln"
    let projects =
        from srcDir
        ++ "**/*.*proj"
        -- "*.Build/*"

    /// The profile where the project is posted
    let gitOwner = "vbfox"
    let gitHome = "https://github.com/" + gitOwner

    /// The name of the project on GitHub
    let gitName = "ColoredPrintf"

    /// The url for the raw files hosted
    let gitRaw = Environment.environVarOrDefault "gitRaw" ("https://raw.github.com/" + gitOwner)

    let release =
        let fromFile = ReleaseNotes.load (rootDir </> "Release Notes.md")
        if BuildServer.buildServer = BuildServer.AppVeyor then
            let appVeyorBuildVersion = int AppVeyor.Environment.BuildVersion
            let nugetVer = sprintf "%s-appveyor%04i" fromFile.NugetVersion appVeyorBuildVersion
            let asmVer = System.Version.Parse(fromFile.AssemblyVersion)
            let asmVer = System.Version(asmVer.Major, asmVer.Minor, asmVer.Build, appVeyorBuildVersion)
            ReleaseNotes.ReleaseNotes.New(asmVer.ToString(), nugetVer, fromFile.Date, fromFile.Notes)
        else
            fromFile

    Trace.setBuildNumber release.AssemblyVersion

    let writeVersionProps() =
        let doc =
            XDocument(
                XElement(XName.Get("Project"),
                    XElement(XName.Get("PropertyGroup"),
                        XElement(XName.Get "Version", release.NugetVersion),
                        XElement(XName.Get "PackageReleaseNotes", String.toLines release.Notes))))
        let path = artifactsDir </> "Version.props"
        System.IO.File.WriteAllText(path, doc.ToString())

    let init = BuildTask.create "Init" [] {
        Directory.create artifactsDir
    }

    let clean = BuildTask.create "Clean" [init] {
        let objDirs = projects |> Seq.map(fun p -> System.IO.Path.GetDirectoryName(p) </> "obj") |> List.ofSeq
        Shell.cleanDirs (artifactsDir :: objDirs)
    }

    let generateVersionInfo = BuildTask.create "GenerateVersionInfo" [init; clean.IfNeeded] {
        writeVersionProps ()
        AssemblyInfoFile.createFSharp (artifactsDir </> "Version.fs") [AssemblyInfo.Version release.AssemblyVersion]
    }

    let build = BuildTask.create "Build" [generateVersionInfo; clean.IfNeeded] {
        DotNet.build
          (fun p -> { p with Configuration = fakeConfiguration })
          solutionFile
    }

    let runTests = BuildTask.create "RunTests" [build] {
        [artifactsDir </> "BlackFox.ColoredPrintf.Tests" </> configuration </> "netcoreapp2.0" </> "BlackFox.ColoredPrintf.Tests.dll"]
            |> Expecto.run (fun p ->
                { p with
                    PrintVersion = false
                    FailOnFocusedTests = true
                })
    }

    let nupkgDir = artifactsDir </> "BlackFox.ColoredPrintf" </> configuration

    let nuget = BuildTask.create "NuGet" [build] {
        DotNet.pack
            (fun p -> { p with Configuration = fakeConfiguration })
            libraryProjectFile
        let nupkgFile =
            nupkgDir
                </> (sprintf "BlackFox.ColoredPrintf.%s.nupkg" release.NugetVersion)

        Trace.publish ImportData.BuildArtifact nupkgFile
    }

    let publishNuget = BuildTask.create "PublishNuget" [nuget] {
        let key =
            match Environment.environVarOrNone "nuget-key" with
            | Some(key) -> key
            | None -> UserInput.getUserPassword "NuGet key: "

        Paket.push <| fun p ->  { p with WorkingDir = nupkgDir; ApiKey = key }
    }

    let zipFile = artifactsDir </> (sprintf "BlackFox.ColoredPrintf-%s.zip" release.NugetVersion)

    let zip = BuildTask.create "Zip" [build] {
        let comment = sprintf "ColoredPrintf v%s" release.NugetVersion
        from libraryBinDir
            ++ "**/*.dll"
            ++ "**/*.xml"
            -- "**/FSharp.Core.*"
            |> Zip.createZip libraryBinDir zipFile comment 9 false

        Trace.publish ImportData.BuildArtifact zipFile
    }

    let gitRelease = BuildTask.create "GitRelease" [nuget.IfNeeded] {
        let remote =
            Git.CommandHelper.getGitResult "" "remote -v"
            |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
            |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
            |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

        Git.Branches.tag "" release.NugetVersion
        Git.Branches.pushTag "" remote release.NugetVersion
    }

    let githubRelease = BuildTask.create "GitHubRelease" [zip;gitRelease.IfNeeded] {
        let user =
            match Environment.environVarOrNone "github-user" with
            | Some s -> s
            | _ -> UserInput.getUserInput "GitHub Username: "
        let pw =
            match Environment.environVarOrNone "github-pw" with
            | Some s -> s
            | _ -> UserInput.getUserPassword "GitHub Password or Token: "

        // release on github
        GitHub.createClient user pw
        |> GitHub.createRelease gitOwner gitName release.NugetVersion (fun p ->
            { p with
                Prerelease = release.SemVer.PreRelease <> None
                Body = String.toLines release.Notes
                Draft = true
            }
        )
        |> GitHub.uploadFile zipFile
        |> GitHub.publishDraft
        |> Async.RunSynchronously
    }

    let _releaseTask = BuildTask.createEmpty "Release" [clean; gitRelease; githubRelease; publishNuget]
    let _ciTask = BuildTask.createEmpty "CI" [clean; runTests; zip; nuget]

    BuildTask.createEmpty "Default" [runTests]
