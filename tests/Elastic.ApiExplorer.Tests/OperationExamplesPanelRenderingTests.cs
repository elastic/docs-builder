// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class OperationExamplesPanelRenderingTests
{
	private static Cancel Ct => TestContext.Current!.Execution.CancellationToken;

	private static CodeSample Sample(string language, string source = "sample") =>
		new(language, source, CodeSample.GetHighlightClass(language));

	private static readonly ExampleScenario TermSearch = new()
	{
		Title = "A simple term search",
		TabId = "term",
		CodeSamples = [Sample("Console", "GET /_search"), Sample("curl"), Sample("Python"), Sample("Java", "client.search(s -> s)")],
		Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
	};

	private static readonly ExampleScenario Slicing = new()
	{
		Title = "Search slicing",
		TabId = "slicing",
		CodeSamples = [Sample("Console") with { Generated = true }, Sample("curl") with { Generated = true }]
	};

	private static ValueTask<string> RenderPanel(params ExampleScenario[] scenarios) =>
		_OperationExamplesPanel.Create(new OperationExamplesPanelModel { Scenarios = scenarios }).RenderAsync(cancellationToken: Ct);

	private static ValueTask<string> RenderScenario(ExampleScenario scenario, params ExampleScenario[] others) =>
		_ExampleScenarioContent.Create(new ExampleScenarioView(
			scenario,
			new OperationExamplesPanelModel { Scenarios = [scenario, .. others] }
		)).RenderAsync(cancellationToken: Ct);

	[Test]
	public async Task Render_MultipleScenarios_ShowsChipsAndKeepsOthersFindable()
	{
		var html = await RenderPanel(TermSearch, Slicing);

		html.Should().Contain("api-example-chips");
		html.Should().Contain("data-scenario=\"term\"");
		html.Should().Contain(">Search slicing</span>");
		// The chip shows just the number; the words are a tooltip and screen-reader text.
		html.Should().Contain("title=\"4 languages\"");
		html.Should().Contain("<span class=\"sr-only\"> languages</span>");
		html.Should().Contain("hidden=\"until-found\"");
		html.Should().NotContain("api-select");
	}

	[Test]
	public async Task Render_SingleScenario_OmitsChips()
	{
		var html = await RenderPanel(TermSearch);

		html.Should().Contain("id=\"api-examples-panel\"");
		html.Should().NotContain("api-example-chips");
	}

	[Test]
	public async Task Render_Carousel_RendersEveryLanguageWithoutHidingIt()
	{
		var html = await RenderScenario(TermSearch);

		html.Should().Contain("data-api-carousel");
		html.Should().Contain("client.search(s -&gt; s)");
		foreach (var language in new[] { "Console", "curl", "Python", "Java" })
			html.Should().Contain($"data-code-card data-lang=\"{language}\"");
		html.Should().NotContain("hidden=\"hidden\"");
		html
			.IndexOf("data-lang=\"Console\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("data-lang=\"Java\"", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_CardHeader_ShowsLanguageAndClient_NotThePath()
	{
		var html = await RenderScenario(TermSearch with { HttpMethod = "post", Route = "/_search" });

		html.Should().Contain("api-code-carousel-card-client\">elasticsearch-java</span>");
		html.Should().Contain("api-code-carousel-card-client\">Kibana Dev Tools</span>");
		html.Should().NotContain("api-method-post");
	}

	[Test]
	public async Task Render_MissingLanguages_PointAtTheExampleThatHasThem()
	{
		var html = await RenderScenario(Slicing, TermSearch);

		html.Should().Contain("data-carousel-jump=\"term\" data-lang=\"Python\"");
		html.Should().Contain("Show Python for &ldquo;A simple term search&rdquo;");
		html.Should().Contain("api-code-carousel-dot is-missing");
	}

	[Test]
	public async Task Render_GeneratedSample_IsMarkedInTheMarkupButNotInTheCard()
	{
		var html = await RenderScenario(Slicing);

		html.Should().Contain("data-generated");
		html.Should().Contain("Built from this example&rsquo;s request body");
		html.Should().NotContain("<footer");
	}

	[Test]
	public async Task Render_Carousel_HasAnExpandButtonThatStartsHidden()
	{
		var html = await RenderScenario(TermSearch);

		// Scripts reveal it only when the active sample is longer than the line cap.
		html.Should().MatchRegex("data-request-expand[^>]*\\shidden>");
	}

	[Test]
	public async Task Render_Description_IsANoteWithAToggleForLongText()
	{
		var html = await RenderScenario(TermSearch with
		{
			DescriptionHtml = new Microsoft.AspNetCore.Html.HtmlString("<p>Splits the search into slices.</p>")
		});

		html.Should().Contain("data-example-description");
		html.Should().Contain("Splits the search into slices.");
		html.Should().MatchRegex("data-description-toggle[^>]*\\shidden>");
	}

	[Test]
	public async Task Render_Panel_LabelsTheChipsWithAnExamplesHeading()
	{
		var html = await RenderPanel(TermSearch, Slicing);

		html.Should().Contain("id=\"api-examples-heading\">Examples</p>");
		html.Should().Contain("aria-labelledby=\"api-examples-heading\"");
		html
			.IndexOf("api-examples-heading\">", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("api-example-chips", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_RequestAndResponse_ShareTheCodeCardClass()
	{
		var request = await _ApiCodeSample.Create(new ApiCodeSampleModel(Sample("JSON", "{}"))).RenderAsync(cancellationToken: Ct);
		var response = await RenderScenario(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
		});

		request.Should().Contain("api-code-card");
		response.Should().Contain("api-code-card");
		response.Should().Contain("example-block--response");
	}

	[Test]
	public async Task Render_ResponseCard_FollowsSharedCodeCardContract()
	{
		var html = await RenderScenario(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			Responses =
			[
				new ExampleResponse { StatusCode = "200", JsonValue = "{}" },
				new ExampleResponse { StatusCode = "404", JsonValue = "{}" }
			]
		});

		html.Should().Contain("data-code-card");
		html.Should().Contain("data-code-actions");
		html.Should().Contain("data-code-panel=\"200\"");
		html.Should().Contain("data-code-panel=\"404\"");
	}

	[Test]
	public async Task Render_RequestBody_WithoutSamples_ShowsMethodChipAndRoute()
	{
		var html = await RenderScenario(new ExampleScenario
		{
			Title = "Create",
			TabId = "create",
			HttpMethod = "post",
			Route = "/_search",
			RequestJson = /*lang=json,strict*/  """{"query":{}}"""
		});

		html.Should().Contain("api-method-post");
		html.Should().Contain("class=\"api-url\">/_search</span>");
		html.Should().NotContain("data-api-carousel");
	}

	[Test]
	public async Task Render_SamplesWithoutTheBody_AlsoRenderTheRequestBodyCard()
	{
		var html = await RenderScenario(new ExampleScenario
		{
			Title = "createAgentRequestExample",
			TabId = "create",
			RequestJson = /*lang=json,strict*/  """{"id":"created-agent-id"}""",
			CodeSamples = [Sample("curl", "curl -X POST")]
		});

		html.Should().Contain(">Request example</span>");
		html
			.IndexOf("created-agent-id", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(html.IndexOf("curl -X POST", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_SamplesEmbeddingTheBody_OmitTheRequestBodyCard()
	{
		var html = await RenderScenario(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			RequestJson = /*lang=json,strict*/  """{"query":{}}""",
			CodeSamples = [Sample("Console", "POST /_search\n{\"query\":{}}")],
			CodeSamplesIncludeRequest = true
		});

		html.Should().NotContain("api-code-sample-heading");
	}

	[Test]
	public async Task Render_LabelsTheRequestAndTheResponse()
	{
		var html = await RenderScenario(TermSearch);

		html.Should().Contain("<span class=\"api-rail-label\">Request</span>");
		html.Should().Contain("<span class=\"api-rail-label\">Response</span>");
	}
}
