// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``hub``.``whats new elements``

open Xunit
open authoring

// The :product: path reads hub-whats-new.yml from the documentation set root. The authoring
// harness builds from a temporary set with no such file, so these cover the inline-body path
// and the error raised when a product key cannot be resolved.

type ``whats new with an inline body`` () =
    static let markdown = Setup.Markdown """
:::{whats-new}
title: What's new in docs-builder
id: whats-new
intro: Recent additions to the toolchain.
items:
  - title: Hub pages
    description: A product-scoped landing page.
    link: /index.md
    date: AUG 2026
    tag: Syntax
    featured: true
  - title: Explore sections
    description: Collapse a long link list.
    link: /index.md
    date: AUG 2026
    tag: Syntax
:::
"""

    [<Fact>]
    let ``renders the heading and intro`` () =
        markdown |> convertsToContainingHtml """<h2 class="hub-wn-title">What's new in docs-builder</h2>"""

    [<Fact>]
    let ``renders a card per item`` () =
        markdown |> convertsToContainingHtml """<h3 class="hub-wn-card-title">Hub pages</h3>"""

    [<Fact>]
    let ``spans the featured card across two columns`` () =
        markdown |> convertsToContainingRawHtml """<li class="hub-wn-card hub-wn-card-featured">"""

    [<Fact>]
    let ``renders the date and tag`` () =
        markdown |> convertsToContainingHtml """<span class="hub-wn-card-tag">Syntax</span>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``whats new with release and upgrade links`` () =
    static let markdown = Setup.Markdown """
:::{whats-new}
title: What's new
release-links:
  - label: View release notes
    url: /index.md
upgrade-link:
  label: Upgrade
  url: /index.md
:::
"""

    [<Fact>]
    let ``renders the release link`` () =
        markdown |> convertsToContainingHtml """<a class="hub-wn-rn-link" href="/">View release notes</a>"""

    [<Fact>]
    let ``renders the upgrade prompt`` () =
        markdown |> convertsToContainingHtml """<span class="hub-wn-footer-text">Ready to move to the latest version?</span>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``whats new with an unresolvable product`` () =
    static let markdown = Setup.Markdown """
:::{whats-new}
:product: not-a-product
:::
"""

    [<Fact>]
    let ``errors and names the file it looked in`` () =
        markdown |> hasError "hub-whats-new.yml"

type ``whats new with neither product nor body`` () =
    static let markdown = Setup.Markdown """
:::{whats-new}
:::
"""

    [<Fact>]
    let ``errors`` () =
        markdown |> hasError "requires either a `:product:` option or a YAML body"

type ``whats new with a relative item link`` () =
    static let markdown = Setup.Markdown """
:::{whats-new}
title: What's new
items:
  - title: Broken
    link: nope.md
:::
"""

    [<Fact>]
    let ``rejects a relative path`` () =
        markdown |> hasError "must be an absolute path starting with `/`"
