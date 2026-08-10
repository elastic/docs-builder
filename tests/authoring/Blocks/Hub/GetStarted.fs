// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``hub``.``get started elements``

open Xunit
open authoring

type ``get started with a title and intro`` () =
    static let markdown = Setup.Markdown """
:::{get-started}
title: Get started in 3 steps
intro: Install, write, preview.
steps:
  - title: Install
    description: Install the CLI.
:::
"""

    [<Fact>]
    let ``renders the heading`` () =
        markdown |> convertsToContainingHtml """<h2 class="hub-get-started-title">Get started in 3 steps</h2>"""

    [<Fact>]
    let ``numbers steps from one, zero padded`` () =
        markdown |> convertsToContainingRawHtml """<span class="hub-get-started-step-num" aria-hidden="true">01</span>"""

    // Nothing renders between the intro and the numbered list. The section is the steps.
    [<Fact>]
    let ``renders nothing above the steps`` () =
        markdown |> doesNotConvertToContainingHtml "hub-get-started-actions"

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``get started with a link step`` () =
    static let markdown = Setup.Markdown """
:::{get-started}
title: Get started
steps:
  - title: Write your first page
    description: Author markdown.
    link: /index.md
    link-label: Start writing
:::
"""

    [<Fact>]
    let ``makes the whole step clickable`` () =
        markdown |> convertsToContainingHtml """<span>Start writing</span>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``get started with option steps`` () =
    static let markdown = Setup.Markdown """
:::{get-started}
title: Get started
steps:
  - title: Preview and publish
    options:
      - label: Preview locally
        description: Serve with live reload.
        code: docs-builder serve
        language: sh
      - label: Publish
        description: Build and publish.
        url: /index.md
        url-label: How to publish
:::
"""

    [<Fact>]
    let ``renders both options`` () =
        markdown |> convertsToContainingHtml """<span class="hub-get-started-option-label">Preview locally</span>"""

    [<Fact>]
    let ``renders the option command`` () =
        markdown |> convertsToContainingHtml """<code class="language-sh">docs-builder serve</code>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``get started with a relative step link`` () =
    static let markdown = Setup.Markdown """
:::{get-started}
title: Get started
steps:
  - title: Broken
    link: nope.md
:::
"""

    [<Fact>]
    let ``rejects a relative path`` () =
        markdown |> hasError "must be an absolute path starting with `/`"

type ``get started without a body`` () =
    static let markdown = Setup.Markdown """
:::{get-started}
:::
"""

    [<Fact>]
    let ``errors`` () =
        markdown |> hasError "{get-started}"
