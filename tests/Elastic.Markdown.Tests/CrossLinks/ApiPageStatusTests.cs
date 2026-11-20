// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests.CrossLinks;

public class ApiPageStatusTests
{
	private static readonly FloatingLinkSection Elasticsearch = new()
	{
		// The roots are stated, not derived: version 8 is not under a "v8" segment. These roots are from an index published
		// before the API Explorer dropped /doc, and read as api/elasticsearch.
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
		Versions = [new() { Name = "main", Root = "api/kibana-serverless", Current = true }],
		Pages = new Dictionary<string, ulong> { [""] = 1, ["operation/operation-live"] = 1 }
	};

	/// <summary>The newest major owns the versionless root and keeps its numbered root as a mirror; main lives at next.</summary>
	private static readonly FloatingLinkSection Logstash = new()
	{
		Versions =
		[
			new() { Name = "main", Root = "api/logstash/next", Hidden = true },
			new() { Name = "9", Root = "api/logstash", MirrorRoot = "api/logstash/v9", Current = true },
			new() { Name = "8", Root = "api/logstash/v8", Hidden = true }
		],
		Pages = new Dictionary<string, ulong> { [""] = 0b111, ["operation/operation-search"] = 0b111, ["operation/operation-new"] = 0b001 }
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
				["api/kibana-serverless"] = KibanaServerless,
				["api/logstash"] = Logstash
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
	[Arguments("/docs/api/elasticsearch", ApiPageStatus.Exists)]
	[Arguments("/docs/api/elasticsearch/operation/operation-search", ApiPageStatus.Exists)]
	[Arguments("/docs/api/elasticsearch/v9/operation/operation-new", ApiPageStatus.Exists)]
	[Arguments("/docs/api/elasticsearch/legacy-8/types/legacy", ApiPageStatus.Exists)]
	[Arguments("/docs/api/elasticsearch/v9/operation/operation-search-1", ApiPageStatus.Exists)]
	[Arguments("/docs/api/elasticsearch/operation/operation-gone", ApiPageStatus.Missing)]
	[Arguments("/docs/api/elasticsearch/legacy-8/operation/operation-new", ApiPageStatus.Missing)]
	[Arguments("/docs/api/elasticsearch/v9/types/legacy", ApiPageStatus.Missing)]
	[Arguments("/docs/api/logstash/v9/operation/operation-search", ApiPageStatus.Exists)]
	[Arguments("/docs/api/logstash/v9", ApiPageStatus.Exists)]
	[Arguments("/docs/api/logstash/operation/operation-search", ApiPageStatus.Exists)]
	[Arguments("/docs/api/logstash/next/operation/operation-new", ApiPageStatus.Exists)]
	[Arguments("/docs/api/logstash/v9/operation/operation-new", ApiPageStatus.Missing)]
	[Arguments("/docs/api/logstash/v7/operation/operation-search", ApiPageStatus.Missing)]
	[Arguments("/docs/api/kibana/operation/operation-x", ApiPageStatus.Unknown)]
	[Arguments("/docs/solutions/search", ApiPageStatus.Unknown)]
	public void CheckApiPage_PathInTheSections_ReportsWhetherThePageExists(string sitePath, ApiPageStatus expected) =>
		Resolver().CheckApiPage(sitePath).Should().Be(expected);

	[Test]
	public void CheckApiPage_ResolverWithoutSections_IsUnknown() =>
		((ICrossLinkResolver)NoopCrossLinkResolver.Instance).CheckApiPage("/docs/api/elasticsearch").Should().Be(ApiPageStatus.Unknown);

	[Test]
	[Arguments("https://www.elastic.co/docs/api/elasticsearch/operation/operation-search#responses", false)]
	[Arguments("https://www.elastic.co/docs/api/elasticsearch//operation/operation-search/", false)]
	[Arguments("https://www.elastic.co/docs/api/elasticsearch/operation/operation-gone", true)]
	[Arguments("/docs/api/elasticsearch/operation/operation-gone", true)]
	[Arguments("https://www.elastic.co/docs/api/kibana/operation/operation-x", false)]
	[Arguments("https://www.elastic.co/docs/solutions/search", false)]
	[Arguments("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-gone", true)]
	// The edge redirects the old serverless root to kibana-serverless, so the page is checked there.
	[Arguments("https://www.elastic.co/docs/api/doc/serverless/operation/operation-removed", true)]
	[Arguments("https://www.elastic.co/docs/api/doc/observability-serverless/operation/operation-x", true)]
	// Spec downloads are files, not pages of a section.
	[Arguments("https://www.elastic.co/docs/api/elasticsearch.yaml", false)]
	[Arguments("https://www.elastic.co/docs/api/doc/elasticsearch.yaml", false)]
	public void GetHint_Link_HintsOnlyForAKnownMissingApiPage(string url, bool hints) =>
		(ApiLinkCheck.GetHint(Resolver(), url, "/docs", [HintType.ApiLinkLegacyPrefix]) is not null).Should().Be(hints);

	[Test]
	public void GetHint_LegacyLinks_NameTheirUrlAfterTheCutover()
	{
		string[] urls =
		[
			"https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search#responses",
			"https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search?lang=console#responses",
			"https://www.elastic.co/docs/api/doc/serverless/operation/operation-live",
			"https://www.elastic.co/docs/api/doc/elasticsearch/servers",
			"https://www.elastic.co/docs/api/doc/kibana/operation/operation-x",
			"https://www.elastic.co/docs/api/elasticsearch/operation/operation-search"
		];

		string
			.Join('\n', urls.Select(url => $"{url}\n  {ApiLinkCheck.GetHint(Resolver(), url, "/docs", null) ?? "(no hint)"}"))
			.Should()
			.MatchInlineSnapshot(
				"""
				https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search#responses
				  API link 'https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search#responses' uses the legacy /docs/api/doc/ prefix. After the API Explorer cutover this page lives at 'https://www.elastic.co/docs/api/elasticsearch/operation/operation-search#responses'.
				https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search?lang=console#responses
				  API link 'https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search?lang=console#responses' uses the legacy /docs/api/doc/ prefix. After the API Explorer cutover this page lives at 'https://www.elastic.co/docs/api/elasticsearch/operation/operation-search?lang=console#responses'.
				https://www.elastic.co/docs/api/doc/serverless/operation/operation-live
				  API link 'https://www.elastic.co/docs/api/doc/serverless/operation/operation-live' uses the legacy /docs/api/doc/ prefix. After the API Explorer cutover this page lives at 'https://www.elastic.co/docs/api/kibana-serverless/operation/operation-live'.
				https://www.elastic.co/docs/api/doc/elasticsearch/servers
				  API link 'https://www.elastic.co/docs/api/doc/elasticsearch/servers' uses the legacy /docs/api/doc/ prefix. After the API Explorer cutover this page lives at 'https://www.elastic.co/docs/api/elasticsearch'.
				https://www.elastic.co/docs/api/doc/kibana/operation/operation-x
				  API link 'https://www.elastic.co/docs/api/doc/kibana/operation/operation-x' uses the legacy /docs/api/doc/ prefix. After the API Explorer cutover this page lives at 'https://www.elastic.co/docs/api/kibana/operation/operation-x'.
				https://www.elastic.co/docs/api/elasticsearch/operation/operation-search
				  (no hint)
				"""
			);
	}

	[Test]
	public void GetHint_SuppressedHint_IsNull() =>
		ApiLinkCheck
			.GetHint(
				Resolver(),
				"https://www.elastic.co/docs/api/elasticsearch/operation/operation-gone",
				"/docs",
				[HintType.ApiLinkMissingPage]
			)
			.Should()
			.BeNull();

	[Test]
	public void GetHint_SuppressedLegacyHint_IsNull() =>
		ApiLinkCheck
			.GetHint(
				Resolver(),
				"https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search",
				"/docs",
				[HintType.ApiLinkLegacyPrefix]
			)
			.Should()
			.BeNull();
}
