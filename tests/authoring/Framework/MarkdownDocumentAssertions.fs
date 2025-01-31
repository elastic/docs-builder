// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System.Diagnostics
open Markdig.Syntax
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
        unsupportedBlocks;
