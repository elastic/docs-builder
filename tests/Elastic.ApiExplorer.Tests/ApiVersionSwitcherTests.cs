// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionSwitcherTests
{
	private static readonly ApiVersionRoutes Es = TestHelpers.EsRoutes;

	[Test]
	public void Build_SingleVersion_ReturnsEmpty()
	{
		var items = ApiVersionSwitcher.Build("", ApiVersionRoutes.For("elasticsearch", ["main"]), ["main"], "elasticsearch");

		items.Should().BeEmpty();
	}

	[Test]
	public void Build_MultipleVersions_OrdersCurrentMajorFirstAndMarksCurrent()
	{
		var items = ApiVersionSwitcher.Build("", Es, ["main", "9", "8"], "elasticsearch/v8");

		items.Should().HaveCount(3);
		items.Select(i => i.Label).Should().Equal("main", "v9 (latest)", "v8");
		items.Select(i => i.Url).Should().Equal("/api/elasticsearch/next", "/api/elasticsearch", "/api/elasticsearch/v8");
		items.Single(i => i.Selected).Label.Should().Be("v8");
	}

	[Test]
	public void Build_CurrentMain_LinksMainAtNextAndMarksTheHighestMajorLatest()
	{
		var items = ApiVersionSwitcher.Build("", Es, ["main", "9", "8"], "elasticsearch/next");

		items.Select(i => i.Label).Should().Equal("main", "v9 (latest)", "v8");
		items.Single(i => i.Selected).Url.Should().Be("/api/elasticsearch/next");
	}

	[Test]
	public void Build_NumberedCopyOfTheDefaultMajor_SelectedEntryLinksToItsOwnRoot()
	{
		var items = ApiVersionSwitcher.Build("", Es, ["main", "9", "8"], "elasticsearch/v9");

		items
			.Select(i => $"{i.Url} {i.Selected}")
			.Should()
			.Equal("/api/elasticsearch/next False", "/api/elasticsearch/v9 True", "/api/elasticsearch/v8 False");
	}

	[Test]
	public void Build_NoNumericMajor_KeepsMainAtTheKey()
	{
		var items = ApiVersionSwitcher.Build(
			"",
			ApiVersionRoutes.For("elasticsearch", ["main", "beta"]),
			["main", "beta"],
			"elasticsearch"
		);

		items.Single(i => i.Selected).Url.Should().Be("/api/elasticsearch");
	}

	private static IReadOnlyDictionary<string, IReadOnlyList<string>> Pages(params string[][] pages) =>
		pages.SelectMany(names => names.Select(name => (name, names))).ToDictionary(x => x.name, IReadOnlyList<string> (x) => x.names);

	private static IReadOnlyList<ApiVersionSwitcherItem> Items(string currentSuffix) =>
		ApiVersionSwitcher.Build(
			"",
			Es,
			["main", "9", "8"],
			currentSuffix,
			new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
			{
				["main"] = Pages([""], ["/operation/operation-ping"], ["/operation/operation-newop"]),
				["9"] = Pages([""], ["/operation/operation-ping"]),
				["8"] = Pages([""])
			}
		);

	[Test]
	public void ForPage_PageExistsInOtherVersion_LinksToSamePage()
	{
		var items = ApiVersionSwitcher.ForPage(Items("elasticsearch/next"), "/api/elasticsearch/next/operation/operation-ping/");

		items
			.Select(i => i.Url)
			.Should()
			.Equal(
				"/api/elasticsearch/next/operation/operation-ping",
				"/api/elasticsearch/operation/operation-ping",
				"/api/elasticsearch/v8"
			);
	}

	[Test]
	public void ForPage_PageMissingInOtherVersions_LinksToVersionRoots()
	{
		var items = ApiVersionSwitcher.ForPage(Items("elasticsearch/next"), "/api/elasticsearch/next/operation/operation-newop");

		items
			.Select(i => i.Url)
			.Should()
			.Equal("/api/elasticsearch/next/operation/operation-newop", "/api/elasticsearch", "/api/elasticsearch/v8");
	}

	[Test]
	public void ForPage_FromTheVersionlessDefault_LinksToSamePageInMain()
	{
		var items = ApiVersionSwitcher.ForPage(Items("elasticsearch"), "/api/elasticsearch/operation/operation-ping");

		items
			.Select(i => i.Url)
			.Should()
			.Equal(
				"/api/elasticsearch/next/operation/operation-ping",
				"/api/elasticsearch/operation/operation-ping",
				"/api/elasticsearch/v8"
			);
		items.Single(i => i.Selected).Label.Should().Be("v9 (latest)");
	}

	[Test]
	public void ForPage_FromTheNumberedCopyOfTheDefault_StaysOnTheSamePage()
	{
		var items = ApiVersionSwitcher.ForPage(Items("elasticsearch/v9"), "/api/elasticsearch/v9/operation/operation-ping");

		items
			.Select(i => i.Url)
			.Should()
			.Equal(
				"/api/elasticsearch/next/operation/operation-ping",
				"/api/elasticsearch/v9/operation/operation-ping",
				"/api/elasticsearch/v8"
			);
	}

	[Test]
	public void ForPage_LandingPage_LinksToEachRoot()
	{
		var items = ApiVersionSwitcher.ForPage(Items("elasticsearch/next"), "/api/elasticsearch/next/");

		items.Select(i => i.Url).Should().Equal("/api/elasticsearch/next", "/api/elasticsearch", "/api/elasticsearch/v8");
	}

	[Test]
	public void ForPage_CollapsedPageInSource_FindsTheOperationUnderItsFormerPath()
	{
		var items = ApiVersionSwitcher.ForPage(
			ApiVersionSwitcher.Build(
				"",
				ApiVersionRoutes.For("elasticsearch", ["main", "8"]),
				["main", "8"],
				"elasticsearch/next",
				new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
				{
					["main"] = Pages(
						["",],
						["/operation/operation-search", "/operation/operation-search-1", "/operation/operation-search-2"]
					),
					["8"] = Pages([""], ["/operation/operation-search-2"])
				}
			),
			"/api/elasticsearch/next/operation/operation-search"
		);

		items
			.Select(i => i.Url)
			.Should()
			.Equal("/api/elasticsearch/next/operation/operation-search", "/api/elasticsearch/operation/operation-search-2");
	}

	[Test]
	public void ForPage_CollapsedPageInTarget_LinksToItsCanonicalPath()
	{
		var items = ApiVersionSwitcher.ForPage(
			ApiVersionSwitcher.Build(
				"",
				ApiVersionRoutes.For("elasticsearch", ["main", "8"]),
				["main", "8"],
				"elasticsearch/v8",
				new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
				{
					["main"] = Pages(
						[""],
						["/operation/operation-search", "/operation/operation-search-1", "/operation/operation-search-2"]
					),
					["8"] = Pages([""], ["/operation/operation-search-2"])
				}
			),
			"/api/elasticsearch/v8/operation/operation-search-2"
		);

		items
			.Select(i => i.Url)
			.Should()
			.Equal("/api/elasticsearch/next/operation/operation-search", "/api/elasticsearch/v8/operation/operation-search-2");
	}
}
