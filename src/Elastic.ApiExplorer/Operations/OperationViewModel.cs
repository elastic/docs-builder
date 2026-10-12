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
	IReadOnlyList<AuthScheme> Schemes,
	IReadOnlyList<PrivilegeRow> Privileges,
	Func<string?, HtmlString> RenderMarkdown
);

public class OperationViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiOperation Operation { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required OperationPageModel Page { get; init; }

	public AuthorizationSectionModel Authorization => new(Page.AuthSchemes, Page.RequiredPrivileges, markdown => RenderMarkdown(markdown));

	protected override string? LayoutPageTitle => Operation.Operation.Summary ?? CurrentNavigationItem.NavigationTitle;

	protected override string? LayoutPageDescription => Page.DescriptionMarkdown;

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		var operation = Operation.Operation;
		var tocItems = new List<ApiTocItem> { new("Paths", "paths") };

		if (Page.RequiredPrivileges.Count > 0)
			tocItems.Add(new ApiTocItem("Required authorization", "required-authorization"));

		if (Page.AuthSchemes.Count > 0)
			tocItems.Add(new ApiTocItem("Authentication", "authentication"));

		if (Page.QueryParameters.Count > 0)
			tocItems.Add(new ApiTocItem("Query parameters", "query-params"));

		if (operation.RequestBody is not null)
			tocItems.Add(new ApiTocItem("Request", "request-body"));

		if (operation.Responses is { Count: > 0 })
			tocItems.Add(new ApiTocItem(operation.Responses.Count == 1 ? "Response" : "Responses", "responses"));

		return tocItems;
	}

	public OperationExamplesPanelModel? ExamplesPanel =>
		Page.Scenarios.Count == 0 ? null : new OperationExamplesPanelModel { Scenarios = Page.Scenarios };
}
