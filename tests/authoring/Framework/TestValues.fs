// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.IO.Abstractions
open System.Threading
open System.Threading.Tasks
open Elastic.Markdown
open Elastic.Markdown.Diagnostics
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


type MarkdownResult = {
    File: MarkdownFile
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
       MarkdownFiles: MarkdownFile seq
       Collector: TestDiagnosticsCollector
       Set: DocumentationSet
       Generator: DocumentationGenerator
       ReadFileSystem: IFileSystem
       WriteFileSystem: IFileSystem
    }

    member this.Bootstrap () = backgroundTask {
        let! ctx = Async.CancellationToken
        do! this.Generator.GenerateAll(ctx)

        let results =
            this.MarkdownFiles
            |> Seq.map (fun (f: MarkdownFile) -> task {
                // technically we do this work twice since generate all also does it
                let! document = f.ParseFullAsync(ctx)
                let html = f.CreateHtml(document)
                return { File = f; Document = document; Html = html; Context = this  }
            })
            // this is not great code, refactor or depend on FSharp.Control.TaskSeq
            // for now this runs without issue
            |> Seq.map (fun t -> t |> Async.AwaitTask |> Async.RunSynchronously)

        return { Context = this; MarkdownResults = results }
    }

    interface IDisposable with
        member this.Dispose() = ()