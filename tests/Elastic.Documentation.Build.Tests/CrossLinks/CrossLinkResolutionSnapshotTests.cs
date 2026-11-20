// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests.CrossLinks;

/// <summary>
/// The resolution table: a cross-link string, a link index and an environment go in, and a URI or the diagnostic comes
/// out. One verified file per family of links; each lists every environment shape. All repositories are synthetic.
/// </summary>
public class CrossLinkResolutionSnapshotTests
{
	private static readonly FetchedCrossLinks Index = CrossLinkFixtures.CreateIndex();

	[Test]
	public void Resolve_PlainPages_InEveryEnvironment() =>
		CrossLinkFixtures
			.Table(
				Index,
				[
					"example-repo://index.md",
					"example-repo://get-started/index.md",
					"example-repo://get-started",
					"example-repo://get-started/",
					"example-repo://guide/page.md",
					"example-repo://guide/page",
					"example-repo://guide/nested/deep.md",
					"example-repo://guide/no-anchors.md",
					"other-repo://index.md",
					"other-repo://target/page.md",
					"other-repo://target/index.md",
					"codex-repo://target/page.md"
				]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_Anchors_FoundMissingAndCaseVariants() =>
		CrossLinkFixtures
			.Table(
				Index,
				[
					"example-repo://get-started/index.md#intro",
					"example-repo://get-started/index.md#Intro",
					"example-repo://get-started/index.md#INTRO",
					"example-repo://get-started/index.md#use-cases",
					"example-repo://get-started/index.md#missing",
					"example-repo://get-started/index.md#",
					"example-repo://get-started/index.md#intro#extra",
					"example-repo://guide/page.md#section-one",
					"example-repo://guide/no-anchors.md#anything",
					"example-repo://index.md#anything",
					"other-repo://target/page.md#target-anchor",
					"other-repo://target/page.md#target-anchor-2"
				]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_HiddenPages_ResolveLikeAnyOther() =>
		CrossLinkFixtures
			.Table(
				Index,
				["example-repo://hidden/secret.md", "example-repo://hidden/secret.md#inside", "example-repo://hidden/secret.md#outside"]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_UriParsingQuirks_ShowWhatTheUriTypeDoes() =>
		CrossLinkFixtures
			.Table(
				Index,
				[
					"example-repo://Guide/Page.md",
					"example-repo://guide/Mixed-Case.md",
					"example-repo://guide/mixed-case.md",
					"EXAMPLE-REPO://guide/page.md",
					"example-repo://guide/page.md?query=1",
					"example-repo://guide/page.md?query=1#section-one",
					"example-repo://guide/page.md/",
					"example-repo://guide//page.md",
					"example-repo://guide/./page.md",
					"example-repo://guide/nested/../page.md",
					"example-repo://guide/page%2Emd",
					"example-repo://guide/some%20page.md",
					"example-repo:guide/page.md",
					"example-repo:///guide/page.md",
					"example-repo://",
					"example-repo://index.md/",
					"example-repo://user@guide/page.md",
					"example-repo://guide:80/page.md",
					"example-repo//guide/page.md",
					"guide/page.md"
				],
				[CrossLinkFixtures.Environments[0], CrossLinkFixtures.Environments[3]]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_UnknownRepositoriesAndPaths_ReportTheDiagnostic() =>
		CrossLinkFixtures
			.Table(
				Index,
				[
					"unknown-repo://index.md",
					"unknown-repo://get-started/index.md#intro",
					"cursor://file/path",
					"vscode://settings",
					"https://example.test/page",
					"mailto:someone@example.test",
					"declared-only://index.md",
					"declared-only://some/page.md#anchor",
					"declared-only://",
					"example-repo://missing.md",
					"example-repo://guide/missing.md",
					"example-repo://guide/missing.md#anchor",
					"example-repo://get-started/missing/index.md",
					"example-repo://get-started/index",
					"example-repo://get-started/index.md/extra"
				],
				[CrossLinkFixtures.Environments[0], CrossLinkFixtures.Environments[1], CrossLinkFixtures.Environments[3]]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_Redirects_FollowTheRulesAndTheirAnchors() =>
		CrossLinkFixtures
			.Table(
				Index,
				[
					"example-repo://old/simple.md",
					"example-repo://old/simple.md#anything",
					"example-repo://old/anchors.md",
					"example-repo://old/anchors.md#a",
					"example-repo://old/anchors.md#c",
					"example-repo://old/anchors.md#unmapped",
					"example-repo://old/catch-all.md#anything",
					"example-repo://old/empty-anchors.md#passes-through",
					"example-repo://old/no-to.md",
					"example-repo://old/no-to.md#a",
					"example-repo://old/no-to.md#z",
					"example-repo://old/many.md",
					"example-repo://old/many.md#intro",
					"example-repo://old/many.md#dropped",
					"example-repo://old/many.md#section-one",
					"example-repo://old/many.md#unmapped",
					"example-repo://old/many-catch-all.md#whatever",
					"example-repo://old/many-catch-all.md",
					"example-repo://old/many-only.md",
					"example-repo://old/many-only.md#x",
					"example-repo://old/many-only.md#unmapped",
					"example-repo://old/many-without-to.md#x",
					"example-repo://old/to-index.md",
					"example-repo://old/to-nested-index.md",
					"example-repo://old/to-missing-page.md",
					"example-repo://redirect-and-link.md",
					"example-repo://redirect-and-link.md#anything"
				],
				[CrossLinkFixtures.Environments[0], CrossLinkFixtures.Environments[3], CrossLinkFixtures.Environments[4]]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_CrossRepositoryRedirects_CheckTheTargetRepository() =>
		CrossLinkFixtures
			.Table(
				Index,
				[
					"example-repo://old/cross.md",
					"example-repo://old/cross.md#ignored",
					"example-repo://old/cross-anchors.md",
					"example-repo://old/cross-anchors.md#a",
					"example-repo://old/cross-anchors.md#b",
					"example-repo://old/cross-missing-target.md",
					"example-repo://old/cross-missing-repo.md",
					"example-repo://old/cross-declared-only.md",
					"example-repo://old/external.md"
				]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_FloatingSections_PickVersionsAndAliases() =>
		CrossLinkFixtures
			.Table(
				Index,
				[
					"example-repo://api/example-api",
					"example-repo://api/example-api/",
					"example-repo://api/example-api/operation/operation-search",
					"example-repo://api/example-api/operation/operation-search#fragment",
					"example-repo://api/example-api/operation/operation-new",
					"example-repo://api/example-api/operation/operation-old",
					"example-repo://api/example-api/operation/operation-missing",
					"example-repo://api/example-api/8",
					"example-repo://api/example-api/8/operation/operation-search",
					"example-repo://api/example-api/8/operation/operation-search-legacy",
					"example-repo://api/example-api/8/operation/operation-old",
					"example-repo://api/example-api/8/operation/operation-new",
					"example-repo://api/example-api/main/operation/operation-search",
					"example-repo://api/example-api/main/operation/operation-search-legacy",
					"example-repo://api/example-api/main",
					"example-repo://api/example-api/9/operation/operation-search",
					"example-repo://api/example-api/operation/operation-search-legacy",
					"example-repo://api/other-api/operation/x",
					"example-repo://api",
					"example-repo://api/"
				],
				[
					CrossLinkFixtures.Environments[0],
					CrossLinkFixtures.Environments[1],
					CrossLinkFixtures.Environments[3],
					CrossLinkFixtures.Environments[4]
				]
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Resolve_NoopResolverAndIsolatedDefault_AnswerWithoutAnIndex()
	{
		var sb = new StringBuilder();
		foreach (var input in new[] { "example-repo://index.md", "unknown://x", "https://example.test/x" })
		{
			var uri = new Uri(input);
			var noop = NoopCrossLinkResolver.Instance;
			_ = sb
				.AppendLine(input)
				.Line($"  noop             {CrossLinkFixtures.Describe(noop.Resolve(uri), uri)}")
				.Line($"  noop-declared    {noop.IsDeclaredCrossLinkScheme(uri.Scheme)}")
				.Line($"  empty-index      {CrossLinkFixtures.Describe(new CrossLinkResolver(FetchedCrossLinks.Empty).Resolve(uri), uri)}");
		}

		_ = sb.Line($"noop uri resolver: {NoopCrossLinkResolver.Instance.UriResolver.GetType().Name}");
		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void IsDeclaredCrossLinkScheme_Schemes_AreMatchedExactly()
	{
		var resolver = new CrossLinkResolver(Index);
		var sb = new StringBuilder();
		foreach (var scheme in new[]
		{
			"example-repo",
			"other-repo",
			"declared-only",
			"codex-repo",
			"Example-Repo",
			"unknown-repo",
			"cursor",
			"https",
			""
		})
			_ = sb.Line($"{(scheme.Length == 0 ? "(empty)" : scheme),-15} {resolver.IsDeclaredCrossLinkScheme(scheme)}");

		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			example-repo    True
			other-repo      True
			declared-only   True
			codex-repo      True
			Example-Repo    False
			unknown-repo    False
			cursor          False
			https           False
			(empty)         False

			"""
			);
	}

	[Test]
	public void CheckApiPage_SitePaths_ReportExistsMissingOrUnknown()
	{
		var resolver = new CrossLinkResolver(Index);
		var sb = new StringBuilder();
		foreach (var path in new[]
		{
			"/docs/api/doc/example-api",
			"/docs/api/doc/example-api/",
			"/docs/api/doc/example-api/operation/operation-search",
			"/docs/api/doc/example-api/operation/operation-search-legacy",
			"/docs/api/doc/example-api/v8",
			"/docs/api/doc/example-api/v8/operation/operation-search",
			"/docs/api/doc/example-api/v8/operation/operation-search-legacy",
			"/docs/api/doc/example-api/v8/operation/operation-new",
			"/docs/api/doc/example-api/v9/operation/operation-search",
			"/docs/api/doc/example-api/operation/operation-missing",
			"api/doc/example-api/operation/operation-new",
			"/docs/api/doc/other-api/operation/x",
			"/docs/api/doc/",
			"/docs/api/doc",
			"/docs/reference/page",
			"/api/doc/example-api",
			""
		})
			_ = sb.Line($"{(path.Length == 0 ? "(empty)" : path)} => {resolver.CheckApiPage(path)}");

		_ = sb.Line($"noop: {((ICrossLinkResolver)NoopCrossLinkResolver.Instance).CheckApiPage("/docs/api/doc/example-api")}");
		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void ToTargetUrlPath_Paths_StripMarkdownExtensionAndIndex()
	{
		var sb = new StringBuilder();
		foreach (var path in new[]
		{
			"index.md",
			"index",
			"page.md",
			"page",
			"guide/index.md",
			"guide/INDEX.md",
			"guide/index",
			"guide/page.md",
			"guide/page.md/",
			"guide/",
			"a.md.md",
			"a.mdx",
			"readme.mdown",
			"my.md-file/page.md",
			"guide/index.md/index.md",
			"guide/subindex.md",
			"indexed/page.md",
			"/index.md",
			"",
			"guide/Page.MD"
		})
			_ = sb.Line($"'{path}' => '{CrossLinkResolver.ToTargetUrlPath(path)}'");

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void UpdateLinkReference_ReplacesOrAddsARepository_AndResolvesFromIt()
	{
		var resolver = new CrossLinkResolver(Index);
		var replacement = LinkManifestJson.Read(
			// language=json
			"""
			{
			  "origin": { "branch": "main", "remote": "r", "ref": "x" },
			  "url_path_prefix": "",
			  "cross_links": [],
			  "links": { "only-now.md": {} }
			}
			"""
		);
		var sb = new StringBuilder();

		void Line(string input)
		{
			var uri = new Uri(input);
			_ = sb.Line($"{input,-40} {CrossLinkFixtures.Describe(resolver.Resolve(uri), uri)}");
		}

		Line("example-repo://guide/page.md");
		Line("example-repo://only-now.md");
		var updated = resolver.UpdateLinkReference("example-repo", replacement);
		_ = sb.Line($"references after: {string.Join(", ", updated.LinkReferences.Keys.Order())}");
		Line("example-repo://guide/page.md");
		Line("example-repo://only-now.md");
		_ = resolver.UpdateLinkReference("brand-new", replacement);
		Line("brand-new://only-now.md");
		_ = sb.Line($"declared after: {string.Join(", ", updated.DeclaredRepositories.Order())}");

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void Resolve_NotInIndexMessages_ShowTheLinksJsonLocationForEachRegistryShape()
	{
		var cases = new (string Name, FrozenDictionary<string, string>? Urls, FrozenDictionary<string, DocSetRegistry>? Registries)[]
		{
			("no-registry-info", null, null),
			("public-s3", Urls("https://registry.example.test/bucket"), Registries(DocSetRegistry.Public)),
			("s3-trailing-slash", Urls("https://registry.example.test/bucket/"), Registries(DocSetRegistry.Public)),
			("link-index-json-url", Urls("https://registry.example.test/bucket/link-index.json"), Registries(DocSetRegistry.Public)),
			("github-url", Urls("https://github.com/example-org/link-index/"), Registries(DocSetRegistry.Public)),
			("internal-registry", Urls("https://internal.example.test/codex"), Registries(DocSetRegistry.Internal)),
			("registry-only", null, Registries(DocSetRegistry.Internal)),
			("url-only", Urls("https://registry.example.test/bucket"), null)
		};
		var sb = new StringBuilder();
		foreach (var (name, urls, registries) in cases)
		{
			// declared-only has no LinkIndexEntries, example-repo has one: that is the difference in the message
			var index = CrossLinkFixtures.CreateIndex(urls, registries);
			_ = sb.AppendLine(name);
			foreach (var input in new[] { "example-repo://missing.md", "other-repo://missing.md", "codex-repo://missing.md" })
			{
				var uri = new Uri(input);
				_ = sb.Line(
					$"  {CrossLinkFixtures.Describe(CrossLinkResolver.Resolve(index, new IsolatedBuildEnvironmentUriResolver(), uri), uri)}"
				);
			}
		}

		sb.ToString().Should().MatchSnapshot();

		static FrozenDictionary<string, string> Urls(string url) =>
			new Dictionary<string, string> { ["example-repo"] = url, ["other-repo"] = url, ["codex-repo"] = url }.ToFrozenDictionary();

		static FrozenDictionary<string, DocSetRegistry> Registries(DocSetRegistry registry) =>
			new Dictionary<string, DocSetRegistry>
			{
				["example-repo"] = registry,
				["other-repo"] = registry,
				["codex-repo"] = registry
			}.ToFrozenDictionary();
	}

	[Test]
	public void Resolve_EnvironmentShapes_ForTheSameResolvedPath()
	{
		var sb = new StringBuilder();
		foreach (var input in new[]
		{
			"example-repo://index.md",
			"example-repo://get-started/index.md",
			"example-repo://guide/page.md#section-one",
			"example-repo://guide/nested/deep.md#deep",
			"other-repo://target/page.md",
			"codex-repo://target/page.md"
		})
		{
			var uri = new Uri(input);
			_ = sb.AppendLine(input);
			foreach (var environment in CrossLinkFixtures.Environments)
			{
				var resolver = environment.Create();
				var path = CrossLinkResolver.ToTargetUrlPath((uri.Host + '/' + uri.AbsolutePath.TrimStart('/')).Trim('/'));
				var resolved = resolver.Resolve(uri, path + uri.Fragment);
				var site = resolver.ResolveSitePath(uri, "api/doc/example-api/operation/operation-search");
				_ = sb.Line($"  {environment.Name,-24} {(resolved.IsAbsoluteUri ? resolved.AbsoluteUri : resolved.OriginalString)}");
				_ = sb.Line($"  {"",-24} site path: {site.AbsoluteUri}");
			}
		}

		sb.ToString().Should().MatchSnapshot();
	}
}
