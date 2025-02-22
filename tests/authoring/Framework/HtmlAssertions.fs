// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Diagnostics
open System.IO
open AngleSharp.Html
open AngleSharp.Html.Parser
open JetBrains.Annotations
open Xunit.Sdk

[<AutoOpen>]
module HtmlAssertions =

    let private prettyHtml (html:string) (querySelector: string option) =
        let parser = HtmlParser()
        let document = parser.ParseDocument(html)
        let element =
            match querySelector with
            | Some q -> document.QuerySelector q
            | None -> document.Body
        use sw = new StringWriter()
        element.Children
        |> Seq.iter _.ToHtml(sw, PrettyMarkupFormatter())
        sw.ToString().TrimStart('\n')

    let private createDiff expected actual =
        let expectedHtml = prettyHtml expected None
        let actualHtml = prettyHtml actual (Some "section#elastic-docs-v3")
        let textDiff = diff expectedHtml actualHtml
        match textDiff with
        | s when String.IsNullOrEmpty s -> ()
        | s ->
            let msg = $"""Html was not equal
-- DIFF --
{textDiff}

-- Actual HTML --
{actualHtml}
"""
            raise (XunitException(msg))

    [<DebuggerStepThrough>]
    let toHtml ([<LanguageInjection("html")>]expected: string) (actual: MarkdownResult) =
        createDiff expected actual.Html

    [<DebuggerStepThrough>]
    let convertsToHtml ([<LanguageInjection("html")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let actual = actual.Value

        let defaultFile = actual.MarkdownResults |> Seq.head
        defaultFile |> toHtml expected

    [<DebuggerStepThrough>]
    let containsHtml ([<LanguageInjection("html")>]expected: string) (actual: MarkdownResult) =

        let prettyExpected = prettyHtml expected None
        let prettyActual = prettyHtml actual.Html (Some "section#elastic-docs-v3")

        if not <| prettyActual.Contains prettyExpected then
            let msg = $"""Expected html to contain:
{prettyExpected}

But was not found in:

{prettyActual}
"""
            raise (XunitException(msg))


    [<DebuggerStepThrough>]
    let convertsToContainingHtml ([<LanguageInjection("html")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let actual = actual.Value

        let defaultFile = actual.MarkdownResults |> Seq.head
        defaultFile |> containsHtml expected
