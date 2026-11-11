// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module Targets

open System
open System.IO
open System.IO.Compression
open System.Text
open Nullean.Make.Fs
open Proc.Fs
open BuildInformation

type TestSuite =
    | All = 0
    | Unit = 1
    | Integration = 2

type TestOptions =
    { /// Which tests to run. Default: all
      Suite: TestSuite option }

type Target =
    /// Clean known output locations
    | Clean
    /// Print version information
    | Version
    /// Compile the solution
    | Compile
    /// Run a clean build with the format check
    | Build
    /// Run a clean build, then the tests (--suite all|unit|integration)
    | Test of TestOptions
    /// Alias of: test --suite unit
    | UnitTest
    /// Alias of: test --suite integration
    | Integrate
    /// Run dotnet curb format and cleanup
    | Format
    /// Run dotnet watch to build code, templates and web assets continuously
    | Watch
    /// Run dotnet curb check
    | Lint
    /// Check for a clean working copy and formatting
    | PristineCheck
    /// Check the licenses of the dependencies
    | ValidateLicenses
    /// Build, test, validate the licenses; shy of publishing
    | Release
    /// Publish the binaries and the container images
    | Publish
    /// Publish the docs-builder binary
    | PublishBinaries
    /// Publish the container images
    | PublishContainers
    /// Zip the published docs-builder and docs-journeys binaries
    | PublishZip
    /// Run the container images with --help
    | RunLocalContainer
    /// Clone, build the docs and package them into an air-gapped container
    | AirGappedBuild
    /// Run the air-gapped docs container on port 8080
    | AirGappedRun

// The journeys need Chrome and a built site, so they run through `mise run journeys` and their own CI jobs, not with the unit tests.
let private isJourneyProject (project: string) = project.Contains "Journeys.Tests"

let private testProjects (suite: TestSuite) =
    let directories =
        match suite with
        | TestSuite.Unit -> [ "tests" ]
        | TestSuite.Integration -> [ "tests-integration" ]
        | TestSuite.All -> [ "tests"; "tests-integration" ]
        | other -> failwithf "Unknown test suite '%O'" other
    let projects =
        directories
        |> List.collect (fun d ->
            Directory.EnumerateFiles(Path.Combine(Paths.Root.FullName, d), "*.csproj", SearchOption.AllDirectories)
            |> Seq.filter (fun project -> not (isJourneyProject project))
            |> Seq.sort
            |> Seq.toList
        )
    if List.isEmpty projects then
        failBuild $"No test projects found under %A{directories}"
    projects

let private envVarOrNone name =
    match Environment.GetEnvironmentVariable name with
    | null | "" -> None
    | value -> Some value

// Empties a folder and keeps it, creating it when it is missing.
let private cleanDir (directory: string) =
    if Directory.Exists directory then Directory.Delete(directory, true)
    Directory.CreateDirectory directory |> ignore

let private zipFiles (workingDir: string) (zipFile: string) (files: string list) =
    if File.Exists zipFile then File.Delete zipFile
    use archive = ZipFile.Open(zipFile, ZipArchiveMode.Create)
    for file in files do
        archive.CreateEntryFromFile(file, Path.GetRelativePath(workingDir, file), CompressionLevel.Optimal) |> ignore

let private prNumber () =
    match envVarOrNone "GITHUB_REF_NAME" with
    | None -> None
    | Some s when s.EndsWith "/merge" -> Some (s.Split('/') |> Seq.head)
    | _ -> None

// `onTag` is whether HEAD is exactly a tag.
let private imageTags (onTag: bool) =
    match onTag, prNumber () with
    | true, _ -> "edge;latest"
    | _, None -> "edge"
    | _, Some pr -> $"ci-%s{pr}"

let private compressibleExtensions = set [".html"; ".css"; ".js"; ".json"; ".svg"; ".xml"; ".txt"]

let private gzipCompressDirectory (directory: string) =
    // Remove stale .gz files from any previous run to avoid orphaned pairs
    Directory.EnumerateFiles(directory, "*.gz", SearchOption.AllDirectories)
    |> Seq.toArray
    |> Array.iter File.Delete

    let files =
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
        |> Seq.filter (fun f ->
            let ext = Path.GetExtension(f)
            not (System.String.IsNullOrEmpty ext) && compressibleExtensions.Contains(ext.ToLowerInvariant())
        )
        |> Seq.toArray

    printfn $"Compressing %d{files.Length} files in %s{directory}"

    files
    |> Array.Parallel.iter (fun file ->
        let gzPath = file + ".gz"
        use input = File.OpenRead(file)
        use output = File.Create(gzPath)
        use gzip = new GZipStream(output, CompressionLevel.SmallestSize)
        input.CopyTo(gzip)
    )

    // Replace originals with zero-byte placeholders so nginx try_files can resolve
    // paths, while gzip_static always serves the .gz version
    for file in files do
        File.Delete(file)
        File.Create(file).Dispose()

    printfn "Compression complete"

