// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Structural;
using Elastic.Documentation.Navigation;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class ApiPageTypeTests
{
	[Test]
	[Arguments("9", false, "9", ApiPageType.Versionless)]
	[Arguments("9", true, "9", ApiPageType.NewestMajorCopy)]
	[Arguments("8", false, "9", ApiPageType.OlderMajor)]
	[Arguments("main", false, "9", ApiPageType.Unreleased)]
	[Arguments("main", false, "main", ApiPageType.Versionless)]
	public void Of_Tree_ReturnsItsPageType(string moniker, bool isMirror, string defaultMoniker, ApiPageType expected) =>
		ApiPageTypes.Of(new ApiVersionRoutes("es", defaultMoniker), moniker, isMirror).Should().Be(expected);

	[Test]
	public void Indexing_EveryPageType_FollowsTheRfcTable() =>
		string
			.Join('\n', Enum.GetValues<ApiPageType>().Select(static t => $"{t}: {ApiPageTypes.Indexing(t)}"))
			.Should()
			.MatchInlineSnapshot(
				"""
				Versionless: ApiPageIndexing { CanonicalIsVersionless = False, Index = True, InSitemap = True }
				NewestMajorCopy: ApiPageIndexing { CanonicalIsVersionless = True, Index = True, InSitemap = False }
				OlderMajor: ApiPageIndexing { CanonicalIsVersionless = False, Index = True, InSitemap = True }
				Unreleased: ApiPageIndexing { CanonicalIsVersionless = False, Index = False, InSitemap = False }
				"""
			);

	[Test]
	[Arguments(ApiPageType.Versionless, true, false, "index, follow")]
	[Arguments(ApiPageType.NewestMajorCopy, true, false, "index, follow")]
	[Arguments(ApiPageType.OlderMajor, true, false, "index, follow")]
	[Arguments(ApiPageType.Unreleased, true, false, "noindex")]
	[Arguments(ApiPageType.Versionless, false, false, "noindex, nofollow")]
	[Arguments(ApiPageType.Unreleased, false, false, "noindex, nofollow")]
	[Arguments(ApiPageType.Versionless, true, true, "noindex")]
	[Arguments(ApiPageType.OlderMajor, true, true, "noindex")]
	[Arguments(ApiPageType.Versionless, false, true, "noindex, nofollow")]
	public void Robots_PageTypeAndBuild_BuildThatDisallowsIndexingWins(
		ApiPageType type,
		bool buildAllowsIndexing,
		bool excludeFromIndexing,
		string expected
	) => ApiPageTypes.Robots(type, buildAllowsIndexing, excludeFromIndexing).Should().Be(expected);

	[Test]
	[Arguments(ApiPageType.NewestMajorCopy, "/api/es/v9/operation/operation-ping", "/api/es/v9", "/api/es/operation/operation-ping")]
	[Arguments(ApiPageType.NewestMajorCopy, "/api/es/v9", "/api/es/v9", "/api/es")]
	[Arguments(ApiPageType.OlderMajor, "/api/es/v8/operation/x", "/api/es/v8", "/api/es/v8/operation/x")]
	[Arguments(ApiPageType.Unreleased, "/api/es/next/operation/x", "/api/es/next", "/api/es/next/operation/x")]
	public void CanonicalPath_PageType_KeepsThePathBelowTheRoot(ApiPageType type, string page, string treeRoot, string expected) =>
		ApiPageTypes.CanonicalPath(type, page, treeRoot, "/api/es").Should().Be(expected);

	[Test]
	[Arguments(ApiStructuralKind.Authentication, true, false)]
	[Arguments(ApiStructuralKind.Servers, false, true)]
	public void StructuralPage_HiddenFromTheSidebar_OnlyServersLeavesTheSitemapAndTheIndex(
		ApiStructuralKind kind,
		bool inSitemap,
		bool excludeFromIndexing
	)
	{
		var landing = new LandingNavigationItem("/api/es/");
		var item = new StructuralNavigationItem(null, "", new ApiStructuralPage(kind), landing, landing);

		item.Hidden.Should().BeTrue();
		item.InSitemap.Should().Be(inSitemap);
		// the layout reads the interface member, so assert through it
		((INavigationItem)item).ExcludeFromIndexing.Should().Be(excludeFromIndexing);
	}
}
