namespace authoring

open System
open System.IO.Abstractions
open Elastic.Markdown.Diagnostics
open Elastic.Markdown.IO
open Markdig.Syntax
open Xunit


type TestDiagnosticsOutput() =

    interface IDiagnosticsOutput with
        member this.Write diagnostic =
            let line = match diagnostic.Line with | NonNullV l -> l | _ -> 0
            match TestContext.Current.TestOutputHelper with
            | NonNull output ->
                match diagnostic.Severity with
                | Severity.Error ->
                    output.WriteLine($"Error: {diagnostic.Message} ({diagnostic.File}:{line})")
                | _ ->
                    output.WriteLine($"Warn : {diagnostic.Message} ({diagnostic.File}:{line})")
            | _ -> ()


type TestDiagnosticsCollector() =
    inherit DiagnosticsCollector([TestDiagnosticsOutput()])

    let diagnostics = System.Collections.Generic.List<Diagnostic>()

    member _.Diagnostics = diagnostics.AsReadOnly()

    override this.HandleItem diagnostic = diagnostics.Add(diagnostic);

type GenerateResult = {
    Document: MarkdownDocument
    Html: string
    Context: MarkdownTestContext
}

and MarkdownTestContext =
    {
       File: MarkdownFile
       Collector: TestDiagnosticsCollector
       Set: DocumentationSet
       ReadFileSystem: IFileSystem
       WriteFileSystem: IFileSystem
    }

    member this.Bootstrap () = backgroundTask {
        let! ctx = Async.CancellationToken
        let _ = this.Collector.StartAsync(ctx)
        do! this.Set.ResolveDirectoryTree(ctx)

        let! document = this.File.ParseFullAsync(ctx)

        let html = this.File.CreateHtml(document);
        this.Collector.Channel.TryComplete()
        do! this.Collector.StopAsync(ctx)
        return { Context = this; Document = document; Html = html }
    }

    interface IDisposable with
        member this.Dispose() = ()


