// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO.State;
using Xunit.Internal;

namespace Elastic.Markdown.Tests;

public class TestCrossLinkResolver : ICrossLinkResolver
{
	private Dictionary<string, LinkReference> LinkReferences { get; } = [];
	private HashSet<string> DeclaredRepositories { get; } = [];

	public Task FetchLinks()
	{
		// language=json
		var json = """
		           {
		           	  "origin": {
		           		"branch": "main",
		           		"remote": " https://github.com/elastic/docs-conten",
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
		var reference = CrossLinkResolver.Deserialize(json);
		LinkReferences.Add("docs-content", reference);
		LinkReferences.Add("kibana", reference);
		DeclaredRepositories.AddRange(["docs-content", "kibana", "elasticsearch"]);
		return Task.CompletedTask;
	}

	public bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri) =>
		CrossLinkResolver.TryResolve(errorEmitter, DeclaredRepositories, LinkReferences, crossLinkUri, out resolvedUri);
}
