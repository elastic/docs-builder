// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``kbd role``

open Elastic.Markdown.Myst.FrontMatter
open Elastic.Markdown.Myst.Roles.AppliesTo
open Swensen.Unquote
open Xunit
open authoring
open authoring.MarkdownDocumentAssertions

type ``parses inline {kbd} role`` () =
    static let markdown = Setup.Markdown """
Press {kbd}`Ctrl` + {kbd}`S` to save.
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<p>Press <kbd>Ctrl</kbd> + <kbd>S</kbd> to save.</p>
"""
