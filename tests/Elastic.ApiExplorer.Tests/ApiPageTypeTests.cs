// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
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
	[Arguments(ApiPageType.Versionless, true, "index, follow")]
	[Arguments(ApiPageType.NewestMajorCopy, true, "index, follow")]
	[Arguments(ApiPageType.OlderMajor, true, "index, follow")]
	[Arguments(ApiPageType.Unreleased, true, "noindex")]
	[Arguments(ApiPageType.Versionless, false, "noindex, nofollow")]
	[Arguments(ApiPageType.Unreleased, false, "noindex, nofollow")]
	public void Robots_PageTypeAndBuild_BuildThatDisallowsIndexingWins(ApiPageType type, bool buildAllowsIndexing, string expected) =>
		ApiPageTypes.Robots(type, buildAllowsIndexing).Should().Be(expected);

	[Test]
	[Arguments(ApiPageType.NewestMajorCopy, "/api/es/v9/operation/operation-ping", "/api/es/v9", "/api/es/operation/operation-ping")]
	[Arguments(ApiPageType.NewestMajorCopy, "/api/es/v9", "/api/es/v9", "/api/es")]
	[Arguments(ApiPageType.OlderMajor, "/api/es/v8/operation/x", "/api/es/v8", "/api/es/v8/operation/x")]
	[Arguments(ApiPageType.Unreleased, "/api/es/next/operation/x", "/api/es/next", "/api/es/next/operation/x")]
	public void CanonicalPath_PageType_KeepsThePathBelowTheRoot(ApiPageType type, string page, string treeRoot, string expected) =>
		ApiPageTypes.CanonicalPath(type, page, treeRoot, "/api/es").Should().Be(expected);
}
