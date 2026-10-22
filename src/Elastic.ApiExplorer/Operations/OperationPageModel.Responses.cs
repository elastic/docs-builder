// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>One content entry of a request or response body with its property tree prebuilt.</summary>
public record ApiMediaContent
{
	public required string ContentType { get; init; }

	/// <summary>Null when the entry declares no schema, as when the spec lists a content type only to attach examples.</summary>
	public required TypeAnnotation? Type { get; init; }

	/// <summary>The body's fields; for an array of objects, the fields of each item (see <see cref="IsArray"/>).</summary>
	public required ApiPropertyList? Properties { get; init; }

	/// <summary>Whether the body is an array of objects, so <see cref="Properties"/> lists each item's fields.</summary>
	public bool IsArray { get; init; }

	/// <summary>Expanded oneOf/anyOf variants when the response body is a union of objects.</summary>
	public ApiUnionVariants? UnionVariants { get; init; }

	/// <summary>Which of the body's fields it needs, when its <c>oneOf</c>/<c>anyOf</c> only lists <c>required</c> sets.</summary>
	public RequiredAlternatives? Requires { get; init; }

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

	public required IReadOnlyList<ApiMediaContent> Contents { get; init; }
	public required IReadOnlyList<ApiResponseHeader> Headers { get; init; }

	/// <summary>False when there is no schema or header to document, e.g. an example-only or 204 response.</summary>
	public bool HasDetails => Contents.Any(static c => c.Type is not null) || Headers.Count > 0;
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
				StatusClass = statusCode.StartsWith('2')
					? "success"
					: statusCode.StartsWith('4') || statusCode.StartsWith('5') ? "error" : "info",
				Contents = BuildMediaContents(
					response.Content,
					new PropertyTreeScope { Prefix = $"res-{statusCode}", JsonPath = "", IsResponse = true },
					analyzer,
					builder
				),
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

	/// <summary>
	/// Every content entry, entries with a schema first so the first is the one the page leads with. Entries after the first
	/// get their own scope prefix so their field ids stay unique on the page.
	/// </summary>
	/// <summary>Content entries in spec order, those with a schema first, so the first is the one a page leads with.</summary>
	internal static IEnumerable<KeyValuePair<string, IOpenApiMediaType>> SchemaFirst(IDictionary<string, IOpenApiMediaType> content) =>
		content.OrderBy(static e => e.Value?.Schema is null);

	private static IReadOnlyList<ApiMediaContent> BuildMediaContents(
		IDictionary<string, IOpenApiMediaType>? content,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		if (content is not { Count: > 0 })
			return [];

		return [
			.. SchemaFirst(content).Select(
				(e, i) => BuildMediaContent(
					e.Key,
					e.Value?.Schema,
					i == 0 ? scope : scope with { Prefix = $"{scope.Prefix}-ct{i}" },
					analyzer,
					builder
				)
			)
		];
	}

	private static ApiMediaContent BuildMediaContent(
		string contentType,
		IOpenApiSchema? schema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		if (schema is null)
		{
			return new ApiMediaContent { ContentType = contentType, Type = null, Properties = null };
		}

		var body = ApiBodyContent.Build(schema, scope, analyzer, builder);

		return new ApiMediaContent
		{
			ContentType = contentType,
			Type = builder.Describe(schema),
			Properties = body.Properties,
			IsArray = body.IsArray,
			UnionVariants = body.UnionVariants,
			Requires = body.Requires
		};
	}
}

/// <summary>
/// The content entries of one request or response body. <see cref="IdPrefix"/> keeps panel ids unique per body, and
/// <see cref="Label"/> ("Request body", "200 response") names the body's group to a screen reader.
/// </summary>
public record MediaContentsModel(IReadOnlyList<ApiMediaContent> Contents, string IdPrefix, string TypeLabel, string Label)
{
	public static MediaContentsModel ForRequest(IReadOnlyList<ApiMediaContent> contents) =>
		new(contents, "request-body-content", "Type", "Request body");

	public static MediaContentsModel ForResponse(string statusCode, IReadOnlyList<ApiMediaContent> contents) =>
		new(contents, $"response-{statusCode}-content", "Response Type", $"{statusCode} response");

	/// <summary>Whether there is more than one content type to pick from.</summary>
	public bool HasChoice => Contents.Count > 1;

	public string PanelId(int index) => $"{IdPrefix}-{index}";

	public string SelectId => $"{IdPrefix}-select";
}

public record MediaContentBodyModel(ApiMediaContent Content, string TypeLabel);
