// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests.Supplemental;

public class ApiSupplementalRenderTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	private const string SpecOperationDescription = "Returns hits that match the query defined in the request.";
	private const string SpecTagDescription = "Operations that run *queries* against fixture data.";
	private const string SpecIndexDescription = "A comma-separated list of index names to search.";
	private const string SpecQueryQDescription = "A query in the Lucene query string syntax.";
	private const string SpecFieldsDescription = "A field or list of fields to return, exercising the X | X[] simple-union path.";

	[Fact]
	public async Task Operation_NoHeadings_ReplacesDescription()
	{
		var nav = SearchOperation();
		var html = await RenderAsync(nav.Model, nav, operations: Doc("search", """
			SUPP_OP_NO_HEADINGS
			"""));

		html.Should().Contain("SUPP_OP_NO_HEADINGS");
		html.Should().NotContain(SpecOperationDescription);
	}

	[Fact]
	public async Task Operation_DescriptionHeading_ReplacesDescriptionAndKeepsGeneratedSections()
	{
		var nav = SearchOperation();
		var html = await RenderAsync(
			nav.Model,
			nav,
			operations: Doc(
				"search",
				"""
			## Description

			SUPP_OP_DESCRIPTION

			## Best practices

			SUPP_OP_BEST_PRACTICES
			"""
			)
		);

		html.Should().Contain("SUPP_OP_DESCRIPTION");
		html.Should().NotContain(SpecOperationDescription);
		html.Should().Contain("Query String Parameters");
		html.Should().Contain("id=\"responses\"");
		html.Should().Contain(SpecQueryQDescription);
	}

	[Fact]
	public async Task Operation_EmptySpecDescription_ShowsSupplemental()
	{
		var src = SearchOperation();
		var apiOp = new ApiOperation(
			src.Model.OperationType,
			new OpenApiOperation { OperationId = "lonely", Summary = "Lonely", Description = null },
			src.Model.Route,
			src.Model.Path,
			src.Model.ApiName
		);

		var html = await RenderAsync(apiOp, src, operations: Doc("lonely", "LONELY_OVERRIDE"));

		html.Should().Contain("LONELY_OVERRIDE");
		html.Should().Contain("id=\"description\"");
	}

	[Fact]
	public async Task Operation_NoMatchingFile_KeepsSpecDescription()
	{
		var nav = SearchOperation();
		var html = await RenderAsync(nav.Model, nav);

		html.Should().Contain(SpecOperationDescription);
	}

	[Fact]
	public async Task Operation_PostSections_RenderAfterGeneratedContent()
	{
		var nav = SearchOperation();
		var html = await RenderAsync(
			nav.Model,
			nav,
			operations: Doc(
				"search",
				"""
			## Description

			SUPP_OP_DESCRIPTION

			## Best practices

			SUPP_OP_BEST_PRACTICES

			## Common patterns

			SUPP_OP_COMMON_PATTERNS
			"""
			)
		);

		html.Should().Contain("id=\"best-practices\"");
		html.Should().Contain("SUPP_OP_BEST_PRACTICES");
		html.Should().Contain("id=\"common-patterns\"");
		html.Should().Contain("SUPP_OP_COMMON_PATTERNS");
		html
			.IndexOf("id=\"responses\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("id=\"best-practices\"", StringComparison.Ordinal));
		html
			.IndexOf("id=\"best-practices\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("id=\"common-patterns\"", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Operation_PostSections_HonorExplicitIdsAndSkipReservedAnchors()
	{
		var nav = SearchOperation();
		var html = await RenderAsync(
			nav.Model,
			nav,
			operations: Doc(
				"search",
				"""
			## Description

			SUPP_OP_DESCRIPTION

			## Responses

			SUPP_OP_RESPONSES_SECTION

			## Errors {#errors-guide}

			SUPP_OP_ERRORS

			## Best practices

			SUPP_OP_BEST_1

			## Best practices

			SUPP_OP_BEST_2
			"""
			)
		);

		html.Should().Contain("id=\"responses\"");
		html.Should().Contain("id=\"responses-2\"");
		html.Should().Contain("SUPP_OP_RESPONSES_SECTION");
		html.Should().Contain("id=\"errors-guide\"");
		html.Should().Contain(">Errors<");
		html.Should().NotContain("{#errors-guide}");
		html.Should().Contain("SUPP_OP_ERRORS");
		html.Should().Contain("id=\"best-practices\"");
		html.Should().Contain("id=\"best-practices-2\"");
		html.Should().Contain("SUPP_OP_BEST_1");
		html.Should().Contain("SUPP_OP_BEST_2");
	}

	[Fact]
	public void SplitHeading_ReadsExplicitId()
	{
		var (title, id) = ApiPostSection.SplitHeading("Errors {#errors-guide}");
		title.Should().Be("Errors");
		id.Should().Be("errors-guide");
	}

	[Fact]
	public void ResolveAnchor_ReservedId_GetsSuffix()
	{
		var used = ApiPostSection.OperationReservedAnchors.ToHashSet(StringComparer.Ordinal);
		ApiPostSection.ResolveAnchor("Responses", "responses", used).Should().Be("responses-2");
		ApiPostSection.ResolveAnchor("Responses", null, used).Should().Be("responses-3");
	}

	[Fact]
	public async Task Operation_ParameterOverrides_ReplaceListedPathAndQueryOnly()
	{
		var nav = SearchOperation();
		var html = await RenderAsync(
			nav.Model,
			nav,
			operations: Doc(
				"search",
				"""
			## Path parameters

			: `index`
			  SUPP_INDEX

			## Query parameters

			: `q`
			  SUPP_Q

			: `nope`
			  UNKNOWN_PARAM_OVERRIDE
			"""
			)
		);

		html.Should().Contain("SUPP_INDEX");
		html.Should().Contain("SUPP_Q");
		html.Should().NotContain(SpecIndexDescription);
		html.Should().NotContain(SpecQueryQDescription);
		html.Should().Contain("The type of index that wildcard patterns can match.");
		html.Should().NotContain("UNKNOWN_PARAM_OVERRIDE");
	}

	[Fact]
	public async Task Operation_RequestBodyOverride_ReplacesListedFieldOnly()
	{
		var nav = SearchOperation();
		var html = await RenderAsync(
			nav.Model,
			nav,
			operations: Doc("search", """
			## Request body

			: `query`
			  SUPP_QUERY_FIELD
			""")
		);

		html.Should().Contain("SUPP_QUERY_FIELD");
		html.Should().Contain(SpecFieldsDescription);
	}

	[Fact]
	public async Task Tag_NoHeadings_ReplacesDescription()
	{
		var nav = SearchTag();
		var html = await RenderAsync(nav.Index.Model, nav, tags: Doc("search", "SUPP_TAG_NO_HEADINGS"));

		html.Should().Contain("SUPP_TAG_NO_HEADINGS");
		html.Should().NotContain(SpecTagDescription);
	}

	[Fact]
	public async Task Tag_DescriptionHeading_ReplacesDescriptionAndKeepsOverview()
	{
		var nav = SearchTag();
		var html = await RenderAsync(
			nav.Index.Model,
			nav,
			tags: Doc(
				"search",
				"""
			## Description

			SUPP_TAG_DESCRIPTION

			## Getting started

			SUPP_TAG_GETTING_STARTED
			"""
			)
		);

		html.Should().Contain("SUPP_TAG_DESCRIPTION");
		html.Should().NotContain(SpecTagDescription);
		html.Should().Contain("api-overview");
		html.Should().Contain("id=\"getting-started\"");
		html.Should().Contain("SUPP_TAG_GETTING_STARTED");
		html
			.IndexOf("SUPP_TAG_DESCRIPTION", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("id=\"getting-started\"", StringComparison.Ordinal));
		html
			.IndexOf("id=\"getting-started\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("api-overview", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Tag_EmptySpecDescription_ShowsSupplemental()
	{
		var nav = SearchTag();
		var tag = nav.Index.Model with { Description = "" };
		var html = await RenderAsync(tag, nav, tags: Doc("search", "SUPP_TAG_EMPTY_SPEC"));

		html.Should().Contain("SUPP_TAG_EMPTY_SPEC");
	}

	[Fact]
	public async Task Tag_NoMatchingFile_KeepsSpecDescription()
	{
		var nav = SearchTag();
		var html = await RenderAsync(nav.Index.Model, nav);

		html.Should().Contain(SpecTagDescription);
	}

	[Fact]
	public void RequestBodyOverride_MatchesPropertyNameOnRequestTreeOnly()
	{
		var schema = fixture.Document.Components!.Schemas!["fixture.SearchRequestBody"];
		var options = new PropertyDisplayOptions { RenderMarkdown = s => new HtmlString($"<p>{s}</p>"), ApiRootUrl = "/api/doc/fixture" };
		var builder = new ApiPropertyTreeBuilder(fixture.Document, options);
		var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["query"] = "SUPP_QUERY_FIELD" };

		var request = builder.BuildPropertyList(
			schema,
			new PropertyTreeScope { Prefix = "req", IsRequest = true, DescriptionOverrides = overrides }
		);
		var response = builder.BuildPropertyList(
			schema,
			new PropertyTreeScope { Prefix = "res", IsRequest = false, DescriptionOverrides = overrides }
		);

		request.Should().NotBeNull();
		request.Items.Single(p => p.Name == "query").DescriptionHtml.Value.Should().Contain("SUPP_QUERY_FIELD");
		request.Items.Single(p => p.Name == "fields").DescriptionHtml.Value.Should().Contain(SpecFieldsDescription);
		response.Should().NotBeNull();
		response.Items.Single(p => p.Name == "query").DescriptionHtml.Value.Should().NotContain("SUPP_QUERY_FIELD");
	}

	private OperationNavigationItem SearchOperation() =>
		fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");

	private TagNavigationItem SearchTag() => fixture.Walk().OfType<TagNavigationItem>().First(t => t.Index.Model.Name == "search");

	private static Dictionary<string, ApiSupplementalDoc> Doc(string key, string raw) =>
		new(StringComparer.Ordinal) { [key] = ApiSupplementalDoc.Parse(raw)! };

	private async Task<string> RenderAsync(
		IApiModel model,
		INavigationItem navigation,
		IReadOnlyDictionary<string, ApiSupplementalDoc>? operations = null,
		IReadOnlyDictionary<string, ApiSupplementalDoc>? tags = null
	)
	{
		var renderContext = new ApiRenderContext(
			fixture.Context,
			fixture.Document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance,
			OperationSupplemental = operations ?? new Dictionary<string, ApiSupplementalDoc>(),
			TagSupplemental = tags ?? new Dictionary<string, ApiSupplementalDoc>()
		};

		var fs = new MockFileSystem();
		await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await model.RenderAsync(stream, renderContext, TestContext.Current.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}
}
