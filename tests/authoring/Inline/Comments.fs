// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

