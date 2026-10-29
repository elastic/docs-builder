// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

/// <summary>Required privileges and credentials for the Required authorization and Authentication sections; <paramref name="RenderMarkdown"/> renders descriptions.</summary>
public record AuthorizationSectionModel(
	IReadOnlyList<AuthRequirement> Requirements,
	IReadOnlyList<PrivilegeRow> Privileges,
	Func<string?, HtmlString> RenderMarkdown,
	bool AuthenticationOptional = false
)
{
	/// <summary>Every privilege, one name each, for the heading's summary.</summary>
	public IReadOnlyList<string> PrivilegeNames => [.. Privileges.SelectMany(static r => r.Values)];

	/// <summary>Alternatives read <c>a | b</c> and privileges needed together <c>a + b</c>, as with the credentials.</summary>
	public SummaryStyle PrivilegeStyle =>
		Privileges.Any(static r => r.Join == "or")
			? SummaryStyle.Variants
			: Privileges.Any(static r => r.Join == "and") ? SummaryStyle.All : SummaryStyle.List;
}

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
		DescriptionOrSummary(Page.DescriptionMarkdown, OpenApiExtensionReader.GetSummary(Operation.Operation));

	internal static string? DescriptionOrSummary(string? description, string? summary) =>
		string.IsNullOrWhiteSpace(description) ? summary : description;

	protected override IReadOnlyList<TocItem> GetTocItems()
	{
		var operation = Operation.Operation;
		var tocItems = new List<TocItem> { new("Paths", "paths", 2) };

		if (Page.RequiredPrivileges.Count > 0)
			tocItems.Add(new TocItem("Required authorization", "required-authorization", 2));

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
			tocItems.Add(new TocItem(operation.Responses.Count == 1 ? "Response" : "Responses", "responses", 2));

		return tocItems;
	}

	public IReadOnlyList<ApiServerDisplay> Servers => field ??= StructuralViewModel.ToDisplays(Page.Servers);

	public OperationExamplesPanelModel? ExamplesPanel =>
		Page.Scenarios.Count == 0 ? null : new OperationExamplesPanelModel { Scenarios = Page.Scenarios };
}
