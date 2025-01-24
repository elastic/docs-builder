module ``inline elements``.``comment block``

open Xunit
open authoring

type ``commented line`` () =

    static let markdown = Setup.Markdown """
% comment
not a comment
"""

    [<Fact>]
    let ``validate HTML: commented line should not be emitted`` () =
        markdown |> convertsToHtml """<p>not a comment</p>"""
