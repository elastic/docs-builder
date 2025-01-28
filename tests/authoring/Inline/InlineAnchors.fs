// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``anchors DEPRECATED``

open Elastic.Markdown.Myst.InlineParsers
open Markdig.Syntax
open Swensen.Unquote
open System.Linq
open Xunit
open authoring
open authoring.MarkdownDocumentAssertions

type ``inline anchor in the middle`` () =

    static let markdown = Setup.Markdown """
this is *regular* text and this $$$is-an-inline-anchor$$$ and this continues to be regular text
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p>this is <em>regular</em> text and this
                <a id="is-an-inline-anchor"></a> and this continues to be regular text
            </p>
            """
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``inline anchors embedded in definition lists`` () =

    static let markdown = Setup.Generate [
        Index """# Testing nested inline anchors

$$$search-type$$$

`search_type`
:   (Optional, string) How distributed term frequencies are calculated for relevance scoring.

    ::::{dropdown} Valid values for `search_type`
    `query_then_fetch`
    :   (Default) Distributed term frequencies are calculated locally for each shard running the search. We recommend this option for faster searches with potentially less accurate scoring.

    $$$dfs-query-then-fetch$$$

    `dfs_query_then_fetch`
    :   Distributed term frequencies are calculated globally, using information gathered from all shards running the search.

    ::::
"""
        Markdown "file.md" """
 [Link to first](index.md#search-type)
 [Link to second](index.md#dfs-query-then-fetch)
        """
    ]

    [<Fact>]
    let ``emits nested inline anchor`` () =
        markdown |> convertsToContainingHtml """<a id="dfs-query-then-fetch"></a>"""

    [<Fact>]
    let ``emits definition list block anchor`` () =
        markdown |> convertsToContainingHtml """<a id="search-type"></a>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``minimal parse sees two inline anchors`` () =
        let inlineAnchors = markdown |> converts "index.md" |> parsesMinimal<InlineAnchor>
        test <@ inlineAnchors.Length = 2 @>



type ``inline anchors embedded in indented code`` () =

    static let markdown = Setup.Generate [
        Index """# Testing nested inline anchors

$$$search-type$$$

    indented codeblock

    $$$dfs-query-then-fetch$$$

    block
"""
        Markdown "file.md" """
 [Link to first](index.md#search-type)
 [Link to second](index.md#dfs-query-then-fetch)
        """
    ]

    [<Fact>]
    let ``emits nested inline anchor`` () =
        markdown |> convertsToContainingHtml """<a id="dfs-query-then-fetch"></a>"""

    [<Fact>]
    let ``emits definition list block anchor`` () =
        markdown |> convertsToContainingHtml """<a id="search-type"></a>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``minimal parse sees two inline anchors`` () =
        let inlineAnchors = markdown |> converts "index.md" |> parsesMinimal<InlineAnchor>
        test <@ inlineAnchors.Length = 2 @>
