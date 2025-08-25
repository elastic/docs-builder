// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``applies_to heading validation``

open Xunit
open authoring
open authoring.MarkdownDocumentAssertions

type ``inline applies_to in h1 heading`` () =
    static let markdown = Setup.Markdown """
# Heading {applies_to}`stack: ga`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about inline applies_to in heading`` () =
        markdown |> hasWarning "Inline applies_to should not be used in headings. Use section-level applies_to directives instead."

type ``inline applies_to in h2 heading`` () =
    static let markdown = Setup.Markdown """
## Heading {applies_to}`stack: ga 9.1`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about inline applies_to in heading`` () =
        markdown |> hasWarning "Inline applies_to should not be used in headings. Use section-level applies_to directives instead."

type ``inline applies_to in h3 heading`` () =
    static let markdown = Setup.Markdown """
### Heading {applies_to}`serverless: beta`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about inline applies_to in heading`` () =
        markdown |> hasWarning "Inline applies_to should not be used in headings. Use section-level applies_to directives instead."

type ``inline applies_to in h4 heading`` () =
    static let markdown = Setup.Markdown """
#### Heading {applies_to}`ece: deprecated 9.2`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about inline applies_to in heading`` () =
        markdown |> hasWarning "Inline applies_to should not be used in headings. Use section-level applies_to directives instead."

type ``inline applies_to in h5 heading`` () =
    static let markdown = Setup.Markdown """
##### Heading {applies_to}`product: preview 9.5`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about inline applies_to in heading`` () =
        markdown |> hasWarning "Inline applies_to should not be used in headings. Use section-level applies_to directives instead."

type ``inline applies_to in h6 heading`` () =
    static let markdown = Setup.Markdown """
###### Heading {applies_to}`edot_dotnet: ga 1.0`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about inline applies_to in heading`` () =
        markdown |> hasWarning "Inline applies_to should not be used in headings. Use section-level applies_to directives instead."

type ``inline applies_to in paragraph text`` () =
    static let markdown = Setup.Markdown """
This is a paragraph with {applies_to}`stack: ga` inline applies_to.
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warnings`` () = markdown |> hasNoWarnings

type ``multiple inline applies_to in same heading`` () =
    static let markdown = Setup.Markdown """
## Heading {applies_to}`stack: ga` and {applies_to}`serverless: beta`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about both inline applies_to in heading`` () =
        markdown |> hasWarnings 2

type ``preview role in heading`` () =
    static let markdown = Setup.Markdown """
## Heading {preview}`9.1`
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``warns about preview role in heading`` () =
        markdown |> hasWarning "Inline applies_to should not be used in headings. Use section-level applies_to directives instead."
