// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring


open System
open System.Collections.Generic
open System.IO
open System.IO.Abstractions.TestingHelpers
open System.Threading.Tasks
open Elastic.Markdown
open Elastic.Markdown.CrossLinks
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

    static member Index ([<LanguageInjection("markdown")>] m) =
        MarkdownFile("index.md" , m)

    static member Markdown path ([<LanguageInjection("markdown")>] m) =
        MarkdownFile(path , m)

type Setup =

    static let GenerateDocSetYaml(
        fileSystem: MockFileSystem,
        globalVariables: Dictionary<string, string> option
    ) =
        let root = fileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, "docs/"));
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
        let redirectYaml = new StringWriter();
        // language=yaml
        redirectYaml.WriteLine("""redirects:
  'testing/redirects/4th-page.md': 'testing/redirects/5th-page.md'
  'testing/redirects/9th-page.md': '!testing/redirects/5th-page.md'
  'testing/redirects/6th-page.md':
  'testing/redirects/7th-page.md':
    to: 'testing/redirects/5th-page.md'
    anchors: '!'
  'testing/redirects/first-page-old.md':
    to: 'testing/redirects/second-page.md'
    anchors:
      'old-anchor': 'active-anchor'
      'removed-anchor':
  'testing/redirects/second-page-old.md':
    many:
      - to: "testing/redirects/second-page.md"
        anchors:
          "aa": "zz"
          "removed-anchor":
      - to: "testing/redirects/third-page.md"
        anchors:
          "bb": "yy"
  'testing/redirects/third-page.md':
    anchors:
      'removed-anchor':
        """)
        fileSystem.AddFile(Path.Combine(root.FullName, redirectsName), MockFileData(redirectYaml.ToString()))

    static member Generator (files: TestFile seq) : Task<GeneratorResults> =

        let d = files
                |> Seq.map (fun f ->
                    match f with
                    | File(name, contents) -> ($"docs/{name}", MockFileData(contents))
                    | MarkdownFile(name, markdown) -> ($"docs/{name}", MockFileData(markdown))
                )
                |> Map.ofSeq

        let opts = MockFileSystemOptions(CurrentDirectory=Paths.Root.FullName)
        let fileSystem = MockFileSystem(d, opts)

        GenerateDocSetYaml (fileSystem, None)

        let collector = TestDiagnosticsCollector();
        let context = BuildContext(collector, fileSystem)
        let logger = new TestLoggerFactory()
        let linkResolver = TestCrossLinkResolver(context.Configuration)
        let set = DocumentationSet(context, logger, linkResolver);
        let generator = DocumentationGenerator(set, logger)

        let markdownFiles =
            files
            |> Seq.map (fun f ->
                match f with
                | File _ -> None
                | MarkdownFile(name, _) -> Some $"docs/{name}"
            )
            |> Seq.choose id
            |> Seq.map (fun f ->
                match set.GetMarkdownFile(fileSystem.FileInfo.New(f)) with
                | NonNull m -> Some m
                | _ -> None
             )
            |> Seq.choose id

        let context = {
            MarkdownFiles = markdownFiles
            Collector = collector
            Set = set
            Generator = generator
            ReadFileSystem = fileSystem
            WriteFileSystem = fileSystem
        }
        context.Bootstrap()

    /// Pass several files to the test setup
    static member Generate files =
        lazy (task { return! Setup.Generator files } |> Async.AwaitTask |> Async.RunSynchronously)

    /// Pass a full documentation page to the test setup
    static member Document ([<LanguageInjection("markdown")>]m: string) =
        lazy (task { return! Setup.Generator [Index m] } |> Async.AwaitTask |> Async.RunSynchronously)

    /// Pass a markdown fragment to the test setup
    static member Markdown ([<LanguageInjection("markdown")>]m: string) =
        // language=markdown
        let m = $"""# Test Document
{m}
"""
        lazy (
            task { return! Setup.Generator [Index m] }
            |> Async.AwaitTask |> Async.RunSynchronously
        )
