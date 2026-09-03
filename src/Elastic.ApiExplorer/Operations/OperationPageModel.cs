// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Supplemental;
using Elastic.ApiExplorer.Types;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>A request/response example with its markdown description prerendered.</summary>
public record ExampleDisplay(
	string Title,
	HtmlString? DescriptionHtml,
	string? JsonValue,
	string? ExternalValue,
	string? StatusCode = null,
	string? DescriptionMarkdown = null
);

/// <summary>One response body example tagged with its HTTP status code for the examples rail.</summary>
public record ExampleResponse
{
	public required string StatusCode { get; init; }
	public string? JsonValue { get; init; }
	public string? ExternalValue { get; init; }

	/// <summary>
	/// When true, the OpenAPI response declares no content (e.g. 204). The rail shows
	/// "No body" instead of "No example".
	/// </summary>
	public bool IsNoBody { get; init; }

	public bool HasExampleBody => JsonValue is not null || !string.IsNullOrEmpty(ExternalValue);
}

/// <summary>
/// One named example scenario for the right rail: optional multi-language code samples,
/// request body, and one or more response bodies (by status code) grouped under a shared title.
/// </summary>
public record ExampleScenario
{
	public required string Title { get; init; }
	public required string TabId { get; init; }
	public HtmlString? DescriptionHtml { get; init; }
	public string? RequestJson { get; init; }
	public string? RequestExternalValue { get; init; }
	public IReadOnlyList<ExampleResponse> Responses { get; init; } = [];
	public IReadOnlyList<CodeSample> CodeSamples { get; init; } = [];

	/// <summary>Request JSON is omitted when code samples already embed the request body.</summary>
	public bool ShowRequest => (RequestJson is not null || !string.IsNullOrEmpty(RequestExternalValue)) && CodeSamples.Count == 0;

	public bool ShowResponse => Responses.Count > 0;
}

/// <summary>Right-rail examples panel for operation pages (Scalar-style layout).</summary>
public record OperationExamplesPanelModel
{
	public required IReadOnlyList<ExampleScenario> Scenarios { get; init; }
}

/// <summary>A query string parameter with its structural display data precomputed.</summary>
public record ApiQueryParameter
{
	/// <summary>The underlying OpenAPI parameter; views read scalar values off it directly.</summary>
	public required IOpenApiParameter Parameter { get; init; }

	public required TypeAnnotation? Type { get; init; }
	public required IReadOnlyList<ConstraintDisplay> Constraints { get; init; }
	public required IReadOnlyList<string> EnumValues { get; init; }
	public required IReadOnlyList<UnionBadge> UnionOptions { get; init; }
	public required HtmlString DescriptionHtml { get; init; }
	public required string? DescriptionMarkdown { get; init; }
}

/// <summary>A path parameter with its effective description precomputed.</summary>
public record ApiPathParameter
{
	public required IOpenApiParameter Parameter { get; init; }
	public required HtmlString DescriptionHtml { get; init; }
	public required string? DescriptionMarkdown { get; init; }

	public string? Name => Parameter.Name;
	public bool? Deprecated => Parameter.Deprecated;
	public bool Required => Parameter.Required;
	public HtmlString Description => DescriptionHtml;
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
public partial record OperationPageModel
{
	public required AvailabilityBadgeData? Availability { get; init; }
	public required bool IsBeta { get; init; }
	public required ExternalDocLink? ExternalDocs { get; init; }
	public required IList<OpenApiServer>? Servers { get; init; }
	public required IReadOnlyCollection<OperationNavigationItem> Overloads { get; init; }
	public bool HasMultipleOverloads => Overloads.Count > 1;
	public required IReadOnlyList<ApiPathParameter> PathParameters { get; init; }
	public required IReadOnlyList<ApiQueryParameter> QueryParameters { get; init; }
	public required string? DescriptionMarkdown { get; init; }
	public required IReadOnlyList<ApiPostSection> PostSections { get; init; }
	public required string RequestContentType { get; init; }
	public required ApiPropertyList? RequestProperties { get; init; }
	public required TypeAnnotation? RequestType { get; init; }
	public required IReadOnlyList<ApiResponse> Responses { get; init; }
	public required IReadOnlyList<CodeSample> CodeSamples { get; init; }
	public required IReadOnlyList<ExampleDisplay> RequestExamples { get; init; }
	public required IReadOnlyList<ExampleDisplay> ResponseExamples { get; init; }
	public required bool ShowRequestExamples { get; init; }
	public required bool ShowResponseExamples { get; init; }
	public required IReadOnlyList<ExampleScenario> Scenarios { get; init; }

