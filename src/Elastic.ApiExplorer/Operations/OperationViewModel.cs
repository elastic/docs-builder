// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

public class OperationViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiOperation Operation { get; init; }

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		var operation = Operation.Operation;
		var tocItems = new List<ApiTocItem> { new("Paths", "paths") };

		if (!string.IsNullOrWhiteSpace(operation.Description))
			tocItems.Add(new ApiTocItem("Description", "description"));

		var queryParams = operation.Parameters?.Where(p => p.In == ParameterLocation.Query).ToArray() ?? [];
		if (queryParams.Length > 0)
			tocItems.Add(new ApiTocItem("Query String Parameters", "query-params"));

		if (operation.RequestBody is not null)
			tocItems.Add(new ApiTocItem("Request Body", "request-body"));

		if (operation.Responses is { Count: > 0 })
			tocItems.Add(new ApiTocItem(operation.Responses.Count == 1 ? "Response" : "Responses", "responses"));

		// Request body examples
		var reqContent = operation.RequestBody?.Content?.FirstOrDefault().Value;
		if (reqContent?.Examples is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Request Examples", "request-examples"));

		// Response examples
		var successResp = operation.Responses?.FirstOrDefault(r => r.Key.StartsWith('2')).Value;
		var respContent = successResp?.Content?.FirstOrDefault().Value;
		if (respContent?.Examples is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Response Examples", "response-examples"));

		return tocItems;
	}
}
