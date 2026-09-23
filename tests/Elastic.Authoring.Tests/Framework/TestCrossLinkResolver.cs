// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>TestCrossLinkResolver.fs</c> from <c>tests/authoring/Framework/</c>.
/// Accepts the resolved <see cref="ConfigurationFile"/> so it can serialise the real redirect
/// rules into the link index, unlike the simpler <c>Elastic.Markdown.Tests.TestCrossLinkResolver</c>
/// which takes no arguments.
/// </summary>
public sealed class TestCrossLinkResolver : ICrossLinkResolver
{
	private readonly Dictionary<string, RepositoryLinks> _references = new();
	private readonly HashSet<string> _declared = [];
	private readonly IsolatedBuildEnvironmentUriResolver _uriResolver = new();
	private FetchedCrossLinks _crossLinks = FetchedCrossLinks.Empty;

	public TestCrossLinkResolver(ConfigurationFile config)
	{
		var redirects = RepositoryLinks.SerializeRedirects(config.Redirects);

		// language=json
		var json =
			$$"""
		             {
		               "origin": {
		                 "branch": "main",
		                 "remote": " https://github.com/elastic/docs-content",
		                 "ref": "76aac68d066e2af935c38bca8ce04d3ee67a8dd9"
		               },
		               "url_path_prefix": "/elastic/docs-content/tree/main",
		               "cross_links": [],
		               "redirects" : {{redirects}},
		               "links": {
		                 "index.md": {},
		                 "get-started/index.md": {
		                   "anchors": [
		                     "elasticsearch-intro-elastic-stack",
		                     "elasticsearch-intro-use-cases"
		                   ]
		                 },
		                 "solutions/observability/apps/apm-server-binary.md": {
		                   "anchors": [ "apm-deb", "elasticsearch-requestHeadersWhitelist" ]
		                 },
		                 "testing/redirects/first-page.md": {
		                   "anchors": [ "current-anchor", "another-anchor" ]
		                 },
		                 "testing/redirects/second-page.md": {
		                   "anchors": [ "active-anchor", "zz" ]
		                 },
		                 "testing/redirects/third-page.md": { "anchors": [ "bb" ] },
		                 "testing/redirects/5th-page.md": { "anchors": [ "yy" ] }
		               }
		             }
		             """;

		var reference = CrossLinkFetcher.Deserialize(json);
		_references.Add("docs-content", reference);
		_references.Add("kibana", reference);
		_declared.Add("docs-content");
		_declared.Add("kibana");

		var indexEntries = _references.ToDictionary(
			e => e.Key,
			e => new LinkRegistryEntry
			{
				Repository = e.Key,
				Path = $"elastic/docs-builder-tests/{e.Key}/links.json",
				Branch = "main",
				ETag = Guid.NewGuid().ToString(),
				GitReference = Guid.NewGuid().ToString()
			}
		);

		_crossLinks = new FetchedCrossLinks
		{
			DeclaredRepositories = _declared,
			LinkReferences = _references.ToFrozenDictionary(),
			LinkIndexEntries = indexEntries.ToFrozenDictionary()
		};
	}

	public IUriEnvironmentResolver UriResolver => _uriResolver;

	public bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri) =>
		CrossLinkResolver.TryResolve(errorEmitter, _crossLinks, _uriResolver, crossLinkUri, out resolvedUri);

	public bool IsDeclaredCrossLinkScheme(string scheme) => _declared.Contains(scheme);
}
