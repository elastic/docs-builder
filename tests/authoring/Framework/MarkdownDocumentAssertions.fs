// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System.Diagnostics
open Elastic.Markdown.Myst.CodeBlocks
open Elastic.Markdown.Myst.FrontMatter
open Markdig.Syntax
open Swensen.Unquote
open Xunit.Sdk

module MarkdownDocumentAssertions =

    [<DebuggerStepThrough>]
    let parses<'element when 'element :> MarkdownObject> (actual: MarkdownResult) =
        let unsupportedBlocks = actual.Document.Descendants<'element>() |> Array.ofSeq
        if unsupportedBlocks.Length = 0 then
            raise (XunitException($"Could not find {typedefof<'element>.Name} in fully parsed document"))
        unsupportedBlocks;

    [<DebuggerStepThrough>]
    let parsesMinimal<'element when 'element :> MarkdownObject> (actual: MarkdownResult) =
        let unsupportedBlocks = actual.MinimalParse.Descendants<'element>() |> Array.ofSeq
        if unsupportedBlocks.Length = 0 then
            raise (XunitException($"Could not find {typedefof<'element>.Name} in minimally parsed document"))
        unsupportedBlocks

    [<DebuggerStepThrough>]
    let appliesTo (expectedAvailability: ApplicableTo) (actual: Lazy<GeneratorResults>) =
        let actual = actual.Value
        let result = actual.MarkdownResults |> Seq.head
        let matter = result.File.YamlFrontMatter
        match matter with
        | NonNull m ->
            let apply = m.Apply
            test <@ apply = expectedAvailability @>
        | _ -> failwithf "%s has no yamlfront matter" result.File.RelativePath


    [<DebuggerStepThrough>]
    let appliesToDirective (expectedAvailability: ApplicableTo) (actual: AppliesToDirective array) =
        let actual = actual |> Array.tryHead
        match actual with
        | Some d ->
            let apply = d.AppliesTo
            test <@ apply = expectedAvailability @>
        | _ -> failwithf "Could not locate an AppliesToDirective"

