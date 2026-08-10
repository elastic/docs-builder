// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``hub``.``hero elements``

open Xunit
open authoring

type ``hero with title only`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:title: Elasticsearch documentation hub
:::
"""

    [<Fact>]
    let ``renders the title as an h1`` () =
        markdown |> convertsToContainingHtml """<h1>Elasticsearch documentation hub</h1>"""

    [<Fact>]
    let ``renders the fixed eyebrow`` () =
        markdown |> convertsToContainingHtml """<span>Browse all Elastic docs</span>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``hero without a title`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:description: Missing the required title option.
:::
"""

    [<Fact>]
    let ``errors`` () =
        markdown |> hasError "{hero} requires a `:title:` option."

type ``hero with a description`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:title: Kibana documentation hub
:description: The UI for the **Elasticsearch** platform.
:::
"""

    [<Fact>]
    let ``renders inline markup in the description`` () =
        markdown |> convertsToContainingHtml """<strong>Elasticsearch</strong>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``hero with an unknown icon key`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:icon: notaproduct
:title: Something else
:::
"""

    [<Fact>]
    let ``falls back to a letter chip`` () =
        markdown |> convertsToContainingHtml """<span class="hub-hero-icon" aria-hidden="true">N</span>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``hero with anchor actions`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:title: Elasticsearch documentation hub
:primary-action: [Get started](#get-started)
:secondary-action: [What's new](#whats-new)
:::
"""

    [<Fact>]
    let ``renders the primary action`` () =
        markdown |> convertsToContainingHtml """<span>Get started</span>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``hero with a relative action url`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:title: Elasticsearch documentation hub
:primary-action: [Get started](get-started.md)
:::
"""

    [<Fact>]
    let ``rejects a relative path`` () =
        markdown |> hasError "must be an absolute path starting with `/`"

type ``hero with a malformed action`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:title: Elasticsearch documentation hub
:primary-action: Get started
:::
"""

    [<Fact>]
    let ``errors`` () =
        markdown |> hasError "must be a markdown link"
