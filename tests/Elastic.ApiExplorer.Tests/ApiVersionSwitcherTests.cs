// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionSwitcherTests
{
	private static ApiVersionSwitcherContext CreateContext(
		IReadOnlyList<string> monikers,
		string currentMoniker,
		int currentMajor = 9,
		ApiCrossVersionPageIndex? crossVersionIndex = null) =>
		new(
			"elasticsearch",
			monikers,
			currentMoniker,
			currentMajor,
			crossVersionIndex ?? ApiCrossVersionPageIndex.Build([]),
			urlPathPrefix: "");

	[Fact]
	public void GetItems_SingleVersion_ReturnsEmpty()
	{
		var items = CreateContext(["main"], "main").GetItems(pageTarget: null);

		items.Should().BeEmpty();
	}

	[Fact]
	public void GetItems_MultipleVersions_OrdersLatestFirstAndMarksCurrent()
	{
		var items = CreateContext(["main", "9", "8"], "8").GetItems(pageTarget: null);

		items.Should().HaveCount(3);
		items.Select(i => i.NavigationTitle).Should().Equal("9.x (latest)", "9.x", "8.x");
		items.Select(i => i.Url).Should().Equal(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch/v9/",
			"/api/doc/elasticsearch/v8/");
		items.Single(i => i.IsActive).NavigationTitle.Should().Be("8.x");
	}

	[Fact]
	public void GetItems_OperationPage_LinksToSameOperationWhenAvailable()
	{
		var crossVersionIndex = ApiCrossVersionPageIndex.Build(
		[
			Versioned("main", OperationSpec("ping")),
			Versioned("8", OperationSpec("ping")),
			Versioned("9", new OpenApiDocument
			{
				Info = new OpenApiInfo { Title = "No ping", Version = "1.0" },
				Paths = new OpenApiPaths()
			})
		]);
		var pageTarget = new ApiPageVersionTarget(ApiPageVersionTargetKind.Operation, "operation-ping");

		var items = CreateContext(["main", "9", "8"], "main", crossVersionIndex: crossVersionIndex)
			.GetItems(pageTarget);

		items.Single(i => i.NavigationTitle == "9.x (latest)").Url
			.Should().Be("/api/doc/elasticsearch/operation/operation-ping");
		items.Single(i => i.NavigationTitle == "8.x").Url
			.Should().Be("/api/doc/elasticsearch/v8/operation/operation-ping");
		items.Single(i => i.NavigationTitle == "9.x").Url
			.Should().Be("/api/doc/elasticsearch/v9/");
	}

	[Fact]
	public void GetItems_OperationPage_FallsBackToLandingWhenMissing()
	{
		var crossVersionIndex = ApiCrossVersionPageIndex.Build(
		[
			Versioned("main", OperationSpec("ping")),
			Versioned("8", new OpenApiDocument
			{
				Info = new OpenApiInfo { Title = "No ping", Version = "1.0" },
				Paths = new OpenApiPaths()
			})
		]);
		var pageTarget = new ApiPageVersionTarget(ApiPageVersionTargetKind.Operation, "operation-ping");

		var items = CreateContext(["main", "8"], "main", crossVersionIndex: crossVersionIndex)
			.GetItems(pageTarget);

		items.Single(i => i.NavigationTitle == "8.x").Url.Should().Be("/api/doc/elasticsearch/v8/");
	}

	[Fact]
	public void GetItems_SamePageTarget_ReusesCachedItems()
	{
		var context = CreateContext(["main", "8"], "main");
		var pageTarget = new ApiPageVersionTarget(ApiPageVersionTargetKind.Operation, "operation-ping");

		var first = context.GetItems(pageTarget);
		var second = context.GetItems(pageTarget);

		ReferenceEquals(first, second).Should().BeTrue();
	}

	private static VersionedOpenApiDocument Versioned(string moniker, OpenApiDocument document) =>
		new(new ResolvedApiVersion { Moniker = moniker, Version = moniker, IsLocal = true }, document);

	private static OpenApiDocument OperationSpec(string operationId) => new()
	{
		Info = new OpenApiInfo { Title = "Spec", Version = "1.0" },
		Paths = new OpenApiPaths
		{
			["/ping"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Get] = new() { OperationId = operationId }
				}
			}
		}
	};
}
