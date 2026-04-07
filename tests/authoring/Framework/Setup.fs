// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring


open System
open System.Collections.Frozen
open System.Collections.Generic
open System.IO
open System.IO.Abstractions.TestingHelpers
open System.Threading.Tasks
open YamlDotNet.RepresentationModel
open Elastic.Documentation
open Elastic.Documentation.Configuration
open Elastic.Documentation.Configuration.LegacyUrlMappings
open Elastic.Documentation.Configuration.Versions
open Elastic.Documentation.Configuration.Products
open Elastic.Documentation.Configuration.Search
open Elastic.Markdown
open Elastic.Markdown.IO
open JetBrains.Annotations
open Xunit

[<assembly: CaptureConsole>]
do()

type Markdown = string

/// For each local redirect target in production `docs/_redirects.yml`, if that path exists under the real `docs/`
/// tree, ensures the mock has a markdown file at the same relative path. Content is a minimal stub (not a byte copy
/// of the real file) so redirect validation passes without running the full docset through production pages.
[<RequireQualifiedAccess>]
module private RedirectMockTargets =
    let private stubMarkdown =
        """---
navigation_title: Stub
---
# Stub
"""

    let private addLocal (path: string) (set: Set<string>) =
        if String.IsNullOrWhiteSpace path then
            set
        else
            let t = path.TrimStart('!')
            if t.Contains("://") then set else Set.add t set

    let private readScalar (node: YamlNode) =
        match node with
        | :? YamlScalarNode as s -> s.Value
        | _ -> null

    let private collectMany (outerFromKey: string) (seq: YamlSequenceNode) =
        seq.Children
        |> Seq.fold
            (fun acc (node: YamlNode) ->
                match node with
                | :? YamlMappingNode as m ->
                    let mutable toVal = None
                    let mutable hasAnchors = false
                    for kv in m.Children do
                        match kv.Key with
                        | :? YamlScalarNode as k ->
                            match k.Value with
                            | "to" ->
                                match readScalar kv.Value with
                                | null -> ()
                                | s -> toVal <- Some s
                            | "anchors" -> hasAnchors <- true
                            | _ -> ()
                        | _ -> ()

                    let effectiveTo =
                        match toVal with
                        | Some t -> t
                        | None when hasAnchors -> outerFromKey
                        | None -> outerFromKey

                    addLocal effectiveTo acc
                | _ -> acc)
            Set.empty

    let collectRedirectTargetPaths (yamlText: string) =
        let stream = YamlStream()
        use reader = new StringReader(yamlText)
        stream.Load(reader)

        if stream.Documents.Count = 0 then
            Set.empty
        else
            match stream.Documents[0].RootNode with
            | :? YamlMappingNode as rootMap ->
                match
                    rootMap.Children
                    |> Seq.tryPick (fun kv ->
                        match kv.Key with
                        | :? YamlScalarNode as k when k.Value = "redirects" -> Some(kv.Value :?> YamlMappingNode)
                        | _ -> None)
                with
                | None -> Set.empty
                | Some redirectsMap ->
                    redirectsMap.Children
                    |> Seq.fold
                        (fun acc kv ->
                            let fromNode = kv.Key
                            let valueNode = kv.Value

                            let fromKey =
                                match fromNode with
                                | :? YamlScalarNode as s -> s.Value |> Option.ofObj |> Option.defaultValue ""
                                | _ -> ""

                            match valueNode with
                            | :? YamlScalarNode as s ->
                                let v = s.Value |> Option.ofObj |> Option.defaultValue ""

                                if String.IsNullOrEmpty v then
                                    addLocal "index.md" acc
                                else
                                    addLocal v acc
                            | :? YamlMappingNode as m ->
                                let mutable toVal = None
                                let mutable manyNode = None

                                for child in m.Children do
                                    match child.Key with
                                    | :? YamlScalarNode as k ->
                                        match k.Value with
                                        | "to" ->
                                            match readScalar child.Value with
                                            | null -> ()
                                            | s -> toVal <- Some s
                                        | "many" ->
                                            match child.Value with
                                            | :? YamlSequenceNode as seq -> manyNode <- Some seq
                                            | _ -> ()
                                        | _ -> ()
                                    | _ -> ()

                                let acc =
                                    match toVal, manyNode with
                                    | Some t, _ -> addLocal t acc
                                    | None, None -> addLocal fromKey acc
                                    | None, Some _ -> acc

                                match manyNode with
                                | Some seq -> Set.union acc (collectMany fromKey seq)
                                | None -> acc
                            | _ -> acc)
                        Set.empty
            | _ -> Set.empty

    let copyTargetsFromRealDocsIntoMock (fileSystem: MockFileSystem) (redirectYaml: string) (mockDocsRoot: string) =
        let targets = collectRedirectTargetPaths redirectYaml
        let repoRoot = Paths.WorkingDirectoryRoot.FullName

        targets
        |> Set.iter (fun rel ->
            let normalized = rel.Replace('/', Path.DirectorySeparatorChar)
            let destPath = Path.Combine(mockDocsRoot, normalized)

            // Tests supply their own minimal pages; do not replace them with production content.
            if not (fileSystem.File.Exists destPath) then
                let sourcePath = Path.Combine(repoRoot, "docs", normalized)

                if File.Exists sourcePath then
                    fileSystem.AddFile(destPath, MockFileData(stubMarkdown)))

