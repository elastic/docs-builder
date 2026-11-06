// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Markdown.Tests.CrossLinks;

public class ApiPageStatusTests
{
	private static readonly FloatingLinkSection Elasticsearch = new()
	{
		// The roots are stated, not derived: version 8 is not under a "v8" segment.
		Versions =
		[
			new() { Name = "main", Root = "api/doc/elasticsearch", Current = true },
			new() { Name = "9", Root = "api/doc/elasticsearch/v9", Hidden = true },
			new() { Name = "8", Root = "api/doc/elasticsearch/legacy-8", Hidden = true }
		],
		Pages = new Dictionary<string, ulong>
		{
			[""] = 0b111,
			["operation/operation-search"] = 0b111,
			["operation/operation-new"] = 0b011,
			["types/legacy"] = 0b100
		},
		Aliases = new Dictionary<string, Dictionary<string, string[]>>
		{
			["9"] = new() { ["operation/operation-search"] = ["operation/operation-search-1"] }
		}
	};

	private static readonly FloatingLinkSection KibanaServerless = new()
	{
		Versions = [new() { Name = "main", Root = "api/doc/kibana-serverless", Current = true }],
		Pages = new Dictionary<string, ulong> { [""] = 1, ["operation/operation-live"] = 1 }
	};

	private static CrossLinkResolver Resolver()
	{
		var links = new RepositoryLinks
		{
			Origin = GitCheckoutInformation.Unavailable,
			UrlPathPrefix = "/elastic/elasticsearch/tree/main",
			Links = new Dictionary<string, LinkMetadata> { ["index.md"] = new() },
			CrossLinks = [],
			Floating = new Dictionary<string, FloatingLinkSection>
			{
				["api/elasticsearch"] = Elasticsearch,
				["api/kibana-serverless"] = KibanaServerless
			}
		};
		return new CrossLinkResolver(new FetchedCrossLinks
		{
			DeclaredRepositories = ["elasticsearch"],
			LinkReferences = new Dictionary<string, RepositoryLinks> { ["elasticsearch"] = links }.ToFrozenDictionary(),
			LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary()
		});
	}

	[Test]
	[Arguments("/docs/api/doc/elasticsearch", ApiPageStatus.Exists)]
	[Arguments("/docs/api/doc/elasticsearch/operation/operation-search", ApiPageStatus.Exists)]
	[Arguments("/docs/api/doc/elasticsearch/v9/operation/operation-new", ApiPageStatus.Exists)]
	[Arguments("/docs/api/doc/elasticsearch/legacy-8/types/legacy", ApiPageStatus.Exists)]
	[Arguments("/docs/api/doc/elasticsearch/v9/operation/operation-search-1", ApiPageStatus.Exists)]
	[Arguments("/docs/api/doc/elasticsearch/operation/operation-gone", ApiPageStatus.Missing)]
	[Arguments("/docs/api/doc/elasticsearch/legacy-8/operation/operation-new", ApiPageStatus.Missing)]
	[Arguments("/docs/api/doc/elasticsearch/v9/types/legacy", ApiPageStatus.Missing)]
	[Arguments("/docs/api/doc/kibana/operation/operation-x", ApiPageStatus.Unknown)]
	[Arguments("/docs/solutions/search", ApiPageStatus.Unknown)]
	public void CheckApiPage_PathInTheSections_ReportsWhetherThePageExists(string sitePath, ApiPageStatus expected) =>
		Resolver().CheckApiPage(sitePath).Should().Be(expected);

	[Test]
	public void CheckApiPage_ResolverWithoutSections_IsUnknown() =>
		((ICrossLinkResolver)NoopCrossLinkResolver.Instance).CheckApiPage("/docs/api/doc/elasticsearch").Should().Be(ApiPageStatus.Unknown);

	[Test]
	[Arguments("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search#responses", false)]
	[Arguments("https://www.elastic.co/docs/api/doc/elasticsearch//operation/operation-search/", false)]
	[Arguments("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-gone", true)]
	[Arguments("/docs/api/doc/elasticsearch/operation/operation-gone", true)]
	[Arguments("https://www.elastic.co/docs/api/doc/kibana/operation/operation-x", false)]
	[Arguments("https://www.elastic.co/docs/solutions/search", false)]
	// The edge redirects the old serverless root to kibana-serverless, so the page is checked there.
	[Arguments("https://www.elastic.co/docs/api/doc/serverless/operation/operation-live", false)]
	[Arguments("https://www.elastic.co/docs/api/doc/serverless/operation/operation-removed", true)]
	[Arguments("https://www.elastic.co/docs/api/doc/observability-serverless/operation/operation-x", true)]
	// Spec downloads are files, not pages of a section.
	[Arguments("https://www.elastic.co/docs/api/doc/elasticsearch.yaml", false)]
	public void GetWarning_Link_WarnsOnlyForAKnownMissingApiPage(string url, bool warns) =>
		(ApiLinkCheck.GetWarning(Resolver(), url, "/docs") is not null).Should().Be(warns);
}
