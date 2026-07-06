// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Schemas;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Loads the fixture spec and builds its navigation tree once for all snapshot tests.
/// </summary>
public sealed class ApiExplorerFixture : IAsyncLifetime
{
	public BuildContext Context { get; private set; } = null!;
	public OpenApiDocument Document { get; private set; } = null!;
	public LandingNavigationItem Navigation { get; private set; } = null!;

	public async ValueTask InitializeAsync()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		// RealGitRootForPath(null) rather than RealRead: it adds the main repo's .git dir as a scope
		// root when the checkout is a git worktree, which BuildContext needs to read git information.
		Context = new BuildContext(new DiagnosticsCollector([]), FileSystemFactory.RealGitRootForPath(null), configurationContext);

		var fs = new FileSystem();
		var path = fs.Path.Combine(AppContext.BaseDirectory, "TestData", "api-explorer-fixture.json");
		Document = await OpenApiReader.Create(fs.FileInfo.New(path))
			?? throw new InvalidOperationException($"Could not read fixture spec at {path}");

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, Context, PassthroughMarkdownRenderer.Instance);
		Navigation = generator.CreateNavigation("fixture", Document);
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	public IEnumerable<INavigationItem> Walk() => Walk(Navigation);

	private static IEnumerable<INavigationItem> Walk(INavigationItem item)
	{
		yield return item;
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			yield break;
		foreach (var child in node.NavigationItems)
		{
			foreach (var descendant in Walk(child))
				yield return descendant;
		}
	}
}

/// <summary>
/// Characterization tests: render representative pages from the fixture spec and compare against
/// checked-in reference HTML. These lock in current rendering behaviour so the view/model refactor
/// can prove it is presentation-neutral. See <see cref="HtmlSnapshot.MatchesReference"/> for updating.
/// </summary>
public class HtmlSnapshotTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	private const string OperationSection = "elastic-api-v3";
	private const string LandingSection = "elastic-docs-v3";
	private const string SchemaSection = "schema-definition";

	private Task<string> RenderAsync(IApiModel model, INavigationItem current) =>
		HtmlSnapshot.RenderPageAsync(model, current, fixture.Context, fixture.Document, TestContext.Current.CancellationToken);

	private OperationNavigationItem Operation(string operationId) =>
		fixture.Walk().OfType<OperationNavigationItem>().First(o => o.Model.Operation.OperationId == operationId);

	[Fact]
	public async Task RenderAsync_Landing_MatchesReference()
	{
		var html = await RenderAsync(fixture.Navigation.Index.Model, fixture.Navigation);
		HtmlSnapshot.MatchesReference("landing", HtmlSnapshot.ExtractSection(html, LandingSection));
	}

	[Fact]
	public async Task RenderAsync_TagLanding_MatchesReference()
	{
		var tag = fixture.Walk().OfType<TagNavigationItem>().First(t => t.NavigationTitle == "Search APIs");
		var html = await RenderAsync(tag.Index.Model, tag);
		HtmlSnapshot.MatchesReference("tag-search", HtmlSnapshot.ExtractSection(html, OperationSection));
	}

	[Fact]
	public async Task RenderAsync_RichOperation_MatchesReference()
	{
		var operation = Operation("search");
		var html = await RenderAsync(operation.Model, operation);
		HtmlSnapshot.MatchesReference("operation-search", HtmlSnapshot.ExtractSection(html, OperationSection));
	}

	[Fact]
	public async Task RenderAsync_EndpointOverloadOperation_MatchesReference()
	{
		var operation = Operation("docs-get");
		operation.Parent.Should().BeOfType<EndpointNavigationItem>();
		var html = await RenderAsync(operation.Model, operation);
		HtmlSnapshot.MatchesReference("operation-docs-get", HtmlSnapshot.ExtractSection(html, OperationSection));
	}

	[Fact]
	public async Task RenderAsync_OperationWithoutOperationId_MatchesReference()
	{
		var operation = fixture.Walk().OfType<OperationNavigationItem>().First(o => o.Model.Route == "/_fixture/cleanup");
		operation.Model.Operation.OperationId.Should().BeNull();
		var html = await RenderAsync(operation.Model, operation);
		HtmlSnapshot.MatchesReference("operation-cleanup", HtmlSnapshot.ExtractSection(html, OperationSection));
	}

	[Fact]
	public async Task RenderAsync_OperationWithoutAnyName_MatchesReference()
	{
		var operation = fixture.Walk().OfType<OperationNavigationItem>().First(o => o.Model.Route == "/_fixture/anonymous");
		var html = await RenderAsync(operation.Model, operation);
		HtmlSnapshot.MatchesReference("operation-anonymous", HtmlSnapshot.ExtractSection(html, OperationSection));
	}

	[Fact]
	public async Task RenderAsync_RecursiveSchemaPage_MatchesReference()
	{
		var schema = fixture.Walk().OfType<SchemaNavigationItem>().First(s => s.Model.SchemaId == "_types.query_dsl.QueryContainer");
		var html = await RenderAsync(schema.Model, schema);
		HtmlSnapshot.MatchesReference("schema-querycontainer", HtmlSnapshot.ExtractSection(html, SchemaSection));
	}

	[Fact]
	public async Task RenderAsync_UnionSchemaPage_MatchesReference()
	{
		var schema = fixture.Walk().OfType<SchemaNavigationItem>().First(s => s.Model.SchemaId == "_types.aggregations.Aggregate");
		var html = await RenderAsync(schema.Model, schema);
		HtmlSnapshot.MatchesReference("schema-aggregate", HtmlSnapshot.ExtractSection(html, SchemaSection));
	}
}
