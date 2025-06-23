// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``directive elements``.``include directive``

open Swensen.Unquote
open Xunit
open authoring
open authoring.MarkdownDocumentAssertions

type ``include hoists anchors and table of contents`` () =

    static let generator = Setup.Generate [
        Index """
# A Document that lives at the root

:::{include} _snippets/my-snippet.md
:::
"""
        Snippet "_snippets/my-snippet.md" """
## header from snippet [aa]

        """
        Markdown "test-links.md" """
# parent.md

## some header
[link to root with included anchor](index.md#aa)
        """
    ]

    [<Fact>]
    let ``validate index.md HTML includes snippet`` () =
        generator |> converts "index.md" |> toHtml """
            <h1>A Document that lives at the root</h1>
            <div class="heading-wrapper" id="aa">
                <h2><a class="headerlink" href="#aa">header from snippet</a></h2>
            </div>
        """

    [<Fact>]
    let ``converts to LLM`` () =
        generator |> convertsToLLM """
# A Document that lives at the root

## header from snippet [aa]

 """

    [<Fact>]
    let ``validate test-links.md HTML includes snippet`` () =
        generator |> converts "test-links.md" |> toHtml """
            <h1>parent.md</h1>
            <div class="heading-wrapper" id="some-header">
                <h2><a class="headerlink" href="#some-header">some header</a></h2>
            </div>
            <p><a href="/#aa">link to root with included anchor</a></p>
       """

    [<Fact>]
    let ``validate index.md includes table of contents`` () =
        let page = generator |> converts "index.md" |> markdownFile
        test <@ page.PageTableOfContent.Count = 1 @>
        test <@ page.PageTableOfContent.ContainsKey("aa") @>

    [<Fact>]
    let ``has no errors`` () = generator |> hasNoErrors


type ``include can contain links to parent page's includes`` () =

    static let generator = Setup.Generate [
        Index """
# A Document that lives at the root

:::{include} _snippets/my-snippet.md
:::

:::{include} _snippets/my-other-snippet.md
:::

Some more content after includes
"""
        Snippet "_snippets/my-snippet.md" """
## header from snippet [aa]
        """

        Snippet "_snippets/my-other-snippet.md" """
[link to root with included anchor](../index.md#aa)
        """
    ]

    [<Fact>]
    let ``has no errors`` () = generator |> hasNoErrors

    [<Fact>]
    let ``converts to LLM`` () =
        generator |> convertsToLLM """
# A Document that lives at the root

## header from snippet [aa]

[link to root with included anchor](../index.md#aa)

Some more content after includes
 """
