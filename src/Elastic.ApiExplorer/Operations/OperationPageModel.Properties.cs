// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Search.Contract;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

internal record OperationPropertyTrees(
	ApiPropertyList Path,
	ApiPropertyList Query,
	ApiPropertyList Header,
	IReadOnlyList<ApiMediaContent> Request,
	IReadOnlyList<ApiResponse> Responses
)
{
	public ApiPropertyEntry[] IndexEntries() =>
		[
			.. ApiPropertyIndex.Entries(ApiPropertyIndex.Flatten(Path), "path"),
			.. ApiPropertyIndex.Entries(ApiPropertyIndex.Flatten(Query), "query"),
			.. ApiPropertyIndex.Entries(ApiPropertyIndex.Flatten(Header), "header"),
			.. ApiPropertyIndex.Entries(Request.SelectMany(Rows), "request"),
			.. Responses.SelectMany(response => ApiPropertyIndex.Entries(response.Contents.SelectMany(Rows), "response"))
		];

	private static IEnumerable<ApiProperty> Rows(ApiMediaContent content) =>
		(content.Properties is { } properties ? ApiPropertyIndex.Flatten(properties) : []).Concat(
			ApiPropertyIndex.Flatten(content.UnionVariants)
		);
}

public partial record OperationPageModel
{
	internal static OperationPropertyTrees BuildPropertyTrees(ApiOperation operation, SchemaAnalyzer analyzer, ParameterRowSource source)
	{
		// Repeated shapes link to their first listing, so consumers must build in page order.
		var path = BuildParameters(operation.Parameters, ParameterLocation.Path, source);
		var query = BuildParameters(operation.Parameters, ParameterLocation.Query, source);
		var header = BuildParameters(operation.Parameters, ParameterLocation.Header, source);
		var request = BuildMediaContents(
			operation.Operation.RequestBody?.Content,
			new PropertyTreeScope
			{
				Prefix = "req",
				JsonPath = "",
				IsRequest = true,
				DescriptionOverrides = source.Supplemental?.RequestBodyOverrides
			},
			analyzer,
			source.Builder
		);
		return new OperationPropertyTrees(path, query, header, request, BuildResponses(operation.Operation, analyzer, source.Builder));
	}
}