	/// <summary>Anchor of the examples rail; null when the page has no examples at all.</summary>
	public required string? ExamplesAnchor { get; init; }

	/// <summary>Effective auth scheme badges. Empty when the spec declares no schemes.</summary>
	public required IReadOnlyList<AuthSchemeBadge> AuthSchemes { get; init; }

	public static OperationPageModel Create(ApiOperation apiOperation, ApiRenderContext context)
	{
		var operation = apiOperation.Operation;
		var document = context.Model;
		var analyzer = new SchemaAnalyzer(document);
		var supplemental = operation.OperationId is { Length: > 0 } operationId
			&& context.OperationSupplemental.TryGetValue(operationId, out var doc) ? doc : null;
		var options = new PropertyDisplayOptions
		{
			RenderMarkdown = markdown => ApiMarkdown.Render(context, markdown),
			ApiRootUrl = context.CurrentNavigation.NavigationRoot.Url,
			VersionsConfiguration = context.BuildContext.VersionsConfiguration
		};
		var builder = new ApiPropertyTreeBuilder(document, options);

		var codeSamples = OpenApiExtensionReader.ParseCodeSamples(operation);
		var servers = operation.Servers is { Count: > 0 } ? operation.Servers : document.Servers;
		if (codeSamples.Count == 0)
			codeSamples = SyntheticCodeSamples.Create(apiOperation.OperationType, apiOperation.Route, operation, servers);

		var requestExamples = MapExamples(operation.RequestBody?.Content?.FirstOrDefault().Value?.Examples, options.RenderMarkdown);
		var responseExamples = MapResponseExamples(operation.Responses, options.RenderMarkdown);
		var scenarios = EnsureResponseTabs(BuildExampleScenarios(requestExamples, responseExamples, codeSamples), operation.Responses);
		var examplesAnchor = scenarios.Count > 0 ? "examples" : null;

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
			IsBeta = OpenApiExtensionReader.IsBeta(operation),
			ExternalDocs = externalDocs,
			Servers = servers,
			Overloads = ResolveOverloads(context),
			PathParameters = (operation.Parameters ?? [])
				.Where(p => p.In == ParameterLocation.Path)
				.Select(p =>
				{
					var description = supplemental?.ParameterOr(p.Name ?? "", p.Description) ?? p.Description;
					return new ApiPathParameter
					{
						Parameter = p,
						DescriptionHtml = ApiMarkdown.Render(context, description),
						DescriptionMarkdown = description
					};
				})
				.ToArray(),
			QueryParameters = (operation.Parameters ?? [])
				.Where(p => p.In == ParameterLocation.Query)
				.Select(p => BuildQueryParameter(p, analyzer, builder, context, supplemental))
				.ToArray(),
			RequestContentType = requestContentEntry?.Key ?? "application/json",
			RequestProperties = requestSchema is not null
				? builder.BuildPropertyList(
					requestSchema,
					new PropertyTreeScope { Prefix = "req", IsRequest = true, DescriptionOverrides = supplemental?.RequestBodyOverrides }
				)
				: null,
			DescriptionMarkdown = supplemental?.DescriptionOr(operation.Description) ?? operation.Description,
			PostSections = ApiPostSection.From(context, supplemental?.PostSections ?? []),
			RequestType = requestSchema is not null ? builder.Describe(requestSchema) : null,
			Responses = BuildResponses(operation, analyzer, builder),
			CodeSamples = codeSamples,
			RequestExamples = requestExamples,
			ResponseExamples = responseExamples,
			ShowRequestExamples = requestExamples.Count > 0 && !(requestExamples.Count == 1 && codeSamples.Count > 0),
			ShowResponseExamples = responseExamples.Count > 0,
			Scenarios = scenarios,
			ExamplesAnchor = examplesAnchor,
			AuthSchemes = OpenApiAuthSchemeResolver.Resolve(operation, document)
		};
	}

