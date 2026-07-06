// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Schema;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>A query string parameter with its structural display data precomputed.</summary>
public record ApiQueryParameter
{
	/// <summary>The underlying OpenAPI parameter; views read scalar values off it directly.</summary>
	public required IOpenApiParameter Parameter { get; init; }

	public required TypeAnnotation? Type { get; init; }
	public required IReadOnlyList<ConstraintDisplay> Constraints { get; init; }
	public required IReadOnlyList<string> EnumValues { get; init; }
	public required IReadOnlyList<UnionBadge> UnionOptions { get; init; }
}

/// <summary>One response content entry with its property tree prebuilt.</summary>
public record ApiResponseContent
{
	public required string ContentType { get; init; }
	public required TypeAnnotation Type { get; init; }
	public required ApiPropertyList? Properties { get; init; }

	/// <summary>Item properties when the response is an array of objects.</summary>
	public required ApiPropertyList? ArrayItemProperties { get; init; }
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
}

/// <summary>
/// Everything structural an operation page renders, precomputed before the view runs.
/// Scalar values (summary, descriptions, parameter names) are read off the raw operation in the view.
/// </summary>
public record OperationPageModel
{
	public required AvailabilityBadgeData? Availability { get; init; }
	public required bool IsBeta { get; init; }
	public required ExternalDocLink? ExternalDocs { get; init; }
	public required IList<OpenApiServer>? Servers { get; init; }
	public required IReadOnlyCollection<OperationNavigationItem> Overloads { get; init; }
	public required IReadOnlyList<IOpenApiParameter> PathParameters { get; init; }
	public required IReadOnlyList<ApiQueryParameter> QueryParameters { get; init; }
	public required string RequestContentType { get; init; }
	public required ApiPropertyList? RequestProperties { get; init; }
	public required TypeAnnotation? RequestType { get; init; }
	public required IReadOnlyList<ApiResponse> Responses { get; init; }
	public required IReadOnlyList<CodeSample> CodeSamples { get; init; }
	public required IDictionary<string, IOpenApiExample>? RequestExamples { get; init; }
	public required IDictionary<string, IOpenApiExample>? ResponseExamples { get; init; }
	public required bool ShowRequestExamples { get; init; }
	public required bool ShowResponseExamples { get; init; }

	/// <summary>Anchor of the first examples section; null when the page has no examples at all.</summary>
	public required string? ExamplesAnchor { get; init; }

	public static OperationPageModel Create(ApiOperation apiOperation, ApiRenderContext context)
	{
		var operation = apiOperation.Operation;
		var document = context.Model;
		var analyzer = new SchemaAnalyzer(document);
		var options = new PropertyDisplayOptions
		{
			RenderMarkdown = markdown => ApiMarkdown.Render(context.MarkdownRenderer, markdown),
			ApiRootUrl = context.CurrentNavigation.NavigationRoot.Url,
			VersionsConfiguration = context.BuildContext.VersionsConfiguration
		};
		var builder = new ApiPropertyTreeBuilder(document, options);

		var codeSamples = OperationViewModel.ParseCodeSamples(operation);
		var requestExamples = operation.RequestBody?.Content?.FirstOrDefault().Value?.Examples;
		var successResponse = operation.Responses?.FirstOrDefault(r => r.Key.StartsWith('2')).Value;
		var responseExamples = successResponse?.Content?.FirstOrDefault().Value?.Examples;

		var showRequestExamples = requestExamples is { Count: > 0 } && !(requestExamples.Count == 1 && codeSamples.Count > 0);
		var showResponseExamples = responseExamples is { Count: > 0 };
		var examplesAnchor = codeSamples.Count > 0 ? "code-examples"
			: requestExamples is { Count: > 0 } ? "request-examples"
			: responseExamples is { Count: > 0 } ? "response-examples"
			: null;

		var requestContentEntry = operation.RequestBody?.Content?.FirstOrDefault();
		var requestSchema = requestContentEntry?.Value?.Schema;

		ExternalDocLink? externalDocs = null;
		if (operation.ExternalDocs?.Url is not null)
		{
			var url = operation.ExternalDocs.Url.ToString();
			externalDocs = new ExternalDocLink(url, ApiPropertyTreeBuilder.IsElasticDocsUrl(url));
		}

		return new OperationPageModel
		{
			Availability = AvailabilityBadgeHelper.FromOperation(operation, context.BuildContext.VersionsConfiguration),
			IsBeta = ParseBetaFlag(operation),
			ExternalDocs = externalDocs,
			Servers = operation.Servers is { Count: > 0 } ? operation.Servers : document.Servers,
			Overloads = ResolveOverloads(context),
			PathParameters = operation.Parameters?.Where(p => p.In == ParameterLocation.Path).ToArray() ?? [],
			QueryParameters = (operation.Parameters ?? [])
				.Where(p => p.In == ParameterLocation.Query)
				.Select(p => BuildQueryParameter(p, analyzer, builder))
				.ToArray(),
			RequestContentType = requestContentEntry?.Key ?? "application/json",
			RequestProperties = requestSchema is not null
				? builder.BuildPropertyList(requestSchema, "req", isRequest: true)
				: null,
			RequestType = requestSchema is not null ? builder.Describe(requestSchema) : null,
			Responses = BuildResponses(operation, analyzer, builder),
			CodeSamples = codeSamples,
			RequestExamples = requestExamples,
			ResponseExamples = responseExamples,
			ShowRequestExamples = showRequestExamples,
			ShowResponseExamples = showResponseExamples,
			ExamplesAnchor = examplesAnchor
		};
	}

