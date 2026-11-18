// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Structural;
using Elastic.Documentation;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>The Authentication section: the alternatives to authenticate with, on the Authentication page and on endpoints that set their own.</summary>
public record AuthenticationSectionModel(
	IReadOnlyList<AuthRequirement> Requirements,
	bool Optional,
	Func<string?, HtmlString> RenderMarkdown,
	bool AlwaysTabs = false
);

/// <summary>Required privileges and credentials for the Required privileges and Authentication sections; <paramref name="RenderMarkdown"/> renders descriptions.</summary>
public record AuthorizationSectionModel(
	IReadOnlyList<AuthRequirement> Requirements,
	IReadOnlyList<PrivilegeRow> Privileges,
	Func<string?, HtmlString> RenderMarkdown,
	bool AuthenticationOptional = false
);

/// <summary>
/// The figure under a section's label in the strip: a count the section's own list backs up, or the media type when the section has nothing to count.
/// </summary>
public record SectionStat(string? Value, string Caption);

/// <summary>The section strip: its links, and the required privileges it carries as its first row.</summary>
public record SectionStripModel(IReadOnlyList<SectionLink> Links, AuthorizationSectionModel Authorization);

/// <summary>One quick link of the strip and the pinned bar above an operation's sections.</summary>
public record SectionLink(string Label, string Anchor, SectionStat? Stat = null);

public class OperationViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiOperation Operation { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required OperationPageModel Page { get; init; }

	public AuthorizationSectionModel Authorization =>
		new(
			Page.AuthRequirements,
			Page.RequiredPrivileges,
			markdown => RenderMarkdown(markdown),
			Page.AuthBadge?.Kind == AuthBadgeKind.Optional
		);

	/// <summary>
	/// The sections under the quick links bar, in page order. Required privileges is not one of them: it comes before the bar.
	/// </summary>
	public IReadOnlyList<SectionLink> SectionLinks
	{
		get
		{
			var links = new List<SectionLink>(
				Page.ParameterSections.Select(
					static s => new SectionLink(ParameterLabel(s.Location), s.Header.Anchor, Counted(s.Parameters.Items.Count, "parameter"))
				)
			);
			if (Operation.Operation.RequestBody is not null)
				links.Add(new SectionLink("Body", "request-body", BodyStat()));
			if (Page.Responses.Count > 0)
				links.Add(new SectionLink("Responses", "responses", Counted(Page.Responses.Count, "response")));
			return links;
		}
	}

	private static SectionStat Counted(int count, string unit, string? plural = null) =>
		new(count.ToString(CultureInfo.InvariantCulture), count == 1 ? unit : (plural ?? (unit + "s")));

	/// <summary>The top-level names the Body section's own summary lists, so the figure matches what the reader opens.</summary>
	private SectionStat? BodyStat()
	{
		var count = Page.RequestPropertyNames.Count;
		if (count > 0)
			return Page.RequestSummaryStyle == SummaryStyle.Variants ? Counted(count, "variant") : Counted(count, "property", "properties");
		return Page.PrimaryRequest is { } primary ? new SectionStat(null, primary.ContentType) : null;
	}

	private static string ParameterLabel(ParameterLocation location) =>
		location == ParameterLocation.Header ? "Headers" : location.ToString();

	/// <summary>Where the <c>auth</c> badge links: the Authentication page, or the endpoint's own section.</summary>
	public string? AuthBadgeHref => Page.AuthBadge?.Target switch
	{
		AuthBadgeTarget.Section => "#authentication",
		AuthBadgeTarget.Page => LandingNavigationItem.Of(CurrentNavigationItem.NavigationRoot)?.AuthenticationUrl,
		_ => null
	};

	protected override string? LayoutPageTitle =>
		OpenApiExtensionReader.GetSummary(Operation.Operation) ?? CurrentNavigationItem.NavigationTitle;

	// Lifting the authorization text out can leave nothing in the description, and a page with no meta description is worse.
	protected override string? LayoutPageDescription =>
		ApiSeoDescription.OperationText(Page.DescriptionMarkdown, OpenApiExtensionReader.GetSummary(Operation.Operation));

	protected override IReadOnlyList<TocItem> GetTocItems()
	{
		var operation = Operation.Operation;
		var tocItems = new List<TocItem> { new("Paths", "paths", 2) };

		if (Page.RequiredPrivileges.Count > 0)
			tocItems.Add(new TocItem("Required privileges", "required-privileges", 2));

		if (Page.AuthRequirements.Count > 0)
			tocItems.Add(new TocItem("Authentication", "authentication", 2));

		// The path parameters have never had an entry: they sit right under the paths they belong to.
		tocItems.AddRange(
			Page
				.ParameterSections
				.Where(static s => s.Location != ParameterLocation.Path)
				.Select(static s => new TocItem(s.Header.Title, s.Header.Anchor, 2))
		);

		if (operation.RequestBody is not null)
			tocItems.Add(new TocItem("Request body", "request-body", 2));

		if (operation.Responses is { Count: > 0 })
			tocItems.Add(new TocItem("Responses", "responses", 2));

		return tocItems;
	}

	public IReadOnlyList<ApiServerDisplay> Servers => field ??= StructuralViewModel.ToDisplays(Page.Servers);

	public OperationExamplesPanelModel? ExamplesPanel =>
		Page.Scenarios.Count == 0 ? null : new OperationExamplesPanelModel { Scenarios = Page.Scenarios };
}