[<AutoOpen>]
type TestFile =
    | File of name: string * contents: string
    | MarkdownFile of name: string * markdown: Markdown
    | SnippetFile of name: string * markdown: Markdown
    | StaticFile of name: string

    static member Index ([<LanguageInjection("markdown")>] m) =
        MarkdownFile("index.md" , m)

    static member Markdown path ([<LanguageInjection("markdown")>] m) =
        MarkdownFile(path , m)

    static member Static path = StaticFile(path)

    static member Snippet path ([<LanguageInjection("markdown")>] m) =
        SnippetFile(path , m)

type SetupOptions =
    { UrlPathPrefix: string option
      DocsetProducts: string list option }
    static member Empty = {
        UrlPathPrefix = None
        DocsetProducts = None
    }

type Setup =

    static let GenerateDocSetYaml(
        fileSystem: MockFileSystem,
        globalVariables: Dictionary<string, string> option,
        docsetProducts: string list option
    ) =
        let root = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs/"));
        let redirectYaml = File.ReadAllText(Path.Combine(root.FullName, "_redirects.yml"))
        RedirectMockTargets.copyTargetsFromRealDocsIntoMock fileSystem redirectYaml root.FullName

        let yaml = new StringWriter();
        yaml.WriteLine("cross_links:")
        yaml.WriteLine("  - docs-content")
        yaml.WriteLine("  - elasticsearch")
        yaml.WriteLine("  - kibana")
        yaml.WriteLine("exclude:")
        yaml.WriteLine("  - '_*.md'")
        
        // Add docset-level products if specified
        match docsetProducts with
        | Some products when products.Length > 0 ->
            yaml.WriteLine("products:")
            products |> List.iter (fun p -> yaml.WriteLine($"  - id: {p}"))
        | _ -> ()
        
        yaml.WriteLine("toc:")
        let markdownFiles = fileSystem.Directory.EnumerateFiles(root.FullName, "*.md", SearchOption.AllDirectories)
        markdownFiles
        |> Seq.iter(fun markdownFile ->
            let relative = fileSystem.Path.GetRelativePath(root.FullName, markdownFile);
            // Skip files that match the exclusion pattern (any path segment starting with _)
            let pathSegments = relative.Split([|'/'; '\\'|], StringSplitOptions.RemoveEmptyEntries)
            let shouldExclude = pathSegments |> Array.exists (fun segment -> segment.StartsWith("_"))
            if not shouldExclude then
                yaml.WriteLine($" - file: {relative}");
        )
        let redirectFiles = ["5th-page"; "second-page"; "third-page"; "first-page"]
        redirectFiles
        |> Seq.iter(fun file ->
            let relative = $"testing/redirects/{file}.md"
            yaml.WriteLine($" - file: {relative}")
            let fullPath = Path.Combine(root.FullName, relative)
            let contents = File.ReadAllText fullPath
            fileSystem.AddFile(fullPath, MockFileData(contents))
        )

        match globalVariables with
        | Some vars ->
            yaml.WriteLine($"subs:")
            vars |> Seq.iter(fun kv ->
                yaml.WriteLine($"  {kv.Key}: {kv.Value}");
            )
        | _ -> ()

        let name = if Random().Next(0, 10) % 2 = 0 then "_docset.yml" else "docset.yml"
        fileSystem.AddFile(Path.Combine(root.FullName, name), MockFileData(yaml.ToString()))

        let redirectsName = if name.StartsWith '_' then "_redirects.yml" else "redirects.yml"
        fileSystem.AddFile(Path.Combine(root.FullName, redirectsName), MockFileData(redirectYaml))

    static member Generator (files: TestFile seq) (options: SetupOptions option) : Task<GeneratorResults> =
        let options = options |> Option.defaultValue SetupOptions.Empty

        let d = files
                |> Seq.map (fun f ->
                    match f with
                    | File(name, contents) -> ($"docs/{name}", MockFileData(contents))
                    | SnippetFile(name, markdown) -> ($"docs/{name}", MockFileData(markdown))
                    | MarkdownFile(name, markdown) -> ($"docs/{name}", MockFileData(markdown))
                    | StaticFile(name) -> ($"docs/{name}", MockFileData(""))
                )
                |> Map.ofSeq

        let opts = MockFileSystemOptions(CurrentDirectory=Paths.WorkingDirectoryRoot.FullName)
        let fileSystem = MockFileSystem(d, opts)

        GenerateDocSetYaml (fileSystem, None, options.DocsetProducts)

        let collector = TestDiagnosticsCollector()
        let versioningSystems = Dictionary<VersioningSystemId, VersioningSystem>()
        versioningSystems.Add(VersioningSystemId.Stack, 
            VersioningSystem(
                Id = VersioningSystemId.Stack,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Self, 
            VersioningSystem(
                Id = VersioningSystemId.Self,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Ece, 
            VersioningSystem(
                Id = VersioningSystemId.Ece,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Eck, 
            VersioningSystem(
                Id = VersioningSystemId.Eck,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Ech, 
            VersioningSystem(
                Id = VersioningSystemId.Ech,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.ApmAgentDotnet, 
            VersioningSystem(
                Id = VersioningSystemId.ApmAgentDotnet,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.ApmAgentNode, 
            VersioningSystem(
                Id = VersioningSystemId.ApmAgentNode,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Ecctl, 
            VersioningSystem(
                Id = VersioningSystemId.Ecctl,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.ElasticsearchProject, 
            VersioningSystem(
                Id = VersioningSystemId.ElasticsearchProject,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Ess, 
            VersioningSystem(
                Id = VersioningSystemId.Ess,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.All, 
            VersioningSystem(
                Id = VersioningSystemId.All,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.ObservabilityProject, 
            VersioningSystem(
                Id = VersioningSystemId.ObservabilityProject,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.SecurityProject, 
            VersioningSystem(
                Id = VersioningSystemId.SecurityProject,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Serverless, 
            VersioningSystem(
                Id = VersioningSystemId.Serverless,
                Current = AllVersions.Instance,
                Base = AllVersions.Instance
            )
        )
        versioningSystems.Add(VersioningSystemId.ApmAgentJava, 
            VersioningSystem(
                Id = VersioningSystemId.ApmAgentJava,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.ApmAgentPython, 
            VersioningSystem(
                Id = VersioningSystemId.ApmAgentPython,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.EdotDotnet, 
            VersioningSystem(
                Id = VersioningSystemId.EdotDotnet,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.EdotJava, 
            VersioningSystem(
                Id = VersioningSystemId.EdotJava,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.EdotPython, 
            VersioningSystem(
                Id = VersioningSystemId.EdotPython,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.Curator, 
            VersioningSystem(
                Id = VersioningSystemId.Curator,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
        versioningSystems.Add(VersioningSystemId.EdotCollector, 
            VersioningSystem(
                Id = VersioningSystemId.EdotCollector,
                Current = SemVersion(8, 0, 0),
                Base = SemVersion(8, 0, 0)
            )
        )
       
        let versionConfig = VersionsConfiguration(VersioningSystems = versioningSystems)
        let productDict = Dictionary<string, Product>()
        productDict.Add("elasticsearch", Product(Id = "elasticsearch",
            DisplayName = "Elasticsearch",
            VersioningSystem = versionConfig.VersioningSystems[VersioningSystemId.ElasticsearchProject]))
        productDict.Add("apm_agent_dotnet", Product(Id = "apm_agent_dotnet",
            DisplayName = "APM Agent for .NET",
            VersioningSystem = versionConfig.VersioningSystems[VersioningSystemId.ApmAgentDotnet]))
        productDict.Add("ecctl", Product(Id = "ecctl",
            DisplayName = "Elastic Cloud Control ECCTL",
            VersioningSystem = versionConfig.VersioningSystems[VersioningSystemId.Ecctl]))
        
        let configurationFileProvider = ConfigurationFileProvider(new TestLoggerFactory(), fileSystem)
        let configurationContext = ConfigurationContext(
            VersionsConfiguration = versionConfig,
            ConfigurationFileProvider = configurationFileProvider,
            Endpoints=DocumentationEndpoints(Elasticsearch = ElasticsearchEndpoint.Default),
            ProductsConfiguration = ProductsConfiguration(
                Products = productDict.ToFrozenDictionary(),
                ProductDisplayNames = (productDict |> Seq.map (fun p -> KeyValuePair(p.Key, p.Value.DisplayName)) |> fun s -> Dictionary(s)).ToFrozenDictionary()),
            LegacyUrlMappings = LegacyUrlMappingConfiguration(Mappings = []),
            SearchConfiguration = SearchConfiguration(Synonyms = Dictionary<string, string[]>(), Rules = [], DiminishTerms = [])
        )
        let context = BuildContext(
            collector,
            FileSystemFactory.ScopeCurrentWorkingDirectory(fileSystem),
            configurationContext,
            UrlPathPrefix = (options.UrlPathPrefix |> Option.defaultValue ""),
            CanonicalBaseUrl = Uri("https://www.elastic.co/")
        )
        let logger = new TestLoggerFactory()
        let conversionCollector = TestConversionCollector()
        let linkResolver = TestCrossLinkResolver(context.Configuration)
        let set = DocumentationSet(context, logger, linkResolver)
        
        
        let generator = DocumentationGenerator(set, logger, null, null, null, null, conversionCollector)

        let context = {
            Collector = collector
            ConversionCollector= conversionCollector
            Set = set
            Generator = generator
            ReadFileSystem = fileSystem
            WriteFileSystem = fileSystem
        }
        context.Bootstrap()

    /// Pass several files to the test setup
    static member Generate files =
        lazy (task { return! Setup.Generator files None } |> Async.AwaitTask |> Async.RunSynchronously)

    static member GenerateWithOptions options files  =
        lazy (task { return! Setup.Generator files (Some options) } |> Async.AwaitTask |> Async.RunSynchronously)

    /// Pass a full documentation page to the test setup
    static member Document ([<LanguageInjection("markdown")>]m: string) =
        lazy (task { return! Setup.Generator [Index m] None } |> Async.AwaitTask |> Async.RunSynchronously)

    /// Pass a Markdown fragment to the test setup
    static member Markdown ([<LanguageInjection("markdown")>]m: string) =
        // language=markdown
        let m = $"""# Test Document
{m}
"""
        lazy (
            task { return! Setup.Generator [Index m] None }
            |> Async.AwaitTask |> Async.RunSynchronously
        )
