// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Testing;

namespace Elastic.Documentation.Build.Tests;

/// <summary>The content of a few fake repositories: files by path, the assembler config and the navigation file.</summary>
public sealed partial class FakeSiteContent
{
	private readonly Dictionary<string, Dictionary<string, string>> _repositories = [];

	public required string AssemblerYaml { get; init; }
	public required string NavigationYaml { get; init; }

	public IReadOnlyCollection<string> Repositories => _repositories.Keys.Order(StringComparer.Ordinal).ToArray();

	public IReadOnlyDictionary<string, string> ConfigFiles =>
		new Dictionary<string, string> { ["assembler.yml"] = AssemblerYaml, ["navigation.yml"] = NavigationYaml };

	/// <summary>Adds a file below the <c>docs</c> folder of <paramref name="repository"/>.</summary>
	public FakeSiteContent File(string repository, string path, string content)
	{
		if (!_repositories.TryGetValue(repository, out var files))
			_repositories[repository] = files = [];
		files[path] = content.Replace("\r\n", "\n");
		return this;
	}

	public void WriteTo(MockFileSystem fileSystem)
	{
		foreach (var (repository, files) in _repositories)
		{
			fileSystem.AddDirectory($"/checkouts/{repository}/.git");
			foreach (var (path, content) in files)
				fileSystem.AddFile($"/checkouts/{repository}/docs/{path}", new MockFileData(content));
		}
	}