let private zipTool tool =
    let binary = match OS.Current with Windows -> $"{tool}.exe" | _ -> tool
    zipFiles
        $".artifacts/publish/{tool}/release"
        $".artifacts/publish/{tool}/release/{tool}-%s{OS.Name}-{OS.Arch}.zip"
        [
            $".artifacts/publish/{tool}/release/%s{binary}"
            $".artifacts/publish/{tool}/release/LICENSE.txt"
            $".artifacts/publish/{tool}/release/NOTICE.txt"
        ]

let private licenseFilter = @"#System\..*#"

let private containerProjects =
    [ "src/tooling/docs-builder/docs-builder.csproj", "docs-builder"
      "src/api/Elastic.Documentation.Mcp.Remote/Elastic.Documentation.Mcp.Remote.csproj", "docs-builder-mcp"
      "src/api/Elastic.Documentation.Api/Elastic.Documentation.Api.csproj", "docs-builder-api"
      "src/tooling/essc/essc.csproj", "website-search-essc"
      "src/tooling/docs-journeys/docs-journeys.csproj", "docs-builder-journeys" ]

let Run (argv: string[]) =
    let app = MakeApp<Target>("build", None)
    let skipDirtyCheck = app.Flag("--skip-dirty-check", short = "-c", desc = "Skip the clean checkout check that guards the release/publish targets")
    app.Default Build

    // TUnit runs the tests of a project in parallel; the projects run one after the other, so the output reads in order.
    let runTests (suite: TestSuite) = steps {
        let mutable failures = []
        for project in testProjects suite do
            let! code = exitCode $"dotnet test {project} -c release --no-restore --no-build"
            if code <> 0 then failures <- project :: failures
        if not (List.isEmpty failures) then
            let names = String.concat "\n" (List.rev failures)
            failBuild $"Test projects failed:\n%s{names}"

        // Every test project has now run, so the run manifests are complete: a verified snapshot nobody used belongs to a deleted or renamed test.
        if suite <> TestSuite.Integration then
            let! orphans = exitCode $"dotnet snap orphans"
            if orphans <> 0 then
                failBuild "Orphaned snapshots found. Delete them with `dotnet snap orphans --delete` and commit the removal."
    }

    app.Bind <| function
        | Version -> target [] {
            printfn $"Informational version: %s{Software.Version.Informational}"
            printfn $"Semantic version: %s{Software.Version.Semantic}" }
        | Clean -> target [Version] {
            sh $"dotnet clean -c release"
            for folder in [ "package"; "release-notes"; "tests"; "docs-builder" ] do
                cleanDir (Paths.ArtifactPath folder).FullName }
        | Compile -> step [] { sh $"dotnet build -c release" }
        | Build -> target [Clean; Lint; Compile] { printfn "build" }

        | Test o -> target [Compile] { do! runTests (defaultArg o.Suite TestSuite.All) }
        | UnitTest -> target [Compile] { do! runTests TestSuite.Unit }
        | Integrate -> target [Compile] { do! runTests TestSuite.Integration }

        | Format -> target [] {
            sh $"dotnet curb format ."
            sh $"dotnet curb cleanup --forward" }
        | Watch -> target [] { sh $"dotnet watch --project src/tooling/docs-builder --configuration debug -- serve --watch" }

        | Lint -> step [] {
            let! code = exitCode $"dotnet curb check . --cache .artifacts/curb.cache"
            if code = 0 then printfn "No formatting violations found."
            else failBuild "Formatting violations found. Run `dotnet curb format .` to fix, or specify -c to ./build.sh to skip this check" }
        | PristineCheck -> step [] {
            if skipDirtyCheck.Value then printfn "Skip checking for clean working copy since -c is specified"
            else
                let! changes = text $"git status --porcelain"
                if changes = "" then printfn "The checkout folder does not have pending changes, proceeding"
                else failBuild "The checkout folder has pending changes, aborting. Specify -c to ./build.sh to skip this check"

            if skipDirtyCheck.Value then printfn "Skip formatting checks since -c is specified"
            else
                let! code = exitCode $"dotnet curb check . --cache .artifacts/curb.cache"
                if code = 0 then printfn "No formatting violations found."
                else failBuild "Formatting violations found. Run `dotnet curb format .` to fix, or specify -c to ./build.sh to skip this check" }
        | ValidateLicenses -> step [] {
            sh $"dotnet dotnet-project-licenses -u -t -i docs-builder.sln --use-project-assets-json --forbidden-license-types build/forbidden-license-types.json --packages-filter {licenseFilter}" }

        | Release -> command {
            requires [PristineCheck; Build]
            composes [ValidateLicenses] }
        | Publish -> command { composes [PublishBinaries; PublishContainers] }

        | PublishBinaries -> step [] { sh $"dotnet publish src/tooling/docs-builder/docs-builder.csproj" }
        | PublishZip -> step [] {
            for tool in [ "docs-builder"; "docs-journeys" ] do
                sh $"dotnet publish src/tooling/{tool}/{tool}.csproj"
                zipTool tool }
        | PublishContainers -> step [] {
            let! onTag = succeeds $"git describe --tags --exact-match HEAD"
            let labels = imageTags onTag
            let ci = envVarOrNone "GITHUB_ACTIONS"
            let noPublish = envVarOrNone "DOCKER_NO_PUBLISH"
            let registry =
                match ci, prNumber (), noPublish with
                | Some _, None, None -> [ "-p"; "ContainerRegistry=ghcr.io"; "-p"; "ContainerUser=1001:1001" ]
                | _ -> []
            let baseImage = "mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute-chiseled"
            // The quotes stay part of the value, as they always did.
            let tags = $"ContainerImageTags=\"%s{labels};%s{Software.Version.Semantic}\""
            for project, name in containerProjects do
                let publish =
                    sh $"dotnet publish {project} /t:PublishContainer -p DebugType=none -p ContainerBaseImage={baseImage} -p {tags} -p ContainerRepository=elastic/{name} {registry}"
                // A push to the registry is retried; a local image is not.
                if List.isEmpty registry then publish else publish |> Cmd.retry 2 }
        | RunLocalContainer -> step [] {
            let! onTag = succeeds $"git describe --tags --exact-match HEAD"
            let tag = match imageTags onTag with ci when ci.StartsWith "ci-" -> ci | _ -> "edge"
            sh $"docker run elastic/docs-builder:{tag} --help"
            sh $"docker run elastic/website-search-essc:{tag} --help" }

        | AirGappedBuild -> target [] {
            let assemblyDir = Path.Combine(Paths.Root.FullName, ".artifacts", "assembly")
            if Directory.Exists assemblyDir then Directory.Delete(assemblyDir, true)

            sh $"dotnet run --project src/tooling/docs-builder -- assembler clone --environment air-gapped"
            sh $"dotnet run --project src/tooling/docs-builder -- assembler build --exporters html --environment air-gapped"
            let searchDir = Path.Combine(assemblyDir, "docs", "_search")
            if Directory.Exists searchDir then Directory.Delete(searchDir, true)

            File.WriteAllText(Path.Combine(assemblyDir, "index.html"), """<meta http-equiv="refresh" content="0;url=/docs">""")
            gzipCompressDirectory assemblyDir

            sh $"docker build -f build/air-gapped/Dockerfile --build-context content=.artifacts/assembly --build-context config=build/air-gapped -t elastic-docs-air-gapped build/air-gapped" }
        | AirGappedRun -> target [] {
            printfn "Running at http://localhost:8080"
            sh $"docker run -p 127.0.0.1:8080:8080 elastic-docs-air-gapped" }

    // Make.Fs only hands over the option once the Compile dependency has run, so check the value here to fail a typo fast.
    let isSuite (value: string) =
        // TryParse also accepts numbers and comma lists, so require a declared name
        match Enum.TryParse<TestSuite>(value, true) with
        | true, suite -> Enum.IsDefined suite
        | _ -> false
    argv
    |> Array.tryFindIndex (fun a -> a = "--suite")
    |> Option.bind (fun i -> Array.tryItem (i + 1) argv)
    |> Option.iter (fun suite ->
        if not (isSuite suite) then
            eprintfn "Unknown test suite '%s', expected all, unit or integration" suite
            exit 2)

    app.RunAsync(argv).GetAwaiter().GetResult()
