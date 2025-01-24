module ``inline elements``.``anchors DEPRECATED``

open Xunit
open authoring

type ``inline anchor in the middle`` () =

    static let markdown = Setup.Markdown """
this is *regular* text and this $$$is-an-inline-anchor$$$ and this continues to be regular text
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p>this is <em>regular</em> text and this <a id="is-an-inline-anchor"></a> and this continues to be regular text</p>
            """
