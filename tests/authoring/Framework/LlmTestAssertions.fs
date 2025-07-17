// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Diagnostics
open System.IO
open Elastic.Markdown.Exporters
open Elastic.Markdown.Myst.Renderers
open JetBrains.Annotations
open Xunit.Sdk

[<AutoOpen>]
module LlmTestAssertions =

    let toNewLLM (actual: MarkdownResult) =
        use writer = new StringWriter()  
        let markdownExportFileContext = MarkdownExportFileContext(
            BuildContext = actual.Context.Generator.Context,
            Resolvers = actual.Context.Set.MarkdownParser.Resolvers,
            Document = actual.Document,
            SourceFile = actual.File,
            DefaultOutputFile = actual.File.SourceFile
        )
        LlmMarkdownExporter.ConvertToLlmMarkdown(actual.Document, markdownExportFileContext).Trim()


    [<DebuggerStepThrough>]
    let convertsToNewLLM ([<LanguageInjection("markdown")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let results = actual.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let actualLLM = toNewLLM defaultFile

        // The new exporter adds a title, so we need to account for that in the expected output.
        let expectedWithTitle = $"{expected}".Trim()

        let difference = diff expectedWithTitle actualLLM
        match difference with
        | s when String.IsNullOrEmpty s -> ()
        | d ->
            let msg = $"""LLM text was not equal
-- DIFF --
{d}

-- EXPECTED --
{expectedWithTitle}

-- ACTUAL --
{actualLLM}
"""
            raise (XunitException(msg))
