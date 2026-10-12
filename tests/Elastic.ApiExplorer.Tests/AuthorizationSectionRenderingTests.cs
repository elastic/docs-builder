// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Microsoft.AspNetCore.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class AuthorizationSectionRenderingTests
{
	private static AuthScheme Scheme(string id, string label, string? example = null, string? description = null) =>
		new(id, label, example, description);

	[Test]
	public async Task Render_SingleSchemeWithNothingToShow_NamesTheScheme()
	{
		var html = await Render(new AuthRequirement([Scheme("oauth", "OAuth 2.0")]));

		html.Should().Contain("Uses OAuth 2.0 authentication.");
		html.Should().NotContain("api-tabs");
	}

	[Test]
	public async Task Render_SingleSchemeWithAnExample_ShowsItWithoutTheFallbackOrTabs()
	{
		var html = await Render(new AuthRequirement([Scheme("basic", "Basic", "Authorization: Basic <credentials>")]));

		html.Should().Contain("Authorization: Basic &lt;credentials&gt;");
		html.Should().NotContain("Uses Basic authentication.");
		html.Should().NotContain("api-tabs");
	}

	[Test]
	public async Task Render_SingleSchemeWithOnlyADescription_ShowsTheDescriptionNotTheFallback()
	{
		var html = await Render(new AuthRequirement([Scheme("oauth", "OAuth 2.0", description: "Get a token first.")]));

		html.Should().Contain("Get a token first.");
		html.Should().NotContain("Uses OAuth 2.0 authentication.");
	}

	[Test]
	public async Task Render_RequirementWithTwoSchemes_SharesOneTabAndSaysToSendBoth()
	{
		var html = await Render(
			new AuthRequirement([
				Scheme("basic", "Basic", "Authorization: Basic <credentials>"),
				Scheme("key", "Api key", "X-Key: <value>")
			]),
			new AuthRequirement([Scheme("bearer", "Bearer", "Authorization: Bearer <token>")])
		);

		html.Should().Contain("<label class=\"tabs-label\" for=\"authentication-tab-0\">Basic &#x2B; Api key</label>");
		html.Should().Contain("<label class=\"tabs-label\" for=\"authentication-tab-1\">Bearer</label>");
		html.Should().Contain("Send all of these together.");
		html.Should().Contain("<h3>Basic</h3>").And.Contain("<h3>Api key</h3>");
	}

	[Test]
	public async Task Render_OneRequirementWithTwoSchemes_HasNoTabsAndShowsBothSchemes()
	{
		var html = await Render(new AuthRequirement([
			Scheme("basic", "Basic", "Authorization: Basic <credentials>"),
			Scheme("key", "Api key", "X-Key: <value>")
		]));

		html.Should().NotContain("api-tabs");
		html.Should().NotContain("tabs-label");
		html.Should().Contain("Send all of these together.");
		html.Should().Contain("<h3>Basic</h3>").And.Contain("<h3>Api key</h3>");
		html.Should().Contain("Authorization: Basic &lt;credentials&gt;").And.Contain("X-Key: &lt;value&gt;");
	}

	private static async Task<string> Render(params AuthRequirement[] requirements) =>
		await _AuthorizationSection.Create(
			new AuthorizationSectionModel(requirements, [], static markdown => new HtmlString($"<p>{markdown}</p>"))
		).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
}
