// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Xunit.Internal;

namespace Elastic.Markdown.Tests;

/// <summary>Cross-link resolver that uses <see cref="CodexAwareUriResolver"/> with configurable relative/absolute mode.</summary>
public class TestCodexCrossLinkResolver : ICrossLinkResolver
{
	private readonly FetchedCrossLinks _crossLinks;

	public IUriEnvironmentResolver UriResolver { get; }

	public TestCodexCrossLinkResolver(bool useRelativePaths)
	{
		// language=json
		var json = """
		           {
		              "content_source": "current",
		           	  "origin": {
		           		"branch": "main",
		           		"remote": " https://github.com/elastic/docs-content",
		           		"ref": "76aac68d066e2af935c38bca8ce04d3ee67a8dd9"
		           	  },
		           	  "url_path_prefix": "/elastic/docs-content/tree/main",
		           	  "cross_links": [],
		           	  "links": {
		           		"index.md": {},
		           		"get-started/index.md": {
		           		  "anchors": [
		           			"elasticsearch-intro-elastic-stack",
		           			"elasticsearch-intro-use-cases"
		           		  ]
		           		},
		           		"solutions/observability/apps/apm-server-binary.md": {
		           		  "anchors": [ "apm-deb" ]
		           		}
		           	  }
		           	}
		           """;
		var reference = CrossLinkFetcher.Deserialize(json);
		var linkReferences = new Dictionary<string, RepositoryLinks>();
		var declaredRepositories = new HashSet<string>();
		linkReferences.Add("docs-content", reference);
		linkReferences.Add("kibana", reference);
		declaredRepositories.AddRange(["docs-content", "kibana"]);

		var codexRepositories = new HashSet<string> { "docs-content", "kibana" }.ToFrozenSet();

		var indexEntries = linkReferences.ToDictionary(e => e.Key, e => new LinkRegistryEntry
		{
			Repository = e.Key,
			Path = $"elastic/docs-builder-tests/{e.Key}/links.json",
			Branch = "main",
			ETag = Guid.NewGuid().ToString(),
			GitReference = Guid.NewGuid().ToString()
		});
		_crossLinks = new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkReferences = linkReferences.ToFrozenDictionary(),
			LinkIndexEntries = indexEntries.ToFrozenDictionary(),
			CodexRepositories = codexRepositories
		};

		UriResolver = new CodexAwareUriResolver(codexRepositories, useRelativePaths);
	}

	public bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri) =>
		CrossLinkResolver.TryResolve(errorEmitter, _crossLinks, UriResolver, crossLinkUri, out resolvedUri);
}
