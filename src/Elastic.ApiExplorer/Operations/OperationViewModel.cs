// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

public class OperationViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiOperation Operation { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required OperationPageModel Page { get; init; }

	public IReadOnlyList<string>? RequiredAuthItems =>
		OpenApiXReqAuthParser.TryGetPrerequisiteLines(
			Operation.Operation,
			RenderContext.ApiExplorerLog,
			Operation.Route,
			Operation.Operation.OperationId
		);

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		var operation = Operation.Operation;
		var tocItems = new List<ApiTocItem> { new("Paths", "paths") };

		if (RequiredAuthItems is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Prerequisites", "prerequisites"));

		if (!string.IsNullOrWhiteSpace(operation.Description))
			tocItems.Add(new ApiTocItem("Description", "description"));

		if (Page.QueryParameters.Count > 0)
			tocItems.Add(new ApiTocItem("Query String Parameters", "query-params"));

		if (operation.RequestBody is not null)
			tocItems.Add(new ApiTocItem("Request Body", "request-body"));

		if (operation.Responses is { Count: > 0 })
			tocItems.Add(new ApiTocItem(operation.Responses.Count == 1 ? "Response" : "Responses", "responses"));

		if (Page.CodeSamples.Count > 0)
			tocItems.Add(new ApiTocItem("Code Examples", "code-examples"));

		if (Page.ShowRequestExamples)
			tocItems.Add(new ApiTocItem("Request Examples", "request-examples"));

		if (Page.ShowResponseExamples)
			tocItems.Add(new ApiTocItem("Response Examples", "response-examples"));

		return tocItems;
	}
}
