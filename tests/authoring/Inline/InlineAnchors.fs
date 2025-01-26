// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
            <p>this is <em>regular</em> text and this
                <a id="is-an-inline-anchor"></a> and this continues to be regular text
            </p>
            """
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors
