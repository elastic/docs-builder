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

	/// <param name="rendered">The versions whose tree rendered, which the switcher lists.</param>
	/// <param name="declared">The versions the API declares. They decide the versionless URL, so the newest of them can be missing from <paramref name="rendered"/>.</param>
	private static string Notice(
		string[] rendered,
		string moniker,
		string currentSuffix,
		ApiPageType type,
		string pageUrl,
		string[]? declared = null
	)
	{
		var routes = ApiVersionRoutes.For("elasticsearch", declared ?? rendered);
		var items = ApiVersionSwitcher.Build(
			"",
			routes,
			rendered,
			currentSuffix,
			new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
			{
				["main"] = Pages("", "/operation/operation-ping", "/operation/operation-newop"),
				["9"] = Pages("", "/operation/operation-ping"),
				["8"] = Pages("", "/operation/operation-ping", "/operation/operation-oldop")
			}
		);
		return ApiVersionNotices.For(type, moniker, routes.DefaultMoniker, ApiVersionSwitcher.ForPage(items, pageUrl))?.ToString()
			?? "none";
	}

	[Test]
	public void For_EachPageType_NamesTheVersionAndLinksToTheNewestMajor()
	{
		string[] all = ["main", "9", "8"];
		var notices = string.Join(
			"\n",
			$"versionless: {Notice(all, "9", "elasticsearch", ApiPageType.Versionless, "/api/elasticsearch/operation/operation-ping")}",
			$"v9 copy: {Notice(all, "9", "elasticsearch/v9", ApiPageType.NewestMajorCopy, "/api/elasticsearch/v9/operation/operation-ping")}",
			$"v8: {Notice(all, "8", "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8/operation/operation-ping")}",
			$"v8 only in v8: {Notice(all, "8", "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8/operation/operation-oldop")}",
			$"v8 landing: {Notice(all, "8", "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8")}",
			$"next: {Notice(all, "main", "elasticsearch/next", ApiPageType.Unreleased, "/api/elasticsearch/next/operation/operation-ping")}",
			$"next only in main: {Notice(all, "main", "elasticsearch/next", ApiPageType.Unreleased, "/api/elasticsearch/next/operation/operation-newop")}",
			$"next with main and 9: {Notice(["main", "9"], "main", "elasticsearch/next", ApiPageType.Unreleased, "/api/elasticsearch/next/operation/operation-ping")}",
			$"single version: {Notice(["9"], "9", "elasticsearch", ApiPageType.Versionless, "/api/elasticsearch/operation/operation-ping")}",
			$"no moniker: {ApiVersionNotices.For(ApiPageType.OlderMajor, null, "9", [])?.ToString() ?? "none"}"
		);

		notices.Should().MatchInlineSnapshot(
			"""
			versionless: none
			v9 copy: none
			v8: OlderMajorNotice { Latest = LatestVersionLink { Label = v9, Url = /api/elasticsearch/operation/operation-ping }, Version = 8 }
			v8 only in v8: OlderMajorNotice { Latest = LatestVersionLink { Label = v9, Url = /api/elasticsearch }, Version = 8 }
			v8 landing: OlderMajorNotice { Latest = LatestVersionLink { Label = v9, Url = /api/elasticsearch }, Version = 8 }
			next: UnreleasedNotice { Latest = LatestVersionLink { Label = v9, Url = /api/elasticsearch/operation/operation-ping } }
			next only in main: UnreleasedNotice { Latest = LatestVersionLink { Label = v9, Url = /api/elasticsearch } }
			next with main and 9: UnreleasedNotice { Latest = LatestVersionLink { Label = v9, Url = /api/elasticsearch/operation/operation-ping } }
			single version: none
			no moniker: none
			"""
		);
	}

	[Test]
	public void For_NewestMajorDidNotRender_KeepsTheVersionAndDropsTheLink()
	{
		string[] declared = ["main", "9", "8"];
		var notices = string.Join(
			"\n",
			$"v9 failed, v8: {Notice(["main", "8"], "8", "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8/operation/operation-ping", declared)}",
			$"v9 failed, next: {Notice(["main", "8"], "main", "elasticsearch/next", ApiPageType.Unreleased, "/api/elasticsearch/next/operation/operation-ping", declared)}",
			$"only v8 rendered: {Notice(["8"], "8", "elasticsearch/v8", ApiPageType.OlderMajor, "/api/elasticsearch/v8/operation/operation-ping", declared)}",
			$"no released major: {Notice(["main", "beta"], "beta", "elasticsearch/vbeta", ApiPageType.OlderMajor, "/api/elasticsearch/vbeta/operation/operation-ping")}"
		);

		notices.Should().MatchInlineSnapshot(
			"""
			v9 failed, v8: OlderMajorNotice { Latest = , Version = 8 }
			v9 failed, next: UnreleasedNotice { Latest =  }
			only v8 rendered: OlderMajorNotice { Latest = , Version = 8 }
			no released major: OlderMajorNotice { Latest = , Version = beta }
			"""
		);
	}
}
