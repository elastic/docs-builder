// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``block elements``.``code blocks``

open Elastic.Markdown.Myst.CodeBlocks
open Swensen.Unquote
open Xunit
open authoring
open authoring.MarkdownDocumentAssertions

type ``warns on invalid language`` () =
    static let markdown = Setup.Markdown """
```not-a-valid-language
```
"""

    [<Fact>]
    let ``validate HTML: generates link and alt attr`` () =
        markdown |> hasWarning "Unknown language: not-a-valid-language"

    [<Fact>]
    let ``parses to EnhancedCodeBlock`` () =
        let codeBlocks = markdown |> converts "index.md" |> parses<EnhancedCodeBlock>
        test <@ codeBlocks.Length = 1 @>
