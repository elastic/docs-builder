// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>One response content entry with its property tree prebuilt.</summary>
public record ApiResponseContent
{
	public required string ContentType { get; init; }
	public required TypeAnnotation Type { get; init; }
	public required ApiPropertyList? Properties { get; init; }

	/// <summary>Item properties when the response is an array of objects.</summary>
	public required ApiPropertyList? ArrayItemProperties { get; init; }

	/// <summary>Expanded oneOf/anyOf variants when the response body is a union of objects.</summary>
	public ApiUnionVariants? UnionVariants { get; init; }

	/// <summary>Whether the body lists properties, variants, or both, rather than only its type.</summary>
	public bool HasBody => Properties is not null || UnionVariants is { Variants.Count: > 0 };
}

/// <summary>A response header with its type annotation precomputed.</summary>
public record ApiResponseHeader
{
	public required string Name { get; init; }
	public required IOpenApiHeader Header { get; init; }
	public required TypeAnnotation? Type { get; init; }
}

/// <summary>A single response with its renderable content entries.</summary>
public record ApiResponse
{
	public required string StatusCode { get; init; }
	public required IOpenApiResponse Response { get; init; }
	public required string StatusClass { get; init; }

	/// <summary>Content type of the first content entry regardless of whether it declares a schema.</summary>
	public required string? FirstContentType { get; init; }
	public required IReadOnlyList<ApiResponseContent> Contents { get; init; }
	public required IReadOnlyList<ApiResponseHeader> Headers { get; init; }

	/// <summary>False when there is no schema or header to document, e.g. an example-only or 204 response.</summary>
	public bool HasDetails => Contents.Count > 0 || Headers.Count > 0;
}

/// <summary>Status-code accordion under the Responses heading.</summary>
public record ResponsesBlockModel(IReadOnlyList<ApiResponse> Responses, Func<string?, HtmlString> RenderMarkdown);

public partial record OperationPageModel
{
	private static IReadOnlyList<ApiResponse> BuildResponses(
		OpenApiOperation operation,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		if (operation.Responses is not { Count: > 0 })
			return [];

		var responses = new List<ApiResponse>(operation.Responses.Count);
		foreach (var (statusCode, response) in operation.Responses)
		{
			if (response is null)
				continue;

			responses.Add(new ApiResponse
			{
				StatusCode = statusCode,
				Response = response,
				FirstContentType = response.Content is { Count: > 0 } ? response.Content.First().Key : null,
				StatusClass = statusCode.StartsWith('2')
					? "success"
					: statusCode.StartsWith('4') || statusCode.StartsWith('5') ? "error" : "info",
				Contents = response.Content is null
					? []
					: response
						.Content
						.Where(ct => ct.Value?.Schema is not null)
						.Select(ct => BuildResponseContent(ct.Key, ct.Value!.Schema!, statusCode, analyzer, builder))
						.ToArray(),
				Headers = response.Headers is null
					? []
					: response
						.Headers
						.Select(
							h => new ApiResponseHeader
							{
								Name = h.Key,
								Header = h.Value,
								Type = h.Value?.Schema is not null ? builder.Describe(h.Value.Schema) : null
							}
						)
						.ToArray()
			});
		}

		return responses;
	}

	private static ApiResponseContent BuildResponseContent(
		string contentType,
		IOpenApiSchema responseSchema,
		string statusCode,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		var scope = new PropertyTreeScope { Prefix = $"res-{statusCode}" };
		var (properties, unionVariants) = ApiBodyContent.Build(responseSchema, scope, analyzer, builder);
		var arrayItemProperties = properties is null && unionVariants is null
			? BuildArrayItemProperties(responseSchema, scope, analyzer, builder)
			: null;

		return new ApiResponseContent
		{
			ContentType = contentType,
			Type = builder.Describe(responseSchema),
			Properties = properties,
			ArrayItemProperties = arrayItemProperties,
			UnionVariants = unionVariants
		};
	}

	private static ApiPropertyList? BuildArrayItemProperties(
		IOpenApiSchema responseSchema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		if (!analyzer.GetTypeInfo(responseSchema).IsArray)
			return null;

		var arrayItemSchema = ResolveArrayItems(responseSchema, analyzer);
		return arrayItemSchema is null ? null : builder.BuildPropertyList(arrayItemSchema, scope);
	}

	private static IOpenApiSchema? ResolveArrayItems(IOpenApiSchema schema, SchemaAnalyzer analyzer)
	{
		if (schema.Items is not null)
			return schema.Items;

		// Schema references may need explicit resolution before Items is available
		if (schema is OpenApiSchemaReference)
			return analyzer.ResolveSchema(schema)?.Items;
		return null;
	}
}
