// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring


open System
open System.Collections.Generic
open System.IO
open System.IO.Abstractions.TestingHelpers
open System.Threading.Tasks
open Elastic.Documentation.Configuration
open Elastic.Markdown
open Elastic.Markdown.IO
open JetBrains.Annotations
open Xunit

[<assembly: CaptureConsole>]
do()

type Markdown = string

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
    { UrlPathPrefix: string option }
    static member Empty = {
        UrlPathPrefix = None
    }

type Setup =

    static let GenerateDocSetYaml(
        fileSystem: MockFileSystem,
        globalVariables: Dictionary<string, string> option
    ) =
        let root = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs/"));
        let yaml = new StringWriter();
        yaml.WriteLine("cross_links:");
        yaml.WriteLine("  - docs-content");
        yaml.WriteLine("  - elasticsearch");
        yaml.WriteLine("  - kibana")
        yaml.WriteLine("toc:");
        let markdownFiles = fileSystem.Directory.EnumerateFiles(root.FullName, "*.md", SearchOption.AllDirectories)
        markdownFiles
        |> Seq.iter(fun markdownFile ->
            let relative = fileSystem.Path.GetRelativePath(root.FullName, markdownFile);
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
        let redirectYaml = File.ReadAllText(Path.Combine(root.FullName, "_redirects.yml"))
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

        GenerateDocSetYaml (fileSystem, None)

        let collector = TestDiagnosticsCollector()
        let context = BuildContext(collector, fileSystem, UrlPathPrefix=(options.UrlPathPrefix |> Option.defaultValue ""))
        let logger = new TestLoggerFactory()
        let conversionCollector = TestConversionCollector()
        let linkResolver = TestCrossLinkResolver(context.Configuration)
        let set = DocumentationSet(context, logger, linkResolver);
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