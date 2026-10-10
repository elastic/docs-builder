// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.StyleGuide;

/// <summary>The Markdown of the style guide's sample documentation set. It goes through the real Markdown pipeline, so the previews show what the renderer writes.</summary>
internal static class PreviewSamples
{
	private const string Lorem =
		"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

	public static string DocSet(bool branded) =>
		$"""
		project: 'Lorem Ipsum Docs'
		{(branded ? "branding:\n  copyright: \"© 2026 Lorem Ipsum Inc. All rights reserved.\"\n" : "")}toc:
		  - file: index.md
		  - file: kitchen-sink.md
		  - file: product.md
		  - file: landing.md
		  - file: archive.md
		  - file: not-found.md
		  - folder: guides
		    children:
		      - file: index.md
		      - file: install.md
		      - file: configure.md
		      - folder: deploy
		        children:
		          - file: index.md
		          - folder: cloud
		            children:
		              - file: index.md
		              - folder: aws
		                children:
		                  - file: index.md
		                  - folder: networking
		                    children:
		                      - file: index.md
		                      - file: private-link.md
		""";

	public const string Landing = """
		---
		layout: landing-page
		---

		# Landing
		""";

	public const string Archive = """
		---
		layout: archive
		---

		# Archive
		""";

	public const string NotFound = """
		---
		layout: not-found
		---

		# Not found
		""";

	public static string IndexPage(string? title, string? description) =>
		(description is null ? string.Empty : $"---\ndescription: {description}\n---\n\n")
			+ (title is null ? Index : Index.Replace("# Lorem ipsum dolor sit amet", $"# {title}", StringComparison.Ordinal));

	private const string Index =
		$$"""
		# Lorem ipsum dolor sit amet

		{{Lorem}}

		## Consectetur adipiscing

		{{Lorem}} [Duis aute irure](/kitchen-sink.md) dolor in reprehenderit in voluptate velit esse cillum dolore.

		* Sed ut perspiciatis unde omnis iste natus
		* Nemo enim ipsam voluptatem quia voluptas
		* Neque porro quisquam est qui dolorem

		## Sed do eiusmod

		```json
		GET /lorem/_search
		{ "query": { "match": { "ipsum": "dolor" } } }
		```

		### Excepteur sint

		{{Lorem}}

		| Setting | Default |
		|---------|---------|
		| lorem_ipsum | 1 |
		| dolor_sit | amet |
		""";

	public const string GuideIndex =
		$$"""
		# Guides

		{{Lorem}}
		""";

	public const string GuideInstall =
		$$"""
		# Install

		{{Lorem}}
		""";

	public const string DeployIndex =
		$$"""
		# Deploy

		{{Lorem}}
		""";

	public const string DeployCloudIndex =
		$$"""
		# Cloud

		{{Lorem}}
		""";

	public const string DeployAwsIndex =
		$$"""
		# Amazon Web Services

		{{Lorem}}
		""";

	public const string DeployNetworkingIndex =
		$$"""
		# Networking

		{{Lorem}}
		""";

	/// <summary>A page six levels down, so the breadcrumb has a long trail to show.</summary>
	public const string DeployPrivateLink =
		$$"""
		# Private link

		{{Lorem}}

		## Create the endpoint

		{{Lorem}}

		## Connect a deployment

		{{Lorem}}
		""";

	public const string GuideConfigure =
		$$"""
		# Configure

		{{Lorem}}
		""";

	/// <summary>One page with the directives and roles of the syntax guide.</summary>
	public const string KitchenSink =
		$$"""
		# Every directive and role

		{{Lorem}}

		## Admonitions

		:::{note}
		Lorem ipsum dolor sit amet, consectetur adipiscing elit.
		:::

		:::{tip}
		Sed do eiusmod tempor incididunt ut labore.
		:::

		:::{important}
		Ut enim ad minim veniam, quis nostrud exercitation.
		:::

		:::{warning}
		Duis aute irure dolor in reprehenderit in voluptate.
		:::

		:::{admonition} Custom title
		Excepteur sint occaecat cupidatat non proident.
		:::

		## Applies to

		```{applies_to}
		stack: ga 9.0
		deployment:
		  ess: ga
		```

		Lorem ipsum {applies_to}`stack: preview 9.1` dolor sit amet.

		## Dropdown

		:::{dropdown} Lorem ipsum dolor sit amet
		Consectetur adipiscing elit, sed do eiusmod tempor incididunt.
		:::

		## Tabs

		::::{tab-set}
		:::{tab-item} Lorem
		Content of the first tab.
		:::
		:::{tab-item} Ipsum
		Content of the second tab.
		:::
		::::

		## Code blocks

		```json
		{ "query": { "match": { "title": "lorem" } } } <1>
		```
		1. Match the title field.

		## Stepper

		::::{stepper}
		:::{step} Install
		Lorem ipsum dolor sit amet.
		:::
		:::{step} Configure
		Consectetur adipiscing elit.
		:::
		:::{step} Run
		Sed do eiusmod tempor.
		:::
		::::

		## Tables and definition lists

		| Setting | Default |
		|---------|---------|
		| lorem_ipsum | 1 |
		| dolor_sit | amet |

		Shard
		:   A slice of an index.

		Replica
		:   A copy of a primary shard.

		## Inline roles

		Press {kbd}`cmd+k` to search, then {kbd}`enter`. An icon in text: {icon}`search`. Inline math: {math}`E = mc^2`.

		:::{math}
		S(doc) = exp(\lambda \cdot max(0, |fieldvalue_{doc} - origin| - offset))
		:::

		## Diagrams

		```mermaid
		flowchart LR
		  A[Lorem] --> B[Ipsum] --> C[Dolor]
		```

		## Buttons

		:::{button}
		[Primary action](/index.md)
		:::

		## Footnotes and task lists

		Lorem ipsum[^1] dolor sit amet.

		[^1]: Consectetur adipiscing elit.

		- [x] Sed do eiusmod
		- [ ] Tempor incididunt
		""";

	/// <summary>A product hub: the hub directives, composed into one page.</summary>
	public const string Product =
		$$"""
		---
		layout: hub
		description: Lorem ipsum product hub.
		---

		:::{hero}
		:title: Lorem ipsum
		:description: Dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
		:primary-action: [Get started](/guides/install.md)
		:secondary-action: [Syntax guide](/kitchen-sink.md)
		:::

		:::{get-started}
		title: Get started in 3 steps
		intro: Consectetur adipiscing elit sed do eiusmod tempor.
		steps:
		  - title: Lorem ipsum dolor
		    options:
		      - label: Dolor sit
		        description: Amet consectetur adipiscing elit.
		        url: /guides/install.md
		        url-label: Start
		      - label: Tempor
		        description: Incididunt ut labore.
		        code: lorem ipsum --dolor
		        language: sh
		  - title: Sed do eiusmod
		    description: Duis aute irure dolor in reprehenderit.
		    link: /guides/configure.md
		    link-label: Read more
		  - title: Ut enim ad minim
		    description: Quis nostrud exercitation ullamco.
		:::

		::::{card-group}
		:title: Explore
		:id: explore
		:intro: Sunt in culpa qui officia deserunt mollit.

		:::{link-card}
		title: Lorem ipsum
		link: /guides/index.md
		description: Dolor sit amet consectetur adipiscing elit.
		links:
		  - label: Install
		    url: /guides/install.md
		  - label: Configure
		    url: /guides/configure.md
		:::

		:::{link-card}
		title: Dolor sit
		link: /kitchen-sink.md
		description: Sed do eiusmod tempor incididunt ut labore.
		:::
		::::
		""";
}