	private static bool ParseBetaFlag(OpenApiOperation operation) =>
		operation.Extensions?.TryGetValue("x-beta", out var betaValue) == true
		&& betaValue is JsonNodeExtension betaExtension
		&& betaExtension.Node is JsonValue betaJsonValue
		&& betaJsonValue.TryGetValue<bool>(out var betaFlag) && betaFlag;

	private static IReadOnlyCollection<OperationNavigationItem> ResolveOverloads(ApiRenderContext context)
	{
		if (context.CurrentNavigation.Parent is EndpointNavigationItem { NavigationItems.Count: > 0 } parent
			&& parent.NavigationItems.All(n => n.Hidden))
			return parent.NavigationItems;
		return context.CurrentNavigation is OperationNavigationItem self ? [self] : [];
	}

	private static ApiQueryParameter BuildQueryParameter(IOpenApiParameter parameter, SchemaAnalyzer analyzer, ApiPropertyTreeBuilder builder)
	{
		var schema = parameter.Schema;
		return new ApiQueryParameter
		{
			Parameter = parameter,
			Type = schema is not null ? builder.Describe(schema) : null,
			Constraints = schema is not null ? ApiPropertyTreeBuilder.BuildConstraints(schema) : [],
			EnumValues = CollectEnumValues(schema, analyzer),
			UnionOptions = CollectUnionOptionNames(schema, analyzer)
				.Select(n => new UnionBadge(n, ApiPropertyTreeBuilder.IsTypeOptionBadge(n)))
				.ToArray()
		};
	}

	private static IReadOnlyList<string> CollectEnumValues(IOpenApiSchema? schema, SchemaAnalyzer analyzer)
	{
		var resolved = schema is not null ? analyzer.ResolveSchema(schema) : null;

		// Collect enum values from direct enum, resolved enum, or union of string literals
		var enumValues = new List<string>();
		if (schema?.Enum is { Count: > 0 })
			enumValues.AddRange(schema.Enum.Select(e => e?.ToString()?.Trim('"') ?? "").Where(e => !string.IsNullOrEmpty(e)));
		else if (resolved?.Enum is { Count: > 0 })
			enumValues.AddRange(resolved.Enum.Select(e => e?.ToString()?.Trim('"') ?? "").Where(e => !string.IsNullOrEmpty(e)));

		if (enumValues.Count > 0)
			return enumValues;

		// Check for oneOf/anyOf with string literals (union enums)
		var unionSchemas = resolved?.OneOf is { Count: > 0 } ? resolved.OneOf
			: resolved?.AnyOf is { Count: > 0 } ? resolved.AnyOf
			: null;
		if (unionSchemas is not null)
		{
			enumValues.AddRange(
				unionSchemas
					.Select(analyzer.ResolveSchema)
					.Where(r => r?.Enum is { Count: > 0 })
					.SelectMany(r => r!.Enum!
						.Select(e => e?.ToString()?.Trim('"') ?? "")
						.Where(e => !string.IsNullOrEmpty(e))));
		}

		return enumValues;
	}

	private static IReadOnlyList<string> CollectUnionOptionNames(IOpenApiSchema? schema, SchemaAnalyzer analyzer)
	{
		var typeInfo = schema is not null ? analyzer.GetTypeInfo(schema) : null;
		if (typeInfo?.AnyOfOptions is { Count: > 0 })
			return typeInfo.AnyOfOptions.Select(o => o.Name).Where(n => !string.IsNullOrEmpty(n)).ToArray();
		if (typeInfo?.UnionOptions is { Length: > 0 })
			return typeInfo.UnionOptions.Where(n => !string.IsNullOrEmpty(n)).ToArray();
		return [];
	}

	private static IReadOnlyList<ApiResponse> BuildResponses(OpenApiOperation operation, SchemaAnalyzer analyzer, ApiPropertyTreeBuilder builder)
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
				StatusClass = statusCode.StartsWith('2') ? "success"
					: statusCode.StartsWith('4') || statusCode.StartsWith('5') ? "error"
					: "info",
				Contents = response.Content is null
					? []
					: response.Content
						.Where(ct => ct.Value?.Schema is not null)
						.Select(ct => BuildResponseContent(ct.Key, ct.Value!.Schema!, statusCode, analyzer, builder))
						.ToArray(),
				Headers = response.Headers is null
					? []
					: response.Headers
						.Select(h => new ApiResponseHeader
						{
							Name = h.Key,
							Header = h.Value,
							Type = h.Value?.Schema is not null ? builder.Describe(h.Value.Schema) : null
						})
						.ToArray()
			});
		}

		return responses;
	}

	private static ApiResponseContent BuildResponseContent(
		string contentType, IOpenApiSchema responseSchema, string statusCode, SchemaAnalyzer analyzer, ApiPropertyTreeBuilder builder)
	{
		var properties = builder.BuildPropertyList(responseSchema, $"res-{statusCode}", isRequest: false);

		// For arrays, check if the item type has properties we should render
		ApiPropertyList? arrayItemProperties = null;
		if (properties is null && analyzer.GetTypeInfo(responseSchema).IsArray)
		{
			var arrayItemSchema = ResolveArrayItems(responseSchema, analyzer);
			if (arrayItemSchema is not null)
				arrayItemProperties = builder.BuildPropertyList(arrayItemSchema, $"res-{statusCode}", isRequest: false);
		}

		return new ApiResponseContent
		{
			ContentType = contentType,
			Type = builder.Describe(responseSchema),
			Properties = properties,
			ArrayItemProperties = arrayItemProperties
		};
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
