// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class EndpointOrderTests
{
	[Test]
	public async Task CreateNavigation_OperationsInSpecOrder_ListsThemByRouteThenLifecycleMethod()
	{
		var spec = Spec(
			("delete", "/b", "Delete b"),
			("options", "/a", "Options a"),
			("get", "/a/{id}", "Get one a"),
			("post", "/a", "Create a"),
			("get", "/a", "List a"),
			("patch", "/a", "Patch a"),
			("head", "/a", "Head a"),
			("put", "/a", "Replace a")
		);

		var listed = await ListTagAsync(spec);

		listed.Should().MatchInlineSnapshot(
			"""
			GET /a List a
			POST /a Create a
			PUT /a Replace a
			PATCH /a Patch a
			HEAD /a Head a
			OPTIONS /a Options a
			GET /a/{id} Get one a
			DELETE /b Delete b
			"""
		);
	}

	[Test]
	public async Task CreateNavigation_CollapsedPage_IsPlacedByTheRouteAndMethodItLeadsWith()
	{
		var spec = Spec(
			("get", "/c", "List c"),
			("post", "/c/{id}", "Create c"),
			("post", "/c", "Create c"),
			("delete", "/c/{id}", "Delete c"),
			("get", "/b", "List b")
		);

		var listed = await ListTagAsync(spec);

		listed.Should().MatchInlineSnapshot(
			"""
			GET /b List b
			GET /c List c
			POST /c/{id} Create c
			DELETE /c/{id} Delete c
			"""
		);
	}

	private static async Task<string> ListTagAsync(string spec)
	{
		var (generator, document) = await TestSpecs.CreateGeneratorWithSpecAsync(spec);
		var navigation = generator.CreateNavigation("test", document);
		var rows = navigation
			.NavigationItems
			.OfType<TagNavigationItem>()
			.Single()
			.NavigationItems
			.OfType<OperationNavigationItem>()
			.Select(
				static item => $"{item.Endpoint.Rows[0].Method.ToUpperInvariant()} {item.Endpoint.Rows[0].Route} {item.NavigationTitle}"
			);
		return string.Join('\n', rows);
	}

	private static string Spec(params (string Method, string Route, string Summary)[] operations) =>
		new MinimalSpec([
			.. operations.Select(o => new SpecOperation(o.Route, $"{o.Method}-{o.Route}", ["things"], o.Method, o.Summary))
		]).ToJson();
}
