// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>
/// Tables of what <see cref="PublishEnvironmentUriResolver"/> does today with cross-link URIs, path prefixes and mounts, including
/// the cases where the answer looks wrong: these record behaviour ahead of the domain model refactor and fix nothing.
/// </summary>
public class PublishEnvironmentMountResolutionTests
{
	private static FrozenDictionary<Uri, NavigationTocMapping> Mappings(params (string Source, string Prefix)[] mounts) =>
		mounts.ToFrozenDictionary(
			m => new Uri(m.Source),
			m => new NavigationTocMapping { Source = new Uri(m.Source), SourcePathPrefix = m.Prefix }
		);

	private static PublishEnvironmentUriResolver Resolver(
		FrozenDictionary<Uri, NavigationTocMapping> mappings,
		string? pathPrefix = "docs"
	) =>
		new(
			mappings,
			// An environment that renders the API Explorer, so site paths keep their form; PublishEnvironmentSitePathTests covers the other.
			new PublishEnvironment
			{
				Uri = "https://www.elastic.co",
				PathPrefix = pathPrefix,
				FeatureFlags = new() { ["ASSEMBLER_API_EXPLORER"] = true }
			}
		);

	private static string ResolveTable(PublishEnvironmentUriResolver resolver, params (string CrossLink, string Path)[] probes)
	{
		var sb = new StringBuilder();
		foreach (var (crossLink, path) in probes)
		{
			var uri = new Uri(crossLink);
			_ = sb
				.Append(crossLink)
				.Append("  ")
				.Append(path)
				.Append("  =>  ")
				.Append(resolver.Resolve(uri, path))
				.Append("  [")
				.Append(string.Join(", ", resolver.ResolveToSubPaths(uri, path)))
				.Append("]\n");
		}
		return sb.ToString();
	}

	[Test]
	public void TocMappings_OfTheBaselineNavigation_MatchSnapshot() =>
		MappingsTable(BaselineSite.TocMappings("prod", skipPrivateRepositories: false))
			.Should()
			.MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void TocMappings_SkippingPrivateRepositories_MatchSnapshot() =>
		MappingsTable(BaselineSite.TocMappings("prod", skipPrivateRepositories: true))
			.Should()
			.MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void TocMappings_OfTheNavigationPreview_MatchSnapshot() =>
		MappingsTable(BaselineSite.TocMappings("demo", skipPrivateRepositories: false))
			.Should()
			.MatchSnapshot(options: BaselineDumps.Scrubbed());

	private static string MappingsTable(FrozenDictionary<Uri, NavigationTocMapping> mappings) =>
		string.Concat(
			mappings.Values.OrderBy(m => m.Source.ToString(), StringComparer.Ordinal).Select(m => $"{m.Source}  =>  {m.SourcePathPrefix}\n")
		);

