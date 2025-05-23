// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation;
using Elastic.Documentation.Links;
using Elastic.Markdown.Links.CrossLinks;
using Xunit.Internal;

namespace Elastic.Markdown.Tests;

public class TestCrossLinkResolver : ICrossLinkResolver
{
	public IUriEnvironmentResolver UriResolver { get; } = new IsolatedBuildEnvironmentUriResolver();
	private FetchedCrossLinks _crossLinks = FetchedCrossLinks.Empty;
	private Dictionary<string, RepositoryLinks> LinkReferences { get; } = [];
	private HashSet<string> DeclaredRepositories { get; } = [];

	public Task<FetchedCrossLinks> FetchLinks(Cancel ctx)
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
		LinkReferences.Add("docs-content", reference);
		LinkReferences.Add("kibana", reference);
		DeclaredRepositories.AddRange(["docs-content", "kibana", "elasticsearch"]);

		var indexEntries = LinkReferences.ToDictionary(e => e.Key, e => new LinkRegistryEntry
		{
			Repository = e.Key,
			Path = $"elastic/asciidocalypse/{e.Key}/links.json",
			Branch = "main",
			ETag = Guid.NewGuid().ToString(),
			GitReference = Guid.NewGuid().ToString()
		});
		_crossLinks = new FetchedCrossLinks
		{
			DeclaredRepositories = DeclaredRepositories,
			LinkReferences = LinkReferences.ToFrozenDictionary(),
			FromConfiguration = true,
			LinkIndexEntries = indexEntries.ToFrozenDictionary()
		};
		return Task.FromResult(_crossLinks);
	}

	public bool TryResolve(Action<string> errorEmitter, Action<string> warningEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri) =>
		CrossLinkResolver.TryResolve(errorEmitter, warningEmitter, _crossLinks, UriResolver, crossLinkUri, out resolvedUri);
}