	/// <summary>
	/// Groups OpenAPI examples into rail scenarios:
	/// <list type="bullet">
	/// <item>Request examples define scenario variants (the rail <c>select</c>).</item>
	/// <item>Response examples whose title matches a request join that scenario.</item>
	/// <item>Unmatched response examples (typical error statuses) are shared across
	/// those request scenarios as extra status-code tabs, without overwriting a
	/// scenario-specific body for the same status.</item>
	/// <item>When there are no request examples, responses are grouped by title and
	/// then collapsed into a single scenario so status tabs stay primary.</item>
	/// </list>
	/// Multi-language <c>x-codeSamples</c> attach to the scenario whose request body
	/// matches the Console sample (or the first / a code-only scenario).
	/// </summary>
	public static IReadOnlyList<ExampleScenario> BuildExampleScenarios(
		IReadOnlyList<ExampleDisplay> requestExamples,
		IReadOnlyList<ExampleDisplay> responseExamples,
		IReadOnlyList<CodeSample> codeSamples
	)
	{
		var scenarios = new List<ExampleScenario>();
		var indexByTitle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var example in requestExamples)
			UpsertScenario(scenarios, indexByTitle, example, isRequest: true);

		var hasRequestScenarios = scenarios.Count > 0;
		var sharedResponses = new List<ExampleDisplay>();

		foreach (var example in responseExamples)
		{
			if (hasRequestScenarios && !indexByTitle.ContainsKey(example.Title))
			{
				sharedResponses.Add(example);
				continue;
			}

			UpsertScenario(scenarios, indexByTitle, example, isRequest: false);
		}

		if (hasRequestScenarios && sharedResponses.Count > 0)
		{
			for (var i = 0; i < scenarios.Count; i++)
				scenarios[i] = scenarios[i] with { Responses = MergeSharedResponses(scenarios[i].Responses, sharedResponses) };
		}
		else if (!hasRequestScenarios && scenarios.Count > 1)
			scenarios = CollapseIntoSingleScenario(scenarios);

		if (codeSamples.Count == 0)
			return scenarios;

		if (scenarios.Count == 0)
		{
			scenarios.Add(new ExampleScenario { Title = "Examples", TabId = "examples", CodeSamples = codeSamples });
			return scenarios;
		}

