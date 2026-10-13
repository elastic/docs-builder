// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>Required privileges and credentials for the Required authorization and Authentication sections; <paramref name="RenderMarkdown"/> renders descriptions.</summary>
public record AuthorizationSectionModel(
	IReadOnlyList<AuthRequirement> Requirements,
	IReadOnlyList<PrivilegeRow> Privileges,
	Func<string?, HtmlString> RenderMarkdown
);

public class OperationViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiOperation Operation { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required OperationPageModel Page { get; init; }

	public AuthorizationSectionModel Authorization =>
		new(Page.AuthRequirements, Page.RequiredPrivileges, markdown => RenderMarkdown(markdown));

	protected override string? LayoutPageTitle => Operation.Operation.Summary ?? CurrentNavigationItem.NavigationTitle;

	// Lifting the authorization text out can leave nothing in the description, and a page with no meta description is worse.
	protected override string? LayoutPageDescription => DescriptionOrSummary(Page.DescriptionMarkdown, Operation.Operation.Summary);

	internal static string? DescriptionOrSummary(string? description, string? summary) =>
		string.IsNullOrWhiteSpace(description) ? summary : description;

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		var operation = Operation.Operation;
		var tocItems = new List<ApiTocItem> { new("Paths", "paths") };

		if (Page.RequiredPrivileges.Count > 0)
			tocItems.Add(new ApiTocItem("Required authorization", "required-authorization"));

		if (Page.AuthRequirements.Count > 0)
			tocItems.Add(new ApiTocItem("Authentication", "authentication"));

		tocItems.AddRange(Page.ParameterSections.Select(static s => new ApiTocItem(s.Header.Title, s.Header.Anchor)));

		if (operation.RequestBody is not null)
			tocItems.Add(new ApiTocItem("Request", "request-body"));

		if (operation.Responses is { Count: > 0 })
			tocItems.Add(new ApiTocItem(operation.Responses.Count == 1 ? "Response" : "Responses", "responses"));

		return tocItems;
	}

	public OperationExamplesPanelModel? ExamplesPanel =>
		Page.Scenarios.Count == 0 ? null : new OperationExamplesPanelModel { Scenarios = Page.Scenarios };
}
