// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``agent skill elements``

open Xunit
open authoring

type ``agent skill with url`` () =
    static let markdown = Setup.Markdown """
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql
:::
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<div class="agent-skill">
	<div class="agent-skill-header">
		<svg class="agent-skill-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
			<path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09ZM18.259 8.715 18 9.75l-.259-1.035a3.375 3.375 0 0 0-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 0 0 2.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 0 0 2.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 0 0-2.455 2.456ZM16.894 20.567 16.5 21.75l-.394-1.183a2.25 2.25 0 0 0-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 0 0 1.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 0 0 1.423 1.423l1.183.394-1.183.394a2.25 2.25 0 0 0-1.423 1.423Z"></path>
		</svg>
		<span class="agent-skill-title">Agent skill available</span>
	</div>
	<div class="agent-skill-content">
		<div class="agent-skill-text">
			<p>A skill is available to help AI agents with this topic.</p>
			<p><a href="/explore-analyze/ai-features/agent-skills">Learn more about agent skills for Elastic</a></p>
		</div>
		<a href="https://github.com/elastic/agent-skills@elasticsearch-esql" target="_blank" rel="noopener noreferrer" class="agent-skill-button">
			Get the skill
			<svg class="agent-skill-button-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
				<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25"></path>
			</svg>
		</a>
	</div>
</div>
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``agent skill with body content`` () =
    static let markdown = Setup.Markdown """
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql

This skill helps agents write and optimize ES|QL queries.
:::
"""

    [<Fact>]
    let ``renders custom body`` () =
        markdown |> convertsToHtml """
<div class="agent-skill">
	<div class="agent-skill-header">
		<svg class="agent-skill-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
			<path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09ZM18.259 8.715 18 9.75l-.259-1.035a3.375 3.375 0 0 0-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 0 0 2.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 0 0 2.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 0 0-2.455 2.456ZM16.894 20.567 16.5 21.75l-.394-1.183a2.25 2.25 0 0 0-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 0 0 1.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 0 0 1.423 1.423l1.183.394-1.183.394a2.25 2.25 0 0 0-1.423 1.423Z"></path>
		</svg>
		<span class="agent-skill-title">Agent skill available</span>
	</div>
	<div class="agent-skill-content">
		<div class="agent-skill-text">
			<p>A skill is available to help AI agents with this topic.</p>
			<p>This skill helps agents write and optimize ES|QL queries.</p>
			<p><a href="/explore-analyze/ai-features/agent-skills">Learn more about agent skills for Elastic</a></p>
		</div>
		<a href="https://github.com/elastic/agent-skills@elasticsearch-esql" target="_blank" rel="noopener noreferrer" class="agent-skill-button">
			Get the skill
			<svg class="agent-skill-button-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
				<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25"></path>
			</svg>
		</a>
	</div>
</div>
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``agent skill missing url`` () =
    static let markdown = Setup.Markdown """
:::{agent-skill}
:::
"""

    [<Fact>]
    let ``has error`` () = markdown |> hasError "requires a :url: property"

type ``agent skill relative url`` () =
    static let markdown = Setup.Markdown """
:::{agent-skill}
:url: /relative/path
:::
"""

    [<Fact>]
    let ``has error`` () = markdown |> hasError "must be an absolute URL"