		var matchIndex = FindScenarioForCodeSamples(scenarios, codeSamples);
		scenarios[matchIndex] = scenarios[matchIndex] with { CodeSamples = codeSamples };
		return scenarios;
	}

	/// <summary>
	/// Adds shared (title-unmatched) response examples as status tabs, skipping any
	/// status the scenario already owns so request-paired bodies win.
	/// </summary>
	private static IReadOnlyList<ExampleResponse> MergeSharedResponses(
		IReadOnlyList<ExampleResponse> existing,
		IReadOnlyList<ExampleDisplay> shared
	)
	{
		var merged = existing;
		foreach (var example in shared)
		{
			var statusCode = string.IsNullOrEmpty(example.StatusCode) ? "default" : example.StatusCode;
			if (merged.Any(r => string.Equals(r.StatusCode, statusCode, StringComparison.OrdinalIgnoreCase)))
				continue;
			merged = UpsertResponse(merged, example);
		}

		return merged;
	}

	/// <summary>
	/// Response-only operations often name each status differently; fold them into one
	/// scenario so the rail exposes status tabs instead of a scenario <c>select</c>.
	/// </summary>
	private static List<ExampleScenario> CollapseIntoSingleScenario(List<ExampleScenario> scenarios)
	{
		var responses = new List<ExampleResponse>();
		foreach (var scenario in scenarios)
		{
			foreach (var response in scenario.Responses)
			{
				var alreadyPresent = responses.Any(
					r => string.Equals(r.StatusCode, response.StatusCode, StringComparison.OrdinalIgnoreCase)
				);
				if (alreadyPresent)
					continue;
				responses.Add(response);
			}
		}

		var ordered = responses.OrderBy(r => StatusSortKey(r.StatusCode)).ThenBy(r => r.StatusCode, StringComparer.Ordinal).ToArray();

		return [
			new ExampleScenario
			{
				Title = scenarios[0].Title,
				TabId = scenarios[0].TabId,
				DescriptionHtml = scenarios[0].DescriptionHtml,
				Responses = ordered
			}
		];
	}

	private static void UpsertScenario(
		List<ExampleScenario> scenarios,
		Dictionary<string, int> indexByTitle,
		ExampleDisplay example,
		bool isRequest
	)
	{
		if (indexByTitle.TryGetValue(example.Title, out var index))
		{
			var existing = scenarios[index];
			scenarios[index] = isRequest
				? existing with
				{
					DescriptionHtml = existing.DescriptionHtml ?? example.DescriptionHtml,
					RequestJson = example.JsonValue,
					RequestExternalValue = example.ExternalValue
				}
				: existing with
				{
					DescriptionHtml = existing.DescriptionHtml ?? example.DescriptionHtml,
					Responses = UpsertResponse(existing.Responses, example)
				};
			return;
		}

		indexByTitle[example.Title] = scenarios.Count;
		scenarios.Add(
			isRequest
				? new ExampleScenario
				{
					Title = example.Title,
					TabId = ToTabId(example.Title, scenarios.Count),
					DescriptionHtml = example.DescriptionHtml,
					RequestJson = example.JsonValue,
					RequestExternalValue = example.ExternalValue
				}
				: new ExampleScenario
				{
					Title = example.Title,
					TabId = ToTabId(example.Title, scenarios.Count),
					DescriptionHtml = example.DescriptionHtml,
					Responses = UpsertResponse([], example)
				}
		);
	}

	private static IReadOnlyList<ExampleResponse> UpsertResponse(IReadOnlyList<ExampleResponse> existing, ExampleDisplay example)
	{
		var statusCode = string.IsNullOrEmpty(example.StatusCode) ? "default" : example.StatusCode;
		var next = new ExampleResponse { StatusCode = statusCode, JsonValue = example.JsonValue, ExternalValue = example.ExternalValue };
		var list = existing.ToList();
		var index = list.FindIndex(r => string.Equals(r.StatusCode, statusCode, StringComparison.OrdinalIgnoreCase));
		if (index >= 0)
			list[index] = next;
		else
			list.Add(next);

		return list.OrderBy(r => StatusSortKey(r.StatusCode)).ThenBy(r => r.StatusCode, StringComparer.Ordinal).ToArray();
	}

	private static int StatusSortKey(string statusCode) =>
		statusCode.Length > 0 && statusCode[0] == '2'
			? 0
			: statusCode.Length > 0 && statusCode[0] == '3'
				? 1
				: statusCode.Length > 0 && statusCode[0] == '4' ? 2 : statusCode.Length > 0 && statusCode[0] == '5' ? 3 : 4;

	private static int FindScenarioForCodeSamples(IReadOnlyList<ExampleScenario> scenarios, IReadOnlyList<CodeSample> codeSamples)
	{
		var probe = codeSamples.FirstOrDefault(static s => string.Equals(s.Language, "Console", StringComparison.OrdinalIgnoreCase))
			?? codeSamples[0];
		var compactProbe = Compact(probe.Source);

		for (var i = 0; i < scenarios.Count; i++)
		{
			if (scenarios[i].RequestJson is not { Length: > 0 } requestJson)
				continue;
			var compactRequest = Compact(requestJson);
			if (compactRequest.Length == 0)
				continue;
			if (compactProbe.Contains(compactRequest, StringComparison.Ordinal))
				return i;
		}

		return 0;
	}

	private static string Compact(string value) => string.Concat(value.Where(static c => !char.IsWhiteSpace(c)));

	private static string ToTabId(string title, int index)
	{
		var chars = title.Trim().ToLowerInvariant().Select(static c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
		var slug = new string(chars).Trim('-');
		while (slug.Contains("--", StringComparison.Ordinal))
			slug = slug.Replace("--", "-", StringComparison.Ordinal);
		return string.IsNullOrEmpty(slug) ? $"scenario-{index}" : slug;
	}

	/// <summary>
	/// When scenarios have request/code samples but no response example bodies, attach
	/// status-code tabs from the operation's declared responses so the rail still shows
	/// "No body" / "No example" instead of omitting the response card.
	/// </summary>
	public static IReadOnlyList<ExampleScenario> EnsureResponseTabs(IReadOnlyList<ExampleScenario> scenarios, OpenApiResponses? responses)
	{
		if (scenarios.Count == 0 || responses is null || responses.Count == 0)
			return scenarios;

		if (scenarios.Any(static s => s.Responses.Count > 0))
			return scenarios;

		var fallback = BuildStatusOnlyResponses(responses);
		if (fallback.Count == 0)
			return scenarios;

		return scenarios.Select(s => s with { Responses = fallback }).ToArray();
	}

	private static IReadOnlyList<ExampleResponse> BuildStatusOnlyResponses(OpenApiResponses responses) =>
		responses
			.Where(static pair => pair.Value is not null)
			.Select(
				static pair => new ExampleResponse
				{
					StatusCode = pair.Key,
					IsNoBody = pair.Value.Content is null || pair.Value.Content.Count == 0
				}
			)
			.OrderBy(static r => StatusSortKey(r.StatusCode))
			.ThenBy(static r => r.StatusCode, StringComparer.Ordinal)
			.ToArray();

	private static IReadOnlyList<ExampleDisplay> MapResponseExamples(OpenApiResponses? responses, Func<string?, HtmlString> renderMarkdown)
	{
		if (responses is null || responses.Count == 0)
			return [];

		var list = new List<ExampleDisplay>();
		foreach (var (statusCode, response) in responses)
		{
			var examples = response?.Content?.FirstOrDefault().Value?.Examples;
			foreach (var example in MapExamples(examples, renderMarkdown, statusCode))
				list.Add(example);
		}

		return list;
	}

	private static IReadOnlyList<ExampleDisplay> MapExamples(
		IDictionary<string, IOpenApiExample>? examples,
		Func<string?, HtmlString> renderMarkdown,
		string? statusCode = null
	) =>
		examples is null
			? []
			: examples.Select(e =>
			{
				var description = SanitizeExampleDescription(e.Value?.Description);
				return new ExampleDisplay(
					string.IsNullOrEmpty(e.Value?.Summary) ? e.Key : e.Value.Summary,
					string.IsNullOrEmpty(description) ? null : renderMarkdown(description),
					e.Value?.Value?.ToString(),
					string.IsNullOrEmpty(e.Value?.ExternalValue) ? null : e.Value.ExternalValue,
					statusCode,
					description
				);
			}).ToArray();

	/// <summary>
	/// Drops leading boilerplate that only restates the HTTP call or a generic success line
	/// already visible in code samples. Keeps any trailing notes.
	/// </summary>
	public static string? SanitizeExampleDescription(string? description)
	{
		if (string.IsNullOrWhiteSpace(description))
			return null;

		var trimmed = description.Trim();
		while (true)
		{
			var runCommand = RunCommandBoilerplate().Match(trimmed);
			if (runCommand.Success)
			{
				trimmed = trimmed[runCommand.Length..].TrimStart();
				continue;
			}

			var successFrom = SuccessfulResponseFromBoilerplate().Match(trimmed);
			if (successFrom.Success)
			{
				trimmed = trimmed[successFrom.Length..].TrimStart();
				continue;
			}

			var exampleBody = ExampleBodyForRequestBoilerplate().Match(trimmed);
			if (exampleBody.Success)
			{
				trimmed = trimmed[exampleBody.Length..].TrimStart();
				continue;
			}

			var abbreviatedFrom = AbbreviatedResponseFromBoilerplate().Match(trimmed);
			if (abbreviatedFrom.Success)
			{
				trimmed = trimmed[abbreviatedFrom.Length..].TrimStart();
				continue;
			}

			break;
		}

		return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
	}

	/// <summary>Matches <c>Run `…` ….</c> instructional openers from elasticsearch-specification examples.</summary>
	[GeneratedRegex(@"^Run\s+`[^`]+`\s+[^.]*\.\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
	private static partial Regex RunCommandBoilerplate();

	/// <summary>Matches <c>A successful response from `METHOD path`.</c> openers that only echo the call.</summary>
	[GeneratedRegex(@"^A\s+successful\s+response\s+from\s+`[^`]+`\.\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
	private static partial Regex SuccessfulResponseFromBoilerplate();

	/// <summary>Matches <c>An example body for a `METHOD path` request.</c> openers that only label the JSON body.</summary>
	[GeneratedRegex(@"^An\s+example\s+body\s+for\s+a\s+`[^`]+`\s+request\.\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
	private static partial Regex ExampleBodyForRequestBoilerplate();

	/// <summary>Matches <c>An abbreviated response from `METHOD path`.</c> openers that only echo the call.</summary>
	[GeneratedRegex(@"^An\s+abbreviated\s+response\s+from\s+`[^`]+`\.\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
	private static partial Regex AbbreviatedResponseFromBoilerplate();

	private static IReadOnlyCollection<OperationNavigationItem> ResolveOverloads(ApiRenderContext context)
	{
		if (
			context.CurrentNavigation.Parent is EndpointNavigationItem { NavigationItems.Count: > 0 } parent
			&& parent.NavigationItems.All(n => n.Hidden)
		)
			return parent.NavigationItems;
		return context.CurrentNavigation is OperationNavigationItem self ? [self] : [];
	}

	private static ApiQueryParameter BuildQueryParameter(
		IOpenApiParameter parameter,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder,
		ApiRenderContext context,
		ApiSupplementalDoc? supplemental
	)
	{
		var schema = parameter.Schema;
		var description = supplemental?.ParameterOr(parameter.Name ?? "", parameter.Description) ?? parameter.Description;
		return new ApiQueryParameter
		{
			Parameter = parameter,
			Type = schema is not null ? builder.Describe(schema) : null,
			Constraints = schema is not null ? ApiPropertyTreeBuilder.BuildConstraints(schema) : [],
			EnumValues = CollectEnumValues(schema, analyzer),
			UnionOptions = CollectUnionOptionNames(schema, analyzer)
				.Select(n => new UnionBadge(n, ApiPropertyTreeBuilder.IsTypeOptionBadge(n)))
				.ToArray(),
			DescriptionHtml = ApiMarkdown.Render(context, description),
			DescriptionMarkdown = description
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
		var unionSchemas = resolved?.OneOf is { Count: > 0 } ? resolved.OneOf : resolved?.AnyOf is { Count: > 0 } ? resolved.AnyOf : null;
		if (unionSchemas is not null)
		{
			enumValues.AddRange(
				unionSchemas
					.Select(analyzer.ResolveSchema)
					.Where(r => r?.Enum is { Count: > 0 })
					.SelectMany(r => r!.Enum!.Select(e => e?.ToString()?.Trim('"') ?? "").Where(e => !string.IsNullOrEmpty(e)))
			);
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
		var properties = builder.BuildPropertyList(responseSchema, scope);

		// For arrays, check if the item type has properties we should render
		ApiPropertyList? arrayItemProperties = null;
		if (properties is null && analyzer.GetTypeInfo(responseSchema).IsArray)
		{
			var arrayItemSchema = ResolveArrayItems(responseSchema, analyzer);
			if (arrayItemSchema is not null)
				arrayItemProperties = builder.BuildPropertyList(arrayItemSchema, scope);
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
