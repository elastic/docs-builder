// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Microsoft.AspNetCore.Html;
using Nullean.Snapshots.Html;
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

		html.Should().MatchHtmlSnapshot();
		html.Should().Contain("Send all of these together.");
	}

	[Test]
	public async Task Render_OneRequirementWithTwoSchemes_HasNoTabsAndShowsBothSchemes()
	{
		var html = await Render(new AuthRequirement([
			Scheme("basic", "Basic", "Authorization: Basic <credentials>"),
			Scheme("key", "Api key", "X-Key: <value>")
		]));

		html.Should().MatchHtmlSnapshot();
		html.Should().NotContain("api-tabs", "a single requirement needs no tabs");
	}

	[Test]
	public async Task Render_ClusterAndIndexPrivileges_ShowAsLabelledRowsWithoutTheRepeatedWord()
	{
		var html = await RenderPrivileges(
			new PrivilegeRow("Cluster privileges", ["monitor"]),
			new PrivilegeRow("Index privileges", ["read", "view_index_metadata"])
		);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_PrivilegeAlternatives_AreJoinedWithOrAndNeedNoLabel()
	{
		var html = await RenderPrivileges(new PrivilegeRow("Privileges", ["read", "manage"], "or"));

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_RowWithoutPrivilegeNames_ShowsItsTextAsANote()
	{
		var html = await RenderPrivileges(new PrivilegeRow("Needs the `manage` role on the space", []));

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_StripWithPrivileges_ShowsThemAsItsFirstCellBesideTheSectionCells()
	{
		var authorization = new AuthorizationSectionModel(
			[],
			[new PrivilegeRow("Index privileges", ["read", "view_index_metadata"]), new PrivilegeRow("Cluster privileges", ["monitor"])],
			static markdown => new HtmlString($"<p>{markdown}</p>")
		);
		var links = new[]
		{
			new SectionLink("Query", "query-params", new SectionStat("5", "parameters")),
			new SectionLink("Responses", "responses", new SectionStat("200", "application/json"))
		};

		var html = await _SectionLinks.Create(new SectionStripModel(links, authorization)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select(".api-section-strip"));
	}

	private static async Task<string> RenderPrivileges(params PrivilegeRow[] rows) =>
		await _PrivilegesCard.Create(
			new AuthorizationSectionModel([], rows, static markdown => new HtmlString($"<p>{markdown}</p>"))
		).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

	private static async Task<string> Render(params AuthRequirement[] requirements) =>
		await _AuthorizationSection.Create(
			new AuthorizationSectionModel(requirements, [], static markdown => new HtmlString($"<p>{markdown}</p>"))
		).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
}
