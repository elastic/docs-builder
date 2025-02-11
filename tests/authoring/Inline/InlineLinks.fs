// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``links``

open Xunit
open authoring

type ``inline link with mailto`` () =

    static let markdown = Setup.Markdown """
[email me](mailto:fake-email@elastic.co)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p>
	            <a href="mailto:fake-email@elastic.co">email me</a>
            </p>
        """
        
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``inline link with mailto not allowed external host`` () =

    static let markdown = Setup.Markdown """
[email me](mailto:fake-email@somehost.co)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p>
	            <a href="mailto:fake-email@somehost.co">email me</a>
            </p>
        """
        
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has warning`` () = markdown |> hasWarning "External URI 'mailto:fake-email@somehost.co' is not allowed."
