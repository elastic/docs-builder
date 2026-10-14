// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Query and header parameters are property rows: a <c>deepObject</c> map lists its values like a body map does.</summary>
[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ParameterRowsTests(ApiExplorerFixture fixture)
{
	private OperationNavigationItem DocsGet =>
		fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get");

	[Test]
	public void Create_ReferencedMapParameter_ListsItsValueRow()
	{
		var page = OperationPageModel.Create(DocsGet.Model, RenderContext(DocsGet));

		var tags = page.QueryParameters.Items.Single(p => p.Name == "tags");
		tags.AnchorId.Should().Be("query-tags");
		tags.Type.Text.Should().Be("map string to string · min properties: 1 · max properties: 64");
		tags
			.DescriptionMarkdown
			.Should()
			.Be("Only documents with these tags and values.", "the parameter's description wins over its schema's");
		tags.Children.Kind.Should().Be(ChildKind.Dictionary);
		tags.Children.Dictionary!.ValueType.Text.Should().Be("string · min length: 1 · max length: 128 · pattern: ^[a-z0-9][a-z0-9_-]*$");
	}

	[Test]
	public void Create_PrimitiveParameters_KeepTheirTypeAndValues()
	{
		var page = OperationPageModel.Create(DocsGet.Model, RenderContext(DocsGet));

		page.QueryParameterNames.Should().Equal("mode", "stored_states", "tags");
		var states = page.QueryParameters.Items.Single(p => p.Name == "stored_states");
		states.EnumValues.Should().Equal("open", "closed");
		states.Children.Kind.Should().Be(ChildKind.None);
	}

	[Test]
	public void Create_HeaderParameters_ListedApartAndIgnoredNamesLeftOut()
	{
		var page = OperationPageModel.Create(DocsGet.Model, RenderContext(DocsGet));

		page.HeaderParameterNames.Should().Equal(["X-Opaque-Id"], "OpenAPI ignores an Accept header parameter");
		page.HeaderParameters.Items[0].AnchorId.Should().Be("header-X-Opaque-Id");
		page.QueryParameterNames.Should().NotContain("X-Opaque-Id");
		page.ParameterSections.Select(s => s.Header.Anchor).Should().Equal("parameters", "query-params", "header-params");
	}

	[Test]
	public void Write_HeaderParameters_HaveTheirOwnSection()
	{
		var context = RenderContext(DocsGet);
		var markdown = OperationCommonMark.Write(DocsGet.Model, OperationPageModel.Create(DocsGet.Model, context), context);

		markdown.Should().Contain(
			"""
			## Header parameters

			- `X-Opaque-Id` (string) — optional
			  Traces the request in logs and tasks.
			"""
		);
		markdown.Should().NotContain("`Accept`");
	}

	[Test]
	public async Task Render_HeaderParameters_ShowSectionAndTocEntry()
	{
		var html = await RenderAsync(DocsGet);

		html.Should().Contain("id=\"header-params-list\"");
		html.Should().Contain("<dt id=\"header-X-Opaque-Id\">");
		html.Should().Contain("href=\"#header-params\"");
	}

	[Test]
	public void Write_RequiredParameter_SaysSo()
	{
		var page = OperationPageModel.Create(DocsGet.Model, RenderContext(DocsGet));
		var required = page.QueryParameters with { Items = [page.QueryParameters.Items[0] with { IsRequired = true }] };
		var markdown = new System.Text.StringBuilder();

		ApiPropertyMarkdown.WriteList(markdown, required, "/api/doc/fixture");

		page
			.ParameterSections
			.SelectMany(s => s.Parameters.Items)
			.Should()
			.OnlyContain(p => p.IsRequest, "parameters are part of the request");
		markdown.ToString().Should().StartWith("- `mode` (enum) — required");
	}

	[Test]
	public async Task BuildParameterList_PathParameterSomeRoutesLeaveOut_ReadsOptionalWithTheRoute()
	{
		var document = await TestSpecs.LoadSpecAsync("""{ "openapi": "3.0.3", "info": { "title": "t", "version": "1" }, "paths": {} }""");
		var parameters = new[]
		{
			new ParameterRowInput(
				new OpenApiParameter
				{
					Name = "index",
					In = ParameterLocation.Path,
					Required = true,
					Schema = new OpenApiSchema { Type = JsonSchemaType.String }
				},
				"Where to look.",
				RouteWithout: "/_search"
			),
			new ParameterRowInput(
				new OpenApiParameter
				{
					Name = "id",
					In = ParameterLocation.Path,
					Required = true,
					Schema = new OpenApiSchema { Type = JsonSchemaType.String }
				},
				"Which one."
			)
		};

		var list = TestSpecs.BuilderFor(document).BuildParameterList(
			parameters,
			new PropertyTreeScope { Prefix = "path", IsRequest = true }
		);

		list
			.Items
			.Select(p => (p.AnchorId, p.IsRequired, p.OptionalRoute))
			.Should()
			.Equal(("path-index", false, "/_search"), ("path-id", true, null));
		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
		html.Should().Contain("<span class=\"optional type-status\">optional</span>");
		html.Should().Contain("<p class=\"api-path-param-optional-hint\">Leave it out to use <code>/_search</code>.</p>");
		System
			.Text
			.RegularExpressions
			.Regex
			.Matches(html, "required type-status")
			.Should()
			.ContainSingle("only the parameter every route needs reads required");
		var markdown = new System.Text.StringBuilder();
		ApiPropertyMarkdown.WriteList(markdown, list, "/api/doc/fixture");
		markdown
			.ToString()
			.Should()
			.Contain("- `index` (string) — optional, leave it out to use `/_search`")
			.And
			.Contain("- `id` (string) — required");
	}

	[Test]
	public async Task Render_PathParameters_KeepTheirSectionIdsAndAnchors()
	{
		var html = await RenderAsync(DocsGet);

		html.Should().Contain("id=\"parameters-list\"");
		html.Should().Contain("<dt id=\"path-index\">");
		html.Should().Contain("href=\"#parameters\">Path parameters</a>");
		html.Should().NotContain("class=\"path-param", "path parameters are property rows now");
	}

	[Test]
	public void Write_PathParameters_SitBeforeTheDescriptionOneLevelDeeper()
	{
		var context = RenderContext(DocsGet);
		var markdown = OperationCommonMark.Write(DocsGet.Model, OperationPageModel.Create(DocsGet.Model, context), context);

		markdown.Should().MatchRegex("#### Path parameters\\s+- `index` \\(.*\\) — required");
		markdown
			.IndexOf("#### Path parameters", StringComparison.Ordinal)
			.Should()
			.BeLessThan(markdown.IndexOf("## Query string parameters", StringComparison.Ordinal));
	}

	[Test]
	public void TocItems_PathParameters_AreLeftOut()
	{
		var context = RenderContext(DocsGet);
		var viewModel = new OperationViewModel(context)
		{
			Operation = DocsGet.Model,
			Page = OperationPageModel.Create(DocsGet.Model, context)
		};

		viewModel.Page.PathParameterNames.Should().Equal("index", "id");
		viewModel.TocItems.Select(t => t.Slug).Should().Contain("query-params").And.NotContain("parameters");
	}

	[Test]
	public void Write_ReferencedMapParameter_ListsMapValues()
	{
		var context = RenderContext(DocsGet);
		var markdown = OperationCommonMark.Write(DocsGet.Model, OperationPageModel.Create(DocsGet.Model, context), context);

		markdown.Should().Contain(
			"""
			- `tags` (map string to string · min properties: 1 · max properties: 64) — optional
			  Only documents with these tags and values.
			  Map values (string · min length: 1 · max length: 128 · pattern: ^[a-z0-9][a-z0-9_-]*$)
			"""
		);
	}

	[Test]
	public async Task Render_ReferencedMapParameter_ShowsKeyRowInQuerySection()
	{
		var html = await RenderAsync(DocsGet);

		var start = html.IndexOf("id=\"query-params-list\"", StringComparison.Ordinal);
		var end = html.IndexOf("id=\"responses\"", start, StringComparison.Ordinal);
		var section = html[start..end];
		section.Should().Contain("<dt id=\"query-tags\">");
		section.Should().Contain("<dt id=\"query-tags-string\">");
		section.Should().Contain("&lt;string&gt;");
	}

	private ApiRenderContext RenderContext(INavigationItem current) =>
		new(fixture.Context, fixture.Document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};

	private async Task<string> RenderAsync(OperationNavigationItem nav)
	{
		var fs = new MockFileSystem();
		await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await nav.Model.RenderAsync(stream, RenderContext(nav), null, TestContext.Current!.Execution.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}
}
