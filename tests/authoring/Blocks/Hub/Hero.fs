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

type ``hero with an external action`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:title: Elasticsearch documentation hub
:primary-action: [Install Elasticsearch](https://www.elastic.co/downloads/elasticsearch)
:::
"""

    // External links follow the same rules as inline links: they open in a new tab. The
    // assertion starts at the section, because the pretty-printer only matches from the
    // outermost element of the directive output. It also strips `preload`, so the absence
    // of preloading on an external action cannot be asserted here.
    [<Fact>]
    let ``opens in a new tab`` () =
        markdown |> convertsToContainingHtml """
<section class="hub-hero">
	<div class="hub-hero-inner">
		<div class="hub-hero-eyebrow">
			<a class="hub-hero-eyebrow-link" href="/">
				<span>Browse all Elastic docs</span>
				<svg class="hub-hero-eyebrow-arrow" width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
					<path d="M3 8h9M8.5 4l4 4-4 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
				</svg>
			</a>
		</div>
		<div class="hub-hero-top">
			<h1>Elasticsearch documentation hub</h1>
		</div>
		<div class="hub-hero-actions">
			<a class="hub-hero-action hub-hero-action-primary" href="https://www.elastic.co/downloads/elasticsearch" target="_blank" rel="noopener noreferrer">
				<span>Install Elasticsearch</span>
			</a>
		</div>
	</div>
</section>
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``hero with an internal action`` () =
    static let markdown = Setup.Markdown """
:::{hero}
:title: Elasticsearch documentation hub
:primary-action: [Syntax reference](/index.md)
:::
"""

    [<Fact>]
    let ``strips the markdown extension and does not open a new tab`` () =
        markdown |> convertsToContainingHtml """
<section class="hub-hero">
	<div class="hub-hero-inner">
		<div class="hub-hero-eyebrow">
			<a class="hub-hero-eyebrow-link" href="/">
				<span>Browse all Elastic docs</span>
				<svg class="hub-hero-eyebrow-arrow" width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
					<path d="M3 8h9M8.5 4l4 4-4 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
				</svg>
			</a>
		</div>
		<div class="hub-hero-top">
			<h1>Elasticsearch documentation hub</h1>
		</div>
		<div class="hub-hero-actions">
			<a class="hub-hero-action hub-hero-action-primary" href="/">
				<span>Syntax reference</span>
			</a>
		</div>
	</div>
</section>
"""

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
