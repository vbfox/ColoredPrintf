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

open BlackFox.CommandLine
open BlackFox.Fake
open System.Xml.Linq

let testProjectName = "BlackFox.ColoredPrintf.Tests"

let private isNetCoreAppRuntimeAvailable (major: string) =
    let psi = System.Diagnostics.ProcessStartInfo("dotnet", "--list-runtimes")
    psi.RedirectStandardOutput <- true
    psi.UseShellExecute <- false
    use p = System.Diagnostics.Process.Start(psi)
    let output = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    output.Split('\n')
    |> Array.exists (fun line -> line.StartsWith(sprintf "Microsoft.NETCore.App %s." major))

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

    let getUnionCaseName (x:'a) =
        match Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(x, typeof<'a>) with | case, _ -> case.Name

    // A release tag build (the "Publish" workflow, triggered by a version tag push) must use the
    // plain version from Release Notes.md, not a CI-suffixed prerelease version, since it has to
    // match the pushed tag and be the version actually published.
    let isReleaseTagBuild = Environment.environVarOrNone "GITHUB_REF_TYPE" = Some "tag"

    let release =
        let fromFile = ReleaseNotes.load (rootDir </> "Release Notes.md")
        if BuildServer.buildServer <> BuildServer.LocalBuild && not isReleaseTagBuild then
            let buildServerName = (getUnionCaseName BuildServer.buildServer).ToLowerInvariant()
            let nugetVer = sprintf "%s-%s.%s" fromFile.NugetVersion buildServerName BuildServer.buildVersion
            ReleaseNotes.ReleaseNotes.New(fromFile.AssemblyVersion, nugetVer, fromFile.Date, fromFile.Notes)
        else
            fromFile

    Trace.setBuildNumber release.NugetVersion

    let nupkgFile = libraryBinDir </> (sprintf "BlackFox.ColoredPrintf.%s.nupkg" release.NugetVersion)

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
        let baseTestDir = artifactsDir </> testProjectName </> (string configuration)
        let isCI = BuildServer.buildServer <> BuildServer.LocalBuild

        let testConfs =
            if not isCI && not (isNetCoreAppRuntimeAvailable "2") then
                Trace.traceImportant "netcoreapp2.0 runtime not found locally, skipping its tests (this would be a hard error on CI)"
                ["net10.0", ".dll"]
            else
                ["netcoreapp2.0", ".dll"; "net10.0", ".dll"]

        testConfs
        |> List.map (fun (fw, ext) -> baseTestDir </> fw </> (testProjectName + ext))
        |> Expecto.run (fun p ->
            { p with
                PrintVersion = false
                FailOnFocusedTests = true
            })

        for (fw, _) in testConfs do
            let dir = baseTestDir </> fw
            let outFile = sprintf "TestResults_%s.xml" (fw.Replace('.', '_'))
            File.delete (dir </> outFile)
            (dir </> "TestResults.xml") |> Shell.rename (dir </> outFile)
            Trace.publish (ImportData.Nunit NunitDataVersion.Nunit) (dir </> outFile)
    }

    let nuget = BuildTask.create "NuGet" [build;runTests.IfNeeded] {
        DotNet.pack
            (fun p -> { p with Configuration = fakeConfiguration })
            libraryProjectFile

        Trace.publish ImportData.BuildArtifact nupkgFile
    }

    let ciPublishNuget = BuildTask.create "CIPublishNuget" [nuget] {
        let key =
            match Environment.environVarOrNone "nuget-key" with
            | Some key -> key
            | None -> UserInput.getUserPassword "NuGet key: "

        let cmd =
            CmdLine.empty
            |> CmdLine.append "push"
            |> CmdLine.append nupkgFile
            |> CmdLine.append "--api-key"
            |> CmdLine.append key
            |> CmdLine.append "--source"
            |> CmdLine.append "https://api.nuget.org/v3/index.json"
            |> CmdLine.toString

        let result = DotNet.exec id "nuget" cmd

        if not result.OK then
            failwithf "dotnet nuget push failed:\n%s" (String.concat "\n" result.Errors)
    }

    let zipFile = artifactsDir </> (sprintf "BlackFox.ColoredPrintf-%s.zip" release.NugetVersion)

    let zip = BuildTask.create "Zip" [build;runTests.IfNeeded] {
        let comment = sprintf "ColoredPrintf v%s" release.NugetVersion
        from libraryBinDir
            ++ "**/*.dll"
            ++ "**/*.xml"
            -- "**/FSharp.Core.*"
            |> Zip.createZip libraryBinDir zipFile comment 9 false

        Trace.publish ImportData.BuildArtifact zipFile
    }

    /// Validate that it's safe to cut a release, then tag and push. Pushing the tag is what triggers
    /// the "Publish" GitHub Actions workflow, which does the actual build/test/pack/publish.
    let tagRelease = BuildTask.create "TagRelease" [init] {
        Git.CommandHelper.directRunGitCommandAndFail "" "fetch origin main --tags"

        if Git.Information.getBranchName "" <> "main" then
            failwith "Releases must be created from the 'main' branch."

        let localSha = Git.Branches.getSHA1 "" "HEAD"
        let remoteSha = Git.Branches.getSHA1 "" "origin/main"
        if localSha <> remoteSha then
            failwithf "Local 'main' (%s) is not in sync with 'origin/main' (%s). Pull or push before releasing." localSha remoteSha

        if not (Git.Information.isCleanWorkingCopy "") then
            failwith "Working copy has uncommitted changes."

        let tagExistsLocally =
            Git.CommandHelper.getGitResult "" "tag --list"
            |> Seq.contains release.NugetVersion

        let tagExistsOnRemote =
            Git.CommandHelper.getGitResult "" "ls-remote --tags origin"
            |> Seq.exists (fun (line: string) -> line.EndsWith("refs/tags/" + release.NugetVersion))

        if tagExistsLocally || tagExistsOnRemote then
            failwithf "Tag %s already exists, nothing to release." release.NugetVersion

        let remote =
            Git.CommandHelper.getGitResult "" "remote -v"
            |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
            |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
            |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

        Trace.log (sprintf "About to release %s to %s:" release.NugetVersion remote)
        Trace.log (String.toLines release.Notes)

        let answer = UserInput.getUserInput (sprintf "Push tag %s? This triggers the publish workflow. [y/N] " release.NugetVersion)
        if answer.Trim().ToLowerInvariant() <> "y" then
            failwith "Aborted."

        Git.Branches.tag "" release.NugetVersion
        Git.Branches.pushTag "" remote release.NugetVersion
    }

    let ciPublishGitHubRelease = BuildTask.create "CIPublishGitHubRelease" [zip] {
        let client =
            match Environment.environVarOrNone "GITHUB_TOKEN" with
            | Some token -> GitHub.createClientWithToken token
            | None ->
                // This path is never called by CI but kept just in case
                let user =
                    match Environment.environVarOrNone "github-user" with
                    | Some s -> s
                    | _ -> UserInput.getUserInput "GitHub Username: "
                let pw =
                    match Environment.environVarOrNone "github-pw" with
                    | Some s -> s
                    | _ -> UserInput.getUserPassword "GitHub Password or Token: "
                GitHub.createClient user pw

        // release on github
        client
        |> GitHub.draftNewRelease
            gitOwner
            gitName
            release.NugetVersion
            (release.SemVer.PreRelease <> None)
            release.Notes
        |> GitHub.uploadFile zipFile
        |> GitHub.publishDraft
        |> Async.RunSynchronously
    }

    /// Run locally to cut a release: validates preconditions then tags and pushes, which triggers
    /// the `Publish` GitHub Actions workflow (see .github/workflows/publish.yml).
    let _releaseTask = BuildTask.createEmpty "Release" [tagRelease]

    /// Invoked by the `Publish` GitHub Actions workflow after a release tag is pushed: builds,
    /// tests, packs, creates the GitHub Release and publishes the NuGet package.
    let _ciPublishReleaseTask = BuildTask.createEmpty "CIPublishRelease" [clean; runTests; zip; nuget; ciPublishGitHubRelease; ciPublishNuget]

    let _ciTask = BuildTask.createEmpty "CI" [clean; runTests; zip; nuget]

    BuildTask.createEmpty "Default" [runTests]
