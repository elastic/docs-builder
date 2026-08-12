// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``hub``.``card and explore elements``

open Xunit
open authoring

// {card-group} and {link-card} render two ways. Which one is decided entirely by whether an
// {explore} ancestor is present, not by any option. These tests pin both modes and the
// ancestor detection that switches between them.

type ``card group standalone`` () =
    static let markdown = Setup.Markdown """
::::{card-group}
:title: Get hands-on
:id: hands-on
:intro: Follow a guided quickstart.

:::{link-card}
title: Writing content
link: /index.md
description: Author a page and preview it.
links:
  - label: Pages and links
    url: /index.md
:::
::::
"""

    [<Fact>]
    let ``renders a heading and a grid, not an accordion`` () =
        markdown |> convertsToContainingHtml """
<div class="hub-zone" id="hands-on">
	<h2 class="hub-zone-title">Get hands-on</h2>
	<p class="hub-zone-intro">Follow a guided quickstart.</p>
</div>
"""

    [<Fact>]
    let ``renders the card with its description`` () =
        markdown |> convertsToContainingHtml """<p class="hub-card-desc">Author a page and preview it.</p>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``card group with the solutions variant`` () =
    static let markdown = Setup.Markdown """
::::{card-group}
:title: Browse by area
:variant: solutions

:::{link-card}
title: Build the docs
:::
::::
"""

    [<Fact>]
    let ``locks the three column grid`` () =
        markdown |> convertsToContainingHtml """
<ul class="hub-card-grid hub-card-grid-solutions">
	<li class="hub-card">
		<div class="hub-card-head">
			<h3 class="hub-card-title">
				Build the docs
			</h3>
		</div>
	</li>
</ul>
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``card group nested in explore`` () =
    static let markdown = Setup.Markdown """
:::::{explore}
:id: explore
:title: Explore the docs
:intro: Organized by task.

::::{card-group}
:title: Quick links
:id: quick-links

:::{link-card}
title: Releases
description: This description is dropped in column mode.
links:
  - label: Exporters
    url: /index.md
aside:
  label: Also see
  links:
    - label: Versions
      url: /index.md
:::
::::

::::{card-group}
:title: Authoring
:id: authoring

:::{link-card}
title: Syntax
:::
::::
:::::
"""

    [<Fact>]
    let ``renders the explore heading`` () =
        markdown |> convertsToContainingHtml """
<div class="hub-zone" id="explore">
	<h2 class="hub-zone-title">Explore the docs</h2>
	<p class="hub-zone-intro">Organized by task.</p>
</div>
"""

    // One snapshot covers what nesting changes: accordion mode, the first accordion open and
    // the rest closed, link cards as columns, and the aside as a badge cluster under its own
    // label rather than a fixed string. It also pins the heading levels, so an Explore stack
    // keeps a complete outline: section h2, accordion h3, column h4.
    [<Fact>]
    let ``renders the accordion stack`` () =
        markdown |> convertsToContainingHtml """
<div class="hub-explore">
	<details class="hub-accordion" id="quick-links" open="">
		<summary class="hub-accordion-summary">
			<h3 class="hub-accordion-title">Quick links</h3>
			<svg class="hub-accordion-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
				<path d="M3 8h10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"></path>
				<path class="hub-accordion-icon-v" d="M8 3v10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"></path>
			</svg>
		</summary>
		<div class="hub-accordion-body">
			<ul class="hub-explore-cols">
				<li class="hub-col">
					<h4 class="hub-col-title">
						Releases
					</h4>
					<ul class="hub-col-links">
						<li>
							<a href="/">Exporters</a>
						</li>
					</ul>
					<div class="hub-explore-more">
						<div class="hub-explore-more-label">Also see</div>
						<ul class="hub-explore-more-badges">
							<li class="doc-button-item doc-button-secondary doc-button-small">
								<a href="/">Versions</a>
							</li>
						</ul>
					</div>
				</li>
			</ul>
		</div>
	</details>
	<details class="hub-accordion" id="authoring">
		<summary class="hub-accordion-summary">
			<h3 class="hub-accordion-title">Authoring</h3>
			<svg class="hub-accordion-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
				<path d="M3 8h10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"></path>
				<path class="hub-accordion-icon-v" d="M8 3v10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"></path>
			</svg>
		</summary>
		<div class="hub-accordion-body">
			<ul class="hub-explore-cols">
				<li class="hub-col">
					<h4 class="hub-col-title">
						Syntax
					</h4>
				</li>
			</ul>
		</div>
	</details>
</div>
"""

    // A column is a pure link index, so the description is deliberately dropped.
    [<Fact>]
    let ``drops the description in column mode`` () =
        markdown |> doesNotConvertToContainingHtml """This description is dropped in column mode."""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``link card without a title`` () =
    static let markdown = Setup.Markdown """
:::{link-card}
description: No title here.
:::
"""

    [<Fact>]
    let ``errors`` () =
        markdown |> hasError "requires a `title`"

