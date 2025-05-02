// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Collections.Concurrent
open System.IO.Abstractions
open Elastic.Documentation.Diagnostics
open Elastic.Markdown
open Elastic.Markdown.IO
open Markdig.Syntax
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
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

    override this.HandleItem diagnostic = diagnostics.Add(diagnostic)

type TestLogger () =

    interface ILogger with
        member this.IsEnabled(logger) = true
        member this.BeginScope(scope) = null
        member this.Log(logLevel, eventId, state, ex, formatter) =
            match TestContext.Current.TestOutputHelper with
            | NonNull logger ->
                let formatted = formatter.Invoke(state, ex)
                logger.WriteLine formatted
            | _ -> ()

type TestLoggerFactory () =

    interface ILoggerFactory with
        member this.AddProvider(provider) = ()
        member this.CreateLogger(categoryName) = TestLogger()
        member this.Dispose() = ()


type ConversionResult = {
    File: MarkdownFile
    Document: MarkdownDocument
    Html: string
}

type TestConversionCollector () =
    let x = ConcurrentDictionary<string, ConversionResult>()
    member this.Results = x
    interface IConversionCollector with
        member this.Collect(file, document, html) =
            this.Results.TryAdd(file.RelativePath, { File= file; Document=document;Html=html}) |> ignore


type MarkdownResult = {
    File: MarkdownFile
    MinimalParse: MarkdownDocument
    Document: MarkdownDocument
    Html: string
    Context: MarkdownTestContext
}
and GeneratorResults = {
    Context: MarkdownTestContext
    MarkdownResults: MarkdownResult seq
}

and MarkdownTestContext =
    {
       Collector: TestDiagnosticsCollector
       ConversionCollector: TestConversionCollector
       Set: DocumentationSet
       Generator: DocumentationGenerator
       ReadFileSystem: IFileSystem
       WriteFileSystem: IFileSystem
    }

    member this.Bootstrap () = backgroundTask {
        let! ctx = Async.CancellationToken
        let _ = this.Collector.StartAsync(ctx)
        do! this.Generator.GenerateAll(ctx)
        do! this.Collector.StopAsync(ctx)

        let results =
            this.ConversionCollector.Results
            |> Seq.map (fun kv -> task {
                let file = kv.Value.File
                let document = kv.Value.Document
                let html = kv.Value.Html
                let! minimal = kv.Value.File.MinimalParseAsync(ctx)
                return { File = file; Document = document; MinimalParse = minimal; Html = html; Context = this  }
            })
            // this is not great code, refactor or depend on FSharp.Control.TaskSeq
            // for now this runs without issue
            |> Seq.map (fun t -> t |> Async.AwaitTask |> Async.RunSynchronously)

        return { Context = this; MarkdownResults = results }
    }

    interface IDisposable with
        member this.Dispose() = ()