	/// <summary>
	/// The cross links the link index would serve: every Markdown page of every repository with the anchors of its headings,
	/// as a previous build of that repository would have published them.
	/// </summary>
	public FetchedCrossLinks CrossLinks()
	{
		var references = new Dictionary<string, LinkManifest>();
		foreach (var (repository, files) in _repositories)
		{
			var links = string.Join(
				",",
				files
					.Where(f => f.Key.EndsWith(".md", StringComparison.Ordinal))
					.OrderBy(f => f.Key, StringComparer.Ordinal)
					.Select(f => $"\"{f.Key}\": {{ \"anchors\": [{string.Join(",", Anchors(f.Value).Select(a => $"\"{a}\""))}] }}")
			);
			// language=json
			var json =
				$$"""
				{
				  "content_source": "current",
				  "origin": { "branch": "main", "remote": "https://github.com/elastic/{{repository}}", "ref": "0000000000000000000000000000000000000000" },
				  "url_path_prefix": "/elastic/{{repository}}/tree/main",
				  "cross_links": [],
				  "links": { {{links}} }
				}
				""";
			references[repository] = LinkManifestJson.Read(json);
		}

		return new FetchedCrossLinks
		{
			DeclaredRepositories = [.. references.Keys],
			LinkReferences = references.ToFrozenDictionary(),
			LinkIndexEntries = references.ToFrozenDictionary(
				r => r.Key,
				r => new LinkRegistryEntry
				{
					Repository = r.Key,
					Path = $"elastic/{r.Key}/main/links.json",
					Branch = "main",
					ETag = "etag",
					GitReference = "0000000000000000000000000000000000000000"
				}
			)
		};
	}

	private static IEnumerable<string> Anchors(string markdown) =>
		Heading()
			.Matches(markdown)
			.Select(
				m => Regex.Replace(
					m.Groups["title"].Value.Trim().ToLowerInvariant(),
					"[^a-z0-9]+",
					"-",
					RegexOptions.None,
					RegexTimeouts.MatchTimeout
				).Trim('-')
			);

	[GeneratedRegex("^#{1,6} (?<title>.+)$", RegexOptions.Multiline, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex Heading();
}

/// <summary>
/// A narrative repository and three reference repositories, with cross links in both directions, redirects, <c>applies_to</c>,
/// hidden pages and a nested table of contents.
/// </summary>
public static class FakeRepositories
{
	private static readonly string Narrative = NarrativeRepository.RepositoryName;

	public static FakeSiteContent Default { get; } = Create();

	private static FakeSiteContent Create() =>
		new FakeSiteContent
		{
			AssemblerYaml =
				// language=yaml
				"""
				environments:
				  prod:
				    uri: https://www.elastic.co
				    path_prefix: docs
				    content_source: current
				    allow_indexing: true
				narrative:
				  checkout_strategy: full
				references:
				  elasticsearch:
				  kibana:
				  apm-agent-java:
				""",
			NavigationYaml =
				// language=yaml
				"""
				phantoms:
				  - toc: docs-content://
				toc:
				  - toc: get-started
				  - toc: elasticsearch://
				    path_prefix: reference/elasticsearch
				  - toc: kibana://
				    path_prefix: reference/kibana
				  - toc: apm-agent-java://
				    path_prefix: reference/apm/agents/java
				"""
		}
			.File(
				Narrative,
				"docset.yml",
				"""
				project: docs-content
				toc:
				  - file: index.md
				  - hidden: internal.md
				  - toc: get-started
				"""
			)
			.File(
				Narrative,
				"index.md",
				"""
				# Welcome

				Start with [installation](get-started/install.md), then read the [Search APIs](elasticsearch://search-apis.md) and the [Kibana dashboards](kibana://dashboards.md#dashboard-basics).
				"""
			)
			.File(Narrative, "internal.md", "# Internal notes\n\nNot linked from the navigation.\n")
			.File(
				Narrative,
				"redirects.yml",
				"""
				redirects:
				  'old-start.md': 'get-started/index.md'
				  'old-search.md': 'elasticsearch://search-apis.md'
				"""
			)
			.File(
				Narrative,
				"get-started/toc.yml",
				"""
				toc:
				  - file: index.md
				  - file: install.md
				  - file: concepts.md
				"""
			)
			.File(Narrative, "get-started/index.md", "# Get started\n\nRead [Install](install.md) and [Concepts](concepts.md).\n")
			.File(
				Narrative,
				"get-started/install.md",
				"""
				---
				navigation_title: Install
				applies_to:
				  stack: ga 9.0
				  serverless: ga
				---
				# Install the stack

				Download the package, then continue with [core concepts](concepts.md#core-concepts) or the [search APIs](elasticsearch://search-apis.md#search-basics).
				"""
			)
			.File(
				Narrative,
				"get-started/concepts.md",
				"""
				---
				applies_to:
				  stack: preview 9.1
				---
				# Concepts

				## Core concepts

				:::{note}
				Every page covers all supported versions.
				:::

				```{applies_to}
				stack: ga 9.0
				```

				| Term | Meaning |
				|---|---|
				| Index | A collection of documents |
				"""
			)
			.File(
				"elasticsearch",
				"docset.yml",
				"""
				project: elasticsearch
				toc:
				  - file: index.md
				  - file: search-apis.md
				  - folder: query-dsl
				    children:
				      - file: index.md
				      - file: term-queries.md
				"""
			)
			.File(
				"elasticsearch",
				"index.md",
				"# Elasticsearch reference\n\nSee [Search APIs](search-apis.md) and [Query DSL](query-dsl/index.md).\n"
			)
			.File(
				"elasticsearch",
				"search-apis.md",
				"""
				---
				applies_to:
				  stack: ga 9.0
				---
				# Search APIs

				## Search basics

				Install first: [Install](docs-content://get-started/install.md).
				"""
			)
			.File("elasticsearch", "query-dsl/index.md", "# Query DSL\n\nSee [term queries](term-queries.md).\n")
			.File("elasticsearch", "query-dsl/term-queries.md", "# Term queries\n\n## Term query\n\nMatches an exact term.\n")
			.File(
				"elasticsearch",
				"redirects.yml",
				"""
				redirects:
				  'legacy-search.md': 'search-apis.md'
				"""
			)
			.File(
				"kibana",
				"docset.yml",
				"""
				project: kibana
				toc:
				  - file: index.md
				  - file: dashboards.md
				  - hidden: experimental.md
				"""
			)
			.File("kibana", "index.md", "# Kibana\n\nBuild [dashboards](dashboards.md).\n")
			.File(
				"kibana",
				"dashboards.md",
				"# Dashboards\n\n## Dashboard basics\n\nQuery with the [Search APIs](elasticsearch://search-apis.md).\n"
			)
			.File("kibana", "experimental.md", "# Experimental\n\nHidden from the navigation.\n")
			.File(
				"apm-agent-java",
				"docset.yml",
				"""
				project: apm-agent-java
				toc:
				  - file: index.md
				  - file: setup.md
				"""
			)
			.File("apm-agent-java", "index.md", "# Java agent\n\nSee [setup](setup.md).\n")
			.File("apm-agent-java", "setup.md", "# Setup\n\nRead the [concepts](docs-content://get-started/concepts.md).\n");
}
