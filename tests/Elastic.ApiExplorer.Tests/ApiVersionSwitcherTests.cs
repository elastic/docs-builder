// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionSwitcherTests
{
	[Test]
	public void Build_SingleVersion_ReturnsEmpty()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main"], "main");

		items.Should().BeEmpty();
	}

	[Test]
	public void Build_MultipleVersions_OrdersCurrentMajorFirstAndMarksCurrent()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "8");

		items.Should().HaveCount(3);
		items.Select(i => i.Label).Should().Equal("main", "v9", "v8");
		items.Select(i => i.Url).Should().Equal("/api/doc/elasticsearch", "/api/doc/elasticsearch/v9", "/api/doc/elasticsearch/v8");
		items.Single(i => i.Selected).Label.Should().Be("v8");
	}

	[Test]
	public void Build_CurrentMain_LabelsItLatest()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "main");

		items.Select(i => i.Label).Should().Equal("main", "v9", "v8");
		items.Single(i => i.Selected).Label.Should().Be("main");
	}

	private static IReadOnlyDictionary<string, IReadOnlyList<string>> Pages(params string[][] pages) =>
		pages.SelectMany(names => names.Select(name => (name, names))).ToDictionary(x => x.name, IReadOnlyList<string> (x) => x.names);

	private static IReadOnlyList<ApiVersionSwitcherItem> Items(string current) =>
		ApiVersionSwitcher.Build(
			"",
			"elasticsearch",
			["main", "9", "8"],
			current,
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
		var items = ApiVersionSwitcher.ForPage(Items("main"), "/api/doc/elasticsearch/operation/operation-ping/");

		items
			.Select(i => i.Url)
			.Should()
			.Equal(
				"/api/doc/elasticsearch/operation/operation-ping",
				"/api/doc/elasticsearch/v9/operation/operation-ping",
				"/api/doc/elasticsearch/v8"
			);
	}

	[Test]
	public void ForPage_PageMissingInOtherVersions_LinksToVersionRoots()
	{
		var items = ApiVersionSwitcher.ForPage(Items("main"), "/api/doc/elasticsearch/operation/operation-newop");

		items
			.Select(i => i.Url)
			.Should()
			.Equal("/api/doc/elasticsearch/operation/operation-newop", "/api/doc/elasticsearch/v9", "/api/doc/elasticsearch/v8");
	}

	[Test]
	public void ForPage_FromOlderVersion_LinksBackToSamePageInMain()
	{
		var items = ApiVersionSwitcher.ForPage(Items("9"), "/api/doc/elasticsearch/v9/operation/operation-ping");

		items
			.Select(i => i.Url)
			.Should()
			.Equal(
				"/api/doc/elasticsearch/operation/operation-ping",
				"/api/doc/elasticsearch/v9/operation/operation-ping",
				"/api/doc/elasticsearch/v8"
			);
		items.Single(i => i.Selected).Label.Should().Be("v9");
	}

	[Test]
	public void ForPage_LandingPage_LinksToEachRoot()
	{
		var items = ApiVersionSwitcher.ForPage(Items("main"), "/api/doc/elasticsearch/");

		items.Select(i => i.Url).Should().Equal("/api/doc/elasticsearch", "/api/doc/elasticsearch/v9", "/api/doc/elasticsearch/v8");
	}

	[Test]
	public void ForPage_CollapsedPageInSource_FindsTheOperationUnderItsFormerPath()
	{
		var items = ApiVersionSwitcher.ForPage(
			ApiVersionSwitcher.Build(
				"",
				"elasticsearch",
				["main", "8"],
				"main",
				new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
				{
					["main"] = Pages(
						["",],
						["/operation/operation-search", "/operation/operation-search-1", "/operation/operation-search-2"]
					),
					["8"] = Pages([""], ["/operation/operation-search-2"])
				}
			),
			"/api/doc/elasticsearch/operation/operation-search"
		);

		items
			.Select(i => i.Url)
			.Should()
			.Equal("/api/doc/elasticsearch/operation/operation-search", "/api/doc/elasticsearch/v8/operation/operation-search-2");
	}

	[Test]
	public void ForPage_CollapsedPageInTarget_LinksToItsCanonicalPath()
	{
		var items = ApiVersionSwitcher.ForPage(
			ApiVersionSwitcher.Build(
				"",
				"elasticsearch",
				["main", "8"],
				"8",
				new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
				{
					["main"] = Pages(
						[""],
						["/operation/operation-search", "/operation/operation-search-1", "/operation/operation-search-2"]
					),
					["8"] = Pages([""], ["/operation/operation-search-2"])
				}
			),
			"/api/doc/elasticsearch/v8/operation/operation-search-2"
		);

		items
			.Select(i => i.Url)
			.Should()
			.Equal("/api/doc/elasticsearch/operation/operation-search", "/api/doc/elasticsearch/v8/operation/operation-search-2");
	}
}
