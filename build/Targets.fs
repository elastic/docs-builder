// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module Targets

open Argu
open System.IO
open CommandLine
open Fake.Core
open Fake.IO
open Fake.Tools.Git
open Proc.Fs
open BuildInformation

let private clean _ =
    exec { run "dotnet" "clean" "-c" "release" }
    let removeArtifacts folder = Shell.cleanDir (Paths.ArtifactPath folder).FullName
    removeArtifacts "package"
    removeArtifacts "release-notes"
    removeArtifacts "tests"
    removeArtifacts "docs-builder"

let private compile _ = exec { run "dotnet" "build" "-c" "release" }

let private build _ = printfn "build"
let private release _ = printfn "release"
let private publish _ = printfn "publish"

let private version _ =
    let version = Software.Version
    printfn $"Informational version: %s{version.AsString}"
    printfn $"Semantic version: %s{version.Normalize()}"

let private format _ = exec { run "dotnet" "format" "--verbosity" "quiet" }

let private watch _ = exec { run "dotnet" "watch" "--project" "src/docs-builder" "--no-hot-reload" "--" "serve" }

let private lint _ =
    match exec {
        exit_code_of "dotnet" "format" "--verify-no-changes"
    } with
    | 0 -> printfn "There are no dotnet formatting violations, continuing the build."
    | _ -> failwithf "There are dotnet formatting violations. Call `dotnet format` to fix or specify -c to ./build.sh to skip this check"

let private pristineCheck (arguments:ParseResults<Build>) =
    let skipCheck = arguments.TryGetResult Skip_Dirty_Check |> Option.isSome
    match skipCheck, Information.isCleanWorkingCopy "." with
    | true, _ -> printfn "Skip checking for clean working copy since -c is specified"
    | _, true  -> printfn "The checkout folder does not have pending changes, proceeding"
    | _ -> failwithf "The checkout folder has pending changes, aborting. Specify -c to ./build.sh to skip this check"
    
    match skipCheck, (exec { exit_code_of "dotnet" "format" "--verify-no-changes" }) with
    | true, _ -> printfn "Skip formatting checks since -c is specified"
    | _, 0  -> printfn "There are no dotnet formatting violations, continuing the build."
    | _ -> failwithf "There are dotnet formatting violations. Call `dotnet format` to fix or specify -c to ./build.sh to skip this check"

let private publishBinaries _ =
    exec { run "dotnet" "publish" "src/docs-builder/docs-builder.csproj" }
    exec { run "dotnet" "publish" "src/docs-assembler/docs-assembler.csproj" }

let private publishZip _ =
    exec { run "dotnet" "publish" "src/docs-builder/docs-builder.csproj" }
    let binary = match OS.Current with Windows -> "docs-builder.exe" | _ -> "docs-builder"
    Zip.zip
        ".artifacts/publish/docs-builder/release"
        $".artifacts/publish/docs-builder/release/docs-builder-%s{OS.Name}-{OS.Arch}.zip"
        [
            $".artifacts/publish/docs-builder/release/%s{binary}";
            ".artifacts/publish/docs-builder/release/LICENSE.txt";
            ".artifacts/publish/docs-builder/release/NOTICE.txt"
        ]

let private publishContainers _ =

    let createImage project =
        let imageTag =
            match project with
            | "docs-builder" -> "jammy-chiseled-aot"
            | _ -> "jammy-chiseled-aot"
        let labels =
            let exitCode = exec {
                validExitCode (fun _ -> true)
                exit_code_of "git" "describe" "--tags" "--exact-match" "HEAD"
            }
            match exitCode with | 0 -> "edge;latest" | _ -> "edge"
        let args =
            ["publish"; $"src/%s{project}/%s{project}.csproj"]
            @ [
                "/t:PublishContainer";
                "-p"; "DebugType=none";
                "-p"; $"ContainerBaseImage=mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-%s{imageTag}";
                "-p"; $"ContainerImageTags=\"%s{labels};%s{Software.Version.Normalize()}\""
                "-p"; $"ContainerRepository=elastic/%s{project}"
            ]
        let registry =
            match Environment.environVarOrNone "GITHUB_ACTION" with
            | None -> []
            | Some _ -> [
                "-p"; "ContainerRegistry=ghcr.io"
                "-p"; "ContainerUser=1001:1001";
            ]
        exec { run "dotnet" (args @ registry) }
    createImage "docs-builder"
    createImage "docs-assembler"

let private runTests _ =
    exec {
        run "dotnet" (
            ["test"; "-c"; "release"; "--no-restore"; "--no-build"; "--logger"; "GitHubActions"]
            @ ["--"; "RunConfiguration.CollectSourceInformation=true"]
        )
    }
    
let private validateLicenses _ =
    let args = ["-u"; "-t"; "-i"; "docs-builder.sln"; "--use-project-assets-json"
                "--forbidden-license-types"; "build/forbidden-license-types.json"
                "--packages-filter"; "#System\..*#";]
    exec { run "dotnet" (["dotnet-project-licenses"] @ args) }

let private generateReleaseNotes (arguments:ParseResults<Build>) =
    let currentVersion = Software.Version.NormalizeToShorter()
    let releaseNotesPath = Paths.ArtifactPath "release-notes"
    let output =
        Paths.RelativePathToRoot <| Path.Combine(releaseNotesPath.FullName, $"release-notes-%s{currentVersion}.md")
    let tokenArgs =
        match arguments.TryGetResult Token with
        | None -> []
        | Some token -> ["--token"; token;]
    let releaseNotesArgs =
        (Software.GithubMoniker.Split("/") |> Seq.toList)
        @ ["--version"; currentVersion
           "--label"; "enhancement"; "Features"
           "--label"; "bug"; "Fixes"
           "--label"; "documentation"; "Documentation"
        ] @ tokenArgs
        @ ["--output"; output]
        
    let args = ["release-notes"] @ releaseNotesArgs
    exec { run "dotnet" args }

let Setup (parsed:ParseResults<Build>) =
    let wireCommandLine (t: Build) =
        match t with
        // commands
        | Version -> Build.Step version
        | Clean -> Build.Cmd [Version] [] clean
        | Compile -> Build.Step compile
        | Build ->
            Build.Cmd
                [Clean; Lint; Compile] [] build
        
        | Test -> Build.Cmd [Compile] [] runTests
        
        | Release ->
            Build.Cmd 
                [PristineCheck; Build]
                [ValidateLicenses; ReleaseNotes]
                release

        | Publish ->
            Build.Cmd
                []
                [PublishBinaries; PublishContainers]
                release

        | Format -> Build.Step format
        | Watch -> Build.Step watch

        // steps
        | Lint -> Build.Step lint
        | PristineCheck -> Build.Step pristineCheck
        | PublishBinaries -> Build.Step publishBinaries
        | PublishContainers -> Build.Step publishContainers
        | PublishZip -> Build.Step publishZip
        | ValidateLicenses -> Build.Step validateLicenses
        | ReleaseNotes -> Build.Step generateReleaseNotes

        // flags
        | Single_Target
        | Token _
        | Skip_Dirty_Check -> Build.Ignore

    for target in Build.Targets do
        let setup = wireCommandLine target 
        setup target parsed