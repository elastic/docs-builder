// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``layout``.``global layout shell``

#nowarn 3261

open AngleSharp.Html.Parser
open Swensen.Unquote
open Xunit
open authoring

let private generatedDocument =
    Setup.Document """
# Test page

Test content.
"""

let private parseDocument () =
    let result =
        generatedDocument.Value.MarkdownResults
        |> Seq.find (fun r -> r.File.RelativePath = "index.md")
    HtmlParser().ParseDocument(result.Html)

type ``global shell semantics`` () =
    [<Fact>]
    let ``renders a valid root and one htmx indicator attribute`` () =
        let document = parseDocument ()
        test <@ not (document.DocumentElement.HasAttribute("xmlns")) @>
        test <@ document.Body.GetAttribute("hx-indicator") = "#htmx-indicator" @>
        test <@ document.QuerySelectorAll("#htmx-indicator").Length = 1 @>

    [<Fact>]
    let ``provides keyboard bypass navigation`` () =
        let document = parseDocument ()
        let skipLink = document.QuerySelector("a[href='#main-container']")
        test <@ not (isNull skipLink) @>
        test <@ skipLink.TextContent.Trim() = "Skip to main content" @>
        test <@ not (isNull (document.QuerySelector("#main-container[tabindex='-1']"))) @>

    [<Fact>]
    let ``names documentation navigation landmarks`` () =
        let document = parseDocument ()
        test <@ not (isNull (document.QuerySelector("nav#pages-nav[aria-label='Documentation sections']"))) @>
        test <@ not (isNull (document.QuerySelector("nav#toc-nav[aria-label='Page tools and contents']"))) @>

    [<Fact>]
    let ``does not nest interactive controls`` () =
        let document = parseDocument ()
        test <@ document.QuerySelectorAll("button a, a button").Length = 0 @>
