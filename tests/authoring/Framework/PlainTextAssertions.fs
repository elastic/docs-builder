// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Diagnostics
open Elastic.Markdown.Exporters
open JetBrains.Annotations
open Xunit.Sdk

[<AutoOpen>]
module PlainTextAssertions =

    let toPlainText (actual: MarkdownResult) =
        PlainTextExporter.ConvertToPlainText(actual.Document, actual.Context.Generator.Context).Trim()

    [<DebuggerStepThrough>]
    let convertsToPlainText ([<LanguageInjection("text")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let results = actual.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let actualPlainText = toPlainText defaultFile
        let expectedTrimmed = expected.Trim()
        let difference = diff expectedTrimmed actualPlainText
        match difference with
        | s when String.IsNullOrEmpty s -> ()
        | d ->
            let msg = $"""Plain text was not equal
-- DIFF --
{d}

-- EXPECTED --
{expectedTrimmed}

-- ACTUAL --
{actualPlainText}
"""
            raise (XunitException(msg))
