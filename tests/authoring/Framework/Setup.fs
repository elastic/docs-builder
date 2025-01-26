namespace authoring

open System.Collections.Generic
open System.IO
open System.IO.Abstractions.TestingHelpers
open System.Threading.Tasks
open Elastic.Markdown
open Elastic.Markdown.IO
open JetBrains.Annotations

type Setup =

    static let GenerateDocSetYaml(
        fileSystem: MockFileSystem,
        globalVariables: Dictionary<string, string> option
    ) =
        let root = fileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, "docs/"));
        let yaml = new StringWriter();
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

        fileSystem.AddFile(Path.Combine(root.FullName, "docset.yml"), MockFileData(yaml.ToString()));

    static let Generate ([<LanguageInjection("markdown")>]m: string) : Task<GenerateResult> =

        let d = dict [ ("docs/index.md", MockFileData(m)) ]
        let opts = MockFileSystemOptions(CurrentDirectory=Paths.Root.FullName)
        let fileSystem = MockFileSystem(d, opts)

        GenerateDocSetYaml (fileSystem, None)

        let collector = TestDiagnosticsCollector();
        let context = BuildContext(fileSystem, Collector=collector)
        let set = DocumentationSet(context);
        let file =
            match set.GetMarkdownFile(fileSystem.FileInfo.New("docs/index.md")) with
            | NonNull f -> f
            | _ -> failwithf "docs/index.md could not be located"

        let context = {
            File = file
            Collector = collector
            Set = set
            ReadFileSystem = fileSystem
            WriteFileSystem = fileSystem
        }
        context.Bootstrap()

    /// Pass a full documentation page to the test setup
    static member Document ([<LanguageInjection("markdown")>]m: string) =
        let g = task { return! Generate m }
        g |> Async.AwaitTask |> Async.RunSynchronously

    /// Pass a markdown fragment to the test setup
    static member Markdown ([<LanguageInjection("markdown")>]m: string) =
        // language=markdown
        let m = $"""
# Test Document
{m}
"""
        let g = task {
            return! Generate m
        }
        g |> Async.AwaitTask |> Async.RunSynchronously

