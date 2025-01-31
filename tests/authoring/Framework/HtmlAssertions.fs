// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Diagnostics
open System.IO
open AngleSharp.Diffing
open AngleSharp.Diffing.Core
open AngleSharp.Html
open AngleSharp.Html.Parser
open DiffPlex.DiffBuilder
open DiffPlex.DiffBuilder.Model
open JetBrains.Annotations
open Swensen.Unquote
open Xunit.Sdk

[<AutoOpen>]
module HtmlAssertions =

    let htmlDiffString (diffs: seq<IDiff>) =
        let NodeName (source:ComparisonSource) = source.Node.NodeType.ToString().ToLowerInvariant();
        let htmlText (source:IDiff) =
            let formatter = PrettyMarkupFormatter();
            let nodeText (control: ComparisonSource) =
                use sw = new StringWriter()
                control.Node.ToHtml(sw, formatter)
                sw.ToString()
            let attrText (control: AttributeComparisonSource) =
                use sw = new StringWriter()
                control.Attribute.ToHtml(sw, formatter)
                sw.ToString()
            let nodeDiffText (control: ComparisonSource option) (test: ComparisonSource option) =
                let actual = match test with Some t -> nodeText t | None -> "missing"
                let expected = match control with Some t -> nodeText t | None -> "missing"
                $"""

expected: {expected}
actual: {actual}
"""
            let attrDiffText (control: AttributeComparisonSource option) (test: AttributeComparisonSource option) =
                let actual = match test with Some t -> attrText t | None -> "missing"
                let expected = match control with Some t -> attrText t | None -> "missing"
                $"""

expected: {expected}
actual: {actual}
"""

            match source with
            | :? NodeDiff as diff -> nodeDiffText <| Some diff.Control <| Some diff.Test
            | :? AttrDiff as diff -> attrDiffText <| Some diff.Control <| Some diff.Test
            | :? MissingNodeDiff as diff -> nodeDiffText <| Some diff.Control <| None
            | :? MissingAttrDiff as diff -> attrDiffText <| Some diff.Control <| None
            | :? UnexpectedNodeDiff as diff -> nodeDiffText None <| Some diff.Test
            | :? UnexpectedAttrDiff as diff -> attrDiffText None <| Some diff.Test
            | _ -> failwith $"Unknown diff type detected: {source.GetType()}"

        diffs
        |> Seq.map (fun diff ->

            match diff with
            | :? NodeDiff as diff when diff.Target = DiffTarget.Text && diff.Control.Path.Equals(diff.Test.Path, StringComparison.Ordinal)
                -> $"The text in {diff.Control.Path} is different."
            | :? NodeDiff as diff when diff.Target = DiffTarget.Text
                -> $"The expected {NodeName(diff.Control)} at {diff.Control.Path} and the actual {NodeName(diff.Test)} at {diff.Test.Path} is different."
            | :? NodeDiff as diff when diff.Control.Path.Equals(diff.Test.Path, StringComparison.Ordinal)
                -> $"The {NodeName(diff.Control)}s at {diff.Control.Path} are different."
            | :? NodeDiff as diff -> $"The expected {NodeName(diff.Control)} at {diff.Control.Path} and the actual {NodeName(diff.Test)} at {diff.Test.Path} are different."
            | :? AttrDiff as diff when diff.Control.Path.Equals(diff.Test.Path, StringComparison.Ordinal)
                -> $"The values of the attributes at {diff.Control.Path} are different."
            | :? AttrDiff as diff -> $"The value of the attribute {diff.Control.Path} and actual attribute {diff.Test.Path} are different."
            | :? MissingNodeDiff as diff -> $"The {NodeName(diff.Control)} at {diff.Control.Path} is missing."
            | :? MissingAttrDiff as diff -> $"The attribute at {diff.Control.Path} is missing."
            | :? UnexpectedNodeDiff as diff -> $"The {NodeName(diff.Test)} at {diff.Test.Path} was not expected."
            | :? UnexpectedAttrDiff as diff -> $"The attribute at {diff.Test.Path} was not expected."
            | _ -> failwith $"Unknown diff type detected: {diff.GetType()}"
            +
            htmlText diff
        )
        |> String.concat "\n"

    let private prettyHtml (html:string) =
        let parser = HtmlParser()
        let document = parser.ParseDocument(html)
        use sw = new StringWriter()
        document.Body.Children
        |> Seq.iter _.ToHtml(sw, PrettyMarkupFormatter())
        sw.ToString().TrimStart('\n')

    let private createDiff expected actual =
        let diffs =
            DiffBuilder
                .Compare(actual)
                .WithTest(expected)
                .Build()

        let deepComparision = htmlDiffString diffs
        match deepComparision with
        | s when String.IsNullOrEmpty s -> ()
        | s ->
            let expectedHtml = prettyHtml expected
            let actualHtml = prettyHtml actual
            let textDiff = diff expectedHtml actualHtml
            let msg = $"""Html was not equal
{textDiff}

{deepComparision}
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

        let prettyExpected = prettyHtml expected
        let prettyActual = prettyHtml actual.Html

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
