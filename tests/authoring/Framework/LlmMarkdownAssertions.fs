// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Diagnostics
open System.IO
open Elastic.Markdown.Exporters
open JetBrains.Annotations
open Xunit.Sdk

[<AutoOpen>]
module LlmMarkdownAssertions =

    let toLlmMarkdown (actual: MarkdownResult) =
        use writer = new StringWriter()  
        LlmMarkdownExporter.ConvertToLlmMarkdown(actual.Document, actual.Context.Generator.Context).Trim()

    [<DebuggerStepThrough>]
    let convertsToNewLLM ([<LanguageInjection("markdown")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let results = actual.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let actualLLM = toLlmMarkdown defaultFile
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
    
    [<DebuggerStepThrough>]
    let llmOutputContains (expectedText: string) (actual: Lazy<GeneratorResults>) =
        // Check if the document content contains the expected text
        let results = actual.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let actualLLM = toLlmMarkdown defaultFile
        
        if not (actualLLM.Contains(expectedText)) then
            let msg = $"""LLM output does not contain expected text

-- EXPECTED TO CONTAIN --
{expectedText}

-- ACTUAL OUTPUT --
{actualLLM}
"""
            raise (XunitException(msg))