type ``link card with a relative link`` () =
    static let markdown = Setup.Markdown """
:::{link-card}
title: Broken
links:
  - label: Relative
    url: nope.md
:::
"""

    [<Fact>]
    let ``rejects a relative path`` () =
        markdown |> hasError "must be an absolute path starting with `/`"

type ``link card with a missing target`` () =
    static let markdown = Setup.Markdown """
:::{link-card}
title: Broken
links:
  - label: Missing
    url: /does-not-exist.md
:::
"""

    [<Fact>]
    let ``errors on a link that does not resolve`` () =
        markdown |> hasError "does not exist"

type ``link card with a cross-link`` () =
    static let markdown = Setup.Markdown """
::::{card-group}
:title: Documentation this toolchain builds

:::{link-card}
icon: elasticsearch
variant: es
title: Elasticsearch
description: Search and analytics documentation.
links:
  - label: Elastic documentation
    url: docs-content://get-started/index.md
:::
::::
"""

    // Covers three things at once: the icon and variant accent, and that a cross-link resolves
    // to a full URL without being treated as external. Inline links make the same distinction.
    [<Fact>]
    let ``renders the icon and variant, and does not open the cross-link in a new tab`` () =
        markdown |> convertsToContainingHtml """
<ul class="hub-card-grid">
	<li class="hub-card hub-card-sol hub-card-sol-es">
		<div class="hub-card-head">
			<span class="hub-card-icon">
				<svg viewBox="8 4.9995 47.7276 54.001" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
					<path fill-rule="evenodd" clip-rule="evenodd" d="M55.7246 14.7075L55.7276 14.7015C50.7746 8.77351 43.3286 4.99951 34.9996 4.99951C24.4006 4.99951 15.2326 11.1115 10.8136 19.9995H46.0056C48.5306 19.9995 50.9886 19.1295 52.9206 17.5035C53.9246 16.6585 54.8636 15.7385 55.7246 14.7075Z" fill="#FEC514"></path>
					<path fill-rule="evenodd" clip-rule="evenodd" d="M8 32C8 34.422 8.324 36.767 8.922 39H42C45.866 39 49 35.866 49 32C49 28.134 45.866 25 42 25H8.922C8.324 27.233 8 29.578 8 32Z" fill="rgba(255,255,255,0.85)"></path>
					<path fill-rule="evenodd" clip-rule="evenodd" d="M55.7246 49.2925L55.7276 49.2985C50.7746 55.2265 43.3286 59.0005 34.9996 59.0005C24.4006 59.0005 15.2326 52.8885 10.8136 44.0005H46.0056C48.5306 44.0005 50.9886 44.8705 52.9206 46.4965C53.9246 47.3415 54.8636 48.2615 55.7246 49.2925Z" fill="#00BFB3"></path>
				</svg>
			</span>
			<h3 class="hub-card-title">
				Elasticsearch
			</h3>
		</div>
		<p class="hub-card-desc">Search and analytics documentation.</p>
		<ul class="hub-card-links">
			<li>
				<a href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/get-started">Elastic documentation</a>
			</li>
		</ul>
	</li>
</ul>
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors
