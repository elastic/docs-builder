// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionNoticeTests
{
	private static IReadOnlyDictionary<string, IReadOnlyList<string>> Pages(params string[] pages) =>
		pages.ToDictionary(p => p, IReadOnlyList<string> (p) => [p]);

	private static string Notice(string[] monikers, string currentSuffix, ApiPageType type, string pageUrl)
	{
		var items = ApiVersionSwitcher.Build(
			"",
			ApiVersionRoutes.For("elasticsearch", monikers),
			monikers,
			currentSuffix,
			new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
			{
				["main"] = Pages("", "/operation/operation-ping", "/operation/operation-newop"),
				["9"] = Pages("", "/operation/operation-ping"),
				["8"] = Pages("", "/operation/operation-ping", "/operation/operation-oldop")
			}
		);
		return ApiVersionNotices.For(type, ApiVersionSwitcher.ForPage(items, pageUrl))?.ToString() ?? "none";
	}

	[Test]
	public void For_EachPageType_NamesTheVersionAndLinksToTheNewestMajor()
	{
		string[] all = ["main", "9", "8"];
		var notices = string.Join(
			"\n",
			$"versionless: {Notice(all, "elasticsearch", ApiPageType.Versionless, "/api/elasticsearch/operation/operation-ping")}",
			$"v9 copy: {Notice(all, "elasticsearch/v9", ApiPageType.NewestMajorCopy, "/api/elasticsearch/v9/operation/operation-ping")}",
			$"v8: {Notice(all, "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8/operation/operation-ping")}",
			$"v8 only in v8: {Notice(all, "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8/operation/operation-oldop")}",
			$"v8 landing: {Notice(all, "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8")}",
			$"next: {Notice(all, "elasticsearch/next", ApiPageType.Unreleased, "/api/elasticsearch/next/operation/operation-ping")}",
			$"next only in main: {Notice(all, "elasticsearch/next", ApiPageType.Unreleased, "/api/elasticsearch/next/operation/operation-newop")}",
			$"next with main and 9: {Notice(["main", "9"], "elasticsearch/next", ApiPageType.Unreleased, "/api/elasticsearch/next/operation/operation-ping")}",
			$"single version: {Notice(["9"], "elasticsearch", ApiPageType.Versionless, "/api/elasticsearch/operation/operation-ping")}"
		);

		notices.Should().MatchInlineSnapshot(
			"""
			versionless: none
			v9 copy: none
			v8: OlderMajorNotice { LatestLabel = v9, LatestUrl = /api/elasticsearch/operation/operation-ping, Version = 8 }
			v8 only in v8: OlderMajorNotice { LatestLabel = v9, LatestUrl = /api/elasticsearch, Version = 8 }
			v8 landing: OlderMajorNotice { LatestLabel = v9, LatestUrl = /api/elasticsearch, Version = 8 }
			next: UnreleasedNotice { LatestLabel = v9, LatestUrl = /api/elasticsearch/operation/operation-ping }
			next only in main: UnreleasedNotice { LatestLabel = v9, LatestUrl = /api/elasticsearch }
			next with main and 9: UnreleasedNotice { LatestLabel = v9, LatestUrl = /api/elasticsearch/operation/operation-ping }
			single version: none
			"""
		);
	}
}
