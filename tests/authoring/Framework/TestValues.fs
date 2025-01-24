namespace authoring

open System
open System.IO.Abstractions
open Elastic.Markdown.Diagnostics
open Elastic.Markdown.IO
open Markdig.Syntax

type TestResult = {
    Document: MarkdownDocument
    Html: string
    Context: MarkdownTestContext
}

and MarkdownTestContext =
    {
       File: MarkdownFile
       Collector: DiagnosticsCollector
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