	[Test]
	public void Resolve_AgainstTheBaselineMappings_MatchesSnapshot() =>
		ResolveTable(
			Resolver(BaselineSite.TocMappings("prod", skipPrivateRepositories: false)),
			("docs-content://get-started/index.md", "get-started/index.md"),
			("docs-content://get-started/", "get-started"),
			("docs-content://solutions/observability/apm.md", "solutions/observability/apm.md"),
			("docs-content://index.md", "index.md"),
			("elasticsearch://reference/elasticsearch/index.md", "reference/elasticsearch/index.md"),
			("elasticsearch://reference/query-languages/esql/esql-syntax.md", "reference/query-languages/esql/esql-syntax.md"),
			("elasticsearch://reference/text-analysis/analyzer-reference.md", "reference/text-analysis/analyzer-reference.md"),
			("elasticsearch://release-notes/index.md", "release-notes/index.md"),
			("docs-content://reference/elasticsearch-clients/index.md", "reference/elasticsearch-clients/index.md"),
			("elasticsearch-java://reference/getting-started.md", "reference/getting-started.md"),
			("elasticsearch-java://release-notes/index.md", "release-notes/index.md"),
			("kibana://reference/configuration-reference/alerting-settings.md", "reference/configuration-reference/alerting-settings.md"),
			("kibana://extend/index.md", "extend/index.md"),
			("curator://reference/index.md", "reference/index.md"),
			("apm-server://reference/index.md", "reference/index.md"),
			("apm-agent-java://reference/index.md", "reference/index.md"),
			("ecs://reference/ecs-field-reference.md", "reference/ecs-field-reference.md"),
			("unknown-repo://reference/index.md", "reference/index.md")
		).Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Resolve_WherePrefixesStartTheSame_MatchesSnapshot() =>
		ResolveTable(
			Resolver(
				Mappings(
					("docs-content://reference/apm", "reference/apm"),
					("docs-content://reference/apm-agents", "reference/apm-agents"),
					("docs-content://reference", "reference")
				)
			),
			("docs-content://reference/apm/index.md", "reference/apm/index.md"),
			("docs-content://reference/apm/server/setup.md", "reference/apm/server/setup.md"),
			("docs-content://reference/apm-agents/index.md", "reference/apm-agents/index.md"),
			("docs-content://reference/apm-agents/java/index.md", "reference/apm-agents/java/index.md"),
			("docs-content://reference/apm-agentsx/index.md", "reference/apm-agentsx/index.md"),
			("docs-content://reference/apmx/index.md", "reference/apmx/index.md"),
			("docs-content://reference/index.md", "reference/index.md"),
			("docs-content://referencex/index.md", "referencex/index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			docs-content://reference/apm/index.md  reference/apm/index.md  =>  https://www.elastic.co/docs/reference/apm/index.md  [reference/apm/index/, reference/apm/]
			docs-content://reference/apm/server/setup.md  reference/apm/server/setup.md  =>  https://www.elastic.co/docs/reference/apm/server/setup.md  [reference/apm/server/, reference/apm/server/setup/, reference/apm/]
			docs-content://reference/apm-agents/index.md  reference/apm-agents/index.md  =>  https://www.elastic.co/docs/reference/apm-agents/index.md  [reference/apm-agents/index/, reference/apm-agents/]
			docs-content://reference/apm-agents/java/index.md  reference/apm-agents/java/index.md  =>  https://www.elastic.co/docs/reference/apm-agents/java/index.md  [reference/apm-agents/java/, reference/apm-agents/]
			docs-content://reference/apm-agentsx/index.md  reference/apm-agentsx/index.md  =>  https://www.elastic.co/docs/reference/apm-agents/x/index.md  [reference/apm-agents/x/, reference/apm-agents/]
			docs-content://reference/apmx/index.md  reference/apmx/index.md  =>  https://www.elastic.co/docs/reference/apm/x/index.md  [reference/apm/x/, reference/apm/]
			docs-content://reference/index.md  reference/index.md  =>  https://www.elastic.co/docs/reference/index.md  [reference/index/, reference/]
			docs-content://referencex/index.md  referencex/index.md  =>  https://www.elastic.co/docs/reference/x/index.md  [reference/x/, reference/]
			"""
			);

	[Test]
	public void Resolve_WithoutAMappingForTheLongerPrefix_FallsBackToTheShorterOne() =>
		ResolveTable(
			Resolver(Mappings(("docs-content://reference/apm", "reference/apm"))),
			("docs-content://reference/apm/index.md", "reference/apm/index.md"),
			("docs-content://reference/apm-agents/index.md", "reference/apm-agents/index.md"),
			("docs-content://reference/apm-agents/java/index.md", "reference/apm-agents/java/index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			docs-content://reference/apm/index.md  reference/apm/index.md  =>  https://www.elastic.co/docs/reference/apm/index.md  [reference/apm/index/, reference/apm/]
			docs-content://reference/apm-agents/index.md  reference/apm-agents/index.md  =>  https://www.elastic.co/docs/reference/apm/-agents/index.md  [reference/apm/-agents/, reference/apm/]
			docs-content://reference/apm-agents/java/index.md  reference/apm-agents/java/index.md  =>  https://www.elastic.co/docs/reference/apm/-agents/java/index.md  [reference/apm/-agents/, reference/apm/-agents/java/, reference/apm/]
			"""
			);

	[Test]
	public void Resolve_RepositoryRootMounts_UseTheirPathPrefixAsTheBase() =>
		ResolveTable(
			Resolver(Mappings(("apm-server://", "reference/apm"), ("apm-server-extra://", "reference/apm-extra"))),
			("apm-server://index.md", "index.md"),
			("apm-server://setup/install.md", "setup/install.md"),
			("apm-server://setup/", "setup"),
			("apm-server-extra://index.md", "index.md"),
			("apm-server-extra://a/b.md", "a/b.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			apm-server://index.md  index.md  =>  https://www.elastic.co/docs/reference/apm/index.md  [reference/apm/index/, reference/apm/]
			apm-server://setup/install.md  setup/install.md  =>  https://www.elastic.co/docs/reference/apm/setup/install.md  [reference/apm/setup/, reference/apm/setup/install/, reference/apm/]
			apm-server://setup/  setup  =>  https://www.elastic.co/docs/reference/apm/setup  [reference/apm/setup/, reference/apm/]
			apm-server-extra://index.md  index.md  =>  https://www.elastic.co/docs/reference/apm-extra/index.md  [reference/apm-extra/index/, reference/apm-extra/]
			apm-server-extra://a/b.md  a/b.md  =>  https://www.elastic.co/docs/reference/apm-extra/a/b.md  [reference/apm-extra/a/, reference/apm-extra/a/b/, reference/apm-extra/]
			"""
			);

	[Test]
	public void Resolve_PathThatDoesNotStartWithTheSourcePrefix_KeepsTheWholePath() =>
		ResolveTable(
			Resolver(Mappings(("elasticsearch://reference/elasticsearch", "reference/elasticsearch"))),
			("elasticsearch://reference/elasticsearch/index.md", "reference/elasticsearch/index.md"),
			("elasticsearch://reference/elasticsearch/index.md", "other/index.md"),
			("elasticsearch://reference/elasticsearch/index.md", "reference/elasticsearchish/index.md"),
			("elasticsearch://reference/elasticsearch", "reference/elasticsearch")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			elasticsearch://reference/elasticsearch/index.md  reference/elasticsearch/index.md  =>  https://www.elastic.co/docs/reference/elasticsearch/index.md  [reference/elasticsearch/index/, reference/elasticsearch/]
			elasticsearch://reference/elasticsearch/index.md  other/index.md  =>  https://www.elastic.co/docs/reference/elasticsearch/other/index.md  [reference/elasticsearch/other/, reference/elasticsearch/]
			elasticsearch://reference/elasticsearch/index.md  reference/elasticsearchish/index.md  =>  https://www.elastic.co/docs/reference/elasticsearch/ish/index.md  [reference/elasticsearch/ish/, reference/elasticsearch/]
			elasticsearch://reference/elasticsearch  reference/elasticsearch  =>  https://www.elastic.co/docs/reference/elasticsearch  [reference/elasticsearch/]
			"""
			);

	[Test]
	public void Resolve_EnvironmentPathPrefix_Variants_MatchSnapshot()
	{
		var mappings = Mappings(("kibana://reference", "reference/kibana"));
		var sb = new StringBuilder();
		foreach (var prefix in new string?[] { "docs", "", null, "/docs", "docs/", "a/b" })
		{
			var resolver = Resolver(mappings, prefix);
			_ = sb
				.Append('[')
				.Append(prefix ?? "null")
				.Append("] mapped=")
				.Append(resolver.Resolve(new Uri("kibana://reference/page.md"), "reference/page.md"))
				.Append(" unmapped=")
				.Append(resolver.Resolve(new Uri("other://page.md"), "page.md"))
				.Append(" site=")
				.Append(resolver.ResolveSitePath(new Uri("kibana://reference/page.md"), "api/x"))
				.Append('\n');
		}
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			[docs] mapped=https://www.elastic.co/docs/reference/kibana/page.md unmapped=https://www.elastic.co/docs/page.md site=https://www.elastic.co/docs/api/x
			[] mapped=https://www.elastic.co/reference/kibana/page.md unmapped=https://www.elastic.co/page.md site=https://www.elastic.co/api/x
			[null] mapped=https://www.elastic.co/reference/kibana/page.md unmapped=https://www.elastic.co/page.md site=https://www.elastic.co/api/x
			[/docs] mapped=https://www.elastic.co/docs/reference/kibana/page.md unmapped=https://www.elastic.co/docs/page.md site=https://www.elastic.co/docs/api/x
			[docs/] mapped=https://www.elastic.co/docs//reference/kibana/page.md unmapped=https://www.elastic.co/docs//page.md site=https://www.elastic.co/docs//api/x
			[a/b] mapped=https://www.elastic.co/a/b/reference/kibana/page.md unmapped=https://www.elastic.co/a/b/page.md site=https://www.elastic.co/a/b/api/x
			"""
			);
	}

	[Test]
	public void MarkdownPathToUrlPath_Table_MatchSnapshot()
	{
		var sb = new StringBuilder();
		foreach (var path in new[]
		{
			"index.md",
			"a/index.md",
			"a/b.md",
			"a/b",
			"a/b/",
			"a/b.MD",
			"a/INDEX.MD",
			"a/index",
			"a/b.md.md",
			"a/sub/index.md/",
			"a/indexed.md",
			"",
			".md",
			"/index.md"
		})
			_ = sb
				.Append('[')
				.Append(path)
				.Append("] => [")
				.Append(PublishEnvironmentUriResolver.MarkdownPathToUrlPath(path))
				.Append("]\n");
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			[index.md] => [index]
			[a/index.md] => [a/]
			[a/b.md] => [a/b]
			[a/b] => [a/b]
			[a/b/] => [a/b/]
			[a/b.MD] => [a/b]
			[a/INDEX.MD] => [a/]
			[a/index] => [a/index]
			[a/b.md.md] => [a/b.md]
			[a/sub/index.md/] => [a/sub/index.md/]
			[a/indexed.md] => [a/indexed]
			[] => []
			[.md] => []
			[/index.md] => [/]
			"""
			);
	}

	[Test]
	public void ResolveToSubPaths_Table_MatchSnapshot() =>
		ResolveTable(
			Resolver(Mappings(("docs-content://reference", "reference"), ("kibana://", "reference/kibana"))),
			("docs-content://reference/a/b/c.md", "reference/a/b/c.md"),
			("docs-content://reference/a/index.md", "reference/a/index.md"),
			("docs-content://reference/index.md", "reference/index.md"),
			("kibana://a/b.md", "a/b.md"),
			("kibana://index.md", "index.md"),
			("nothing://a/b.md", "a/b.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			docs-content://reference/a/b/c.md  reference/a/b/c.md  =>  https://www.elastic.co/docs/reference/a/b/c.md  [reference/a/, reference/a/b/, reference/a/b/c/, reference/]
			docs-content://reference/a/index.md  reference/a/index.md  =>  https://www.elastic.co/docs/reference/a/index.md  [reference/a/, reference/]
			docs-content://reference/index.md  reference/index.md  =>  https://www.elastic.co/docs/reference/index.md  [reference/index/, reference/]
			kibana://a/b.md  a/b.md  =>  https://www.elastic.co/docs/reference/kibana/a/b.md  [reference/kibana/a/, reference/kibana/a/b/, reference/kibana/]
			kibana://index.md  index.md  =>  https://www.elastic.co/docs/reference/kibana/index.md  [reference/kibana/index/, reference/kibana/]
			nothing://a/b.md  a/b.md  =>  https://www.elastic.co/docs/a/b.md  []
			"""
			);
}
