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
)
{
	/// <summary>Method and path named by a <c>Run `METHOD path`</c> description, before boilerplate is stripped.</summary>
	public (string Method, string Path)? RequestLine { get; init; }
}

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
	public string? HttpMethod { get; init; }
	public string? Route { get; init; }

	/// <summary>Method and path the example description says to run; generated samples use it.</summary>
	public (string Method, string Path)? RequestLine { get; init; }

	/// <summary>True when the attached code samples contain this scenario's request body verbatim.</summary>
	public bool CodeSamplesIncludeRequest { get; init; }

	/// <summary>Request JSON is omitted only when code samples already embed the request body.</summary>
	public bool ShowRequest => (RequestJson is not null || !string.IsNullOrEmpty(RequestExternalValue)) && !CodeSamplesIncludeRequest;

	public bool ShowResponse => Responses.Count > 0;
}

/// <summary>Right-rail examples panel: example chips over a carousel of language samples per example.</summary>
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
	public string UnionLabel { get; init; } = SchemaHelpers.UnionLabel(null);
	public required HtmlString DescriptionHtml { get; init; }
	public required string? DescriptionMarkdown { get; init; }
}

/// <summary>A path parameter with its structural display data precomputed.</summary>
public record ApiPathParameter
{
	public required IOpenApiParameter Parameter { get; init; }
	public required TypeAnnotation? Type { get; init; }
	public required IReadOnlyList<string> EnumValues { get; init; }
	public required IReadOnlyList<UnionBadge> UnionOptions { get; init; }
	public string UnionLabel { get; init; } = SchemaHelpers.UnionLabel(null);
	public required HtmlString DescriptionHtml { get; init; }
	public required string? DescriptionMarkdown { get; init; }

	/// <summary>True when the parameter's route segment can be left out (a sibling route without it exists).</summary>
	public required bool Optional { get; init; }

	public string? Name => Parameter.Name;
	public bool? Deprecated => Parameter.Deprecated;

	/// <summary>OpenAPI forces path parameters required; one that only some routes contain is optional.</summary>
	public required bool Required { get; init; }
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

	/// <summary>Expanded oneOf/anyOf variants when the response body is a union of objects.</summary>
	public ApiUnionVariants? UnionVariants { get; init; }
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
	/// <summary>Every route and method of the operation, merged into display rows.</summary>
	public required OperationEndpoint Endpoint { get; init; }
	public required IReadOnlyList<ApiPathParameter> PathParameters { get; init; }
	public required IReadOnlyList<ApiQueryParameter> QueryParameters { get; init; }

	public IReadOnlyList<string> PathParameterNames => NamesOf(PathParameters.Select(static p => p.Name));

	public IReadOnlyList<string> QueryParameterNames => NamesOf(QueryParameters.Select(static q => q.Parameter.Name));

	public IReadOnlyList<string> RequestPropertyNames =>
		NamesOf(RequestProperties is not null ? RequestProperties.Items.Select(static p => p.Name) : VariantNames(RequestUnionVariants));

	/// <summary>Names a collapsed section header lists; an array variant keeps its <c>[]</c> so it stays apart from the plain one.</summary>
	internal static IEnumerable<string> VariantNames(ApiUnionVariants? variants) =>
		(variants?.Variants ?? []).Select(static v => v.IsArrayVariant ? $"{v.DisplayName}[]" : v.DisplayName);

	public required string? DescriptionMarkdown { get; init; }
	public required IReadOnlyList<ApiPostSection> PostSections { get; init; }
	public required string RequestContentType { get; init; }
	public required ApiPropertyList? RequestProperties { get; init; }

	/// <summary>The variants of a request body that is itself a <c>oneOf</c>/<c>anyOf</c>; set only when there are no plain properties.</summary>
	public ApiUnionVariants? RequestUnionVariants { get; init; }
	public required TypeAnnotation? RequestType { get; init; }
	public required IReadOnlyList<ApiResponse> Responses { get; init; }
	public required IReadOnlyList<CodeSample> CodeSamples { get; init; }
	public required IReadOnlyList<ExampleDisplay> RequestExamples { get; init; }
	public required IReadOnlyList<ExampleDisplay> ResponseExamples { get; init; }
	public required bool ShowRequestExamples { get; init; }
	public required bool ShowResponseExamples { get; init; }
	public required IReadOnlyList<ExampleScenario> Scenarios { get; init; }

	/// <summary>Effective auth scheme badges. Empty when the spec declares no schemes.</summary>
	public required IReadOnlyList<AuthSchemeBadge> AuthSchemes { get; init; }

	public static OperationPageModel Create(ApiOperation apiOperation, ApiRenderContext context)
	{
		var operation = apiOperation.Operation;
		var document = context.Model;
		var analyzer = new SchemaAnalyzer(document, resolveCache: context.SchemaResolveCache);
		var supplemental = operation.OperationId is { Length: > 0 } operationId
			&& context.OperationSupplemental.TryGetValue(operationId, out var doc) ? doc : null;
		var options = new PropertyDisplayOptions
		{
			RenderMarkdown = markdown => ApiMarkdown.Render(context, markdown),
			ApiRootUrl = context.CurrentNavigation.NavigationRoot.Url,
			VersionsConfiguration = context.BuildContext.VersionsConfiguration,
			SchemaResolveCache = context.SchemaResolveCache
		};
		var builder = new ApiPropertyTreeBuilder(document, options);

		var siblings = context.CurrentNavigation is OperationNavigationItem { Siblings: var collapsed } ? collapsed : [];
		var endpoint = OperationEndpoint.Build(
			apiOperation,
			supplemental?.DescriptionOr(operation.Description) ?? operation.Description,
			siblings
		);
		var requestExamples = MapExamples(
			operation.RequestBody?.Content?.FirstOrDefault().Value?.Examples,
			options.RenderMarkdown,
			endpoint: endpoint
		);
		var codeSamples = SampleMethods.Align(OpenApiExtensionReader.ParseCodeSamples(operation), endpoint);
		var servers = operation.Servers is { Count: > 0 } ? operation.Servers : document.Servers;
		if (codeSamples.Count == 0)
		{
			// Built around the first example's body, so the samples attach to that example and carry its request.
			var body = requestExamples.FirstOrDefault(static e => !string.IsNullOrWhiteSpace(e.JsonValue))?.JsonValue;
			codeSamples = SyntheticCodeSamples.Create(apiOperation.OperationType, apiOperation.Route, operation, servers, body);
		}

		var responseExamples = MapResponseExamples(operation.Responses, options.RenderMarkdown);
		var scenarios = WithClientDocs(
			WithOperationIdentity(
				GeneratedCodeSamples.Fill(
					EnsureResponseTabs(BuildExampleScenarios(requestExamples, responseExamples, codeSamples), operation.Responses),
					codeSamples,
					endpoint
				),
				apiOperation.OperationType.ToString().ToLowerInvariant(),
				apiOperation.Route
			),
			context.Product?.Id
		);
		var requestContentEntry = operation.RequestBody?.Content?.FirstOrDefault();
		var requestSchema = requestContentEntry?.Value?.Schema;

		ExternalDocLink? externalDocs = null;
		if (operation.ExternalDocs?.Url is not null)
		{
			var url = operation.ExternalDocs.Url.ToString();
			externalDocs = new ExternalDocLink(url, ApiPropertyTreeBuilder.IsElasticDocsUrl(url), operation.ExternalDocs.Description);
		}

		var requestScope = new PropertyTreeScope
		{
			Prefix = "req",
			IsRequest = true,
			DescriptionOverrides = supplemental?.RequestBodyOverrides
		};
		var (requestProperties, requestUnionVariants) = requestSchema is not null
			? BuildBodyContent(requestSchema, requestScope, analyzer, builder)
			: (null, null);

		return new OperationPageModel
		{
			Availability = AvailabilityBadgeHelper.FromOperation(operation, context.BuildContext.VersionsConfiguration),
			IsBeta = OpenApiExtensionReader.IsBeta(operation),
			ExternalDocs = externalDocs,
			Servers = servers,
			Endpoint = endpoint,
			PathParameters = (operation.Parameters ?? [])
				.Where(p => p.In == ParameterLocation.Path)
				.Select(p => BuildPathParameter(p, analyzer, builder, context, supplemental, endpoint))
				.ToArray(),
			QueryParameters = (operation.Parameters ?? [])
				.Where(p => p.In == ParameterLocation.Query)
				.Select(p => BuildQueryParameter(p, analyzer, builder, context, supplemental))
				.ToArray(),
			RequestContentType = requestContentEntry?.Key ?? "application/json",
			RequestProperties = requestProperties,
			RequestUnionVariants = requestUnionVariants,
			DescriptionMarkdown = endpoint.Description,
			PostSections = ApiPostSection.From(context, supplemental?.PostSections ?? []),
			RequestType = requestSchema is not null ? builder.Describe(requestSchema) : null,
			Responses = BuildResponses(operation, analyzer, builder),
			CodeSamples = codeSamples,
			RequestExamples = requestExamples,
			ResponseExamples = responseExamples,
			ShowRequestExamples = requestExamples.Count > 0 && scenarios.Any(static s => s.ShowRequest),
			ShowResponseExamples = responseExamples.Count > 0,
			Scenarios = scenarios,
			AuthSchemes = OpenApiAuthSchemeResolver.Resolve(
				operation,
				document,
				$"{context.CurrentNavigation.NavigationRoot.Url.TrimEnd('/')}/{ApiUrlBuilder.AuthenticationSegment}"
			)
		};
	}

	internal static IReadOnlyList<ExampleScenario> WithOperationIdentity(
		IReadOnlyList<ExampleScenario> scenarios,
		string httpMethod,
		string route
	) => [.. scenarios.Select(s => s with { HttpMethod = httpMethod, Route = route })];

	/// <summary>
	/// Groups OpenAPI examples into rail scenarios:
	/// <list type="bullet">
	/// <item>Request examples define scenario variants (the Examples header <c>select</c>).</item>
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

		// Samples whose language name carries an example suffix (cURL_tag_names) are their own examples.
		var suffixed = codeSamples.Where(static c => c.Scenario is not null).ToArray();
		var attached = AttachCodeSamples(scenarios, [.. codeSamples.Where(static c => c.Scenario is null)]);
		return suffixed.Length == 0 ? attached : [.. attached, .. SuffixedScenarios(attached, suffixed)];
	}

	/// <summary>Links each sample's client label to the client's docs page where one exists for this product.</summary>
	private static List<ExampleScenario> WithClientDocs(IReadOnlyList<ExampleScenario> scenarios, string? productId) =>
		[
			.. scenarios.Select(
				s =>
					s with
					{
						CodeSamples =
						[
							.. s.CodeSamples.Select(c => c with { ClientDocsUrl = CodeSample.ClientDocsUrlFor(c.Language, productId) })
						]
					}
			)
		];

	private static List<ExampleScenario> AttachCodeSamples(List<ExampleScenario> scenarios, IReadOnlyList<CodeSample> codeSamples)
	{
		if (codeSamples.Count == 0)
			return scenarios;

		if (scenarios.Count == 0)
		{
			scenarios.Add(new ExampleScenario { Title = "Examples", TabId = "examples", CodeSamples = codeSamples });
			return scenarios;
		}

		var matchIndex = FindScenarioForCodeSamples(scenarios, codeSamples);
		if (matchIndex is { } match)
		{
			scenarios[match] = scenarios[match] with { CodeSamples = codeSamples, CodeSamplesIncludeRequest = true };
			return scenarios;
		}

		// Samples written for a body no named example has: keep them as their own first example instead of
		// pinning them to an unrelated one. Body-less samples (e.g. synthetic curl) still join the first example.
		if (SamplesCarryABody(codeSamples) && scenarios.Any(static s => !string.IsNullOrWhiteSpace(s.RequestJson)))
		{
			scenarios.Insert(
				0,
				new ExampleScenario
				{
					Title = "Example",
					TabId = UniqueTabId("example", scenarios),
					CodeSamples = codeSamples,
					Responses = scenarios[0].Responses.Where(static r => !r.StatusCode.StartsWith('2')).ToArray()
				}
			);
			return scenarios;
		}

		scenarios[0] = scenarios[0] with { CodeSamples = codeSamples };
		return scenarios;
	}

	/// <summary>One example per suffix, sharing the first example's responses; the samples already say how to call it.</summary>
	private static IEnumerable<ExampleScenario> SuffixedScenarios(List<ExampleScenario> scenarios, IReadOnlyList<CodeSample> suffixed)
	{
		var responses = scenarios.Count > 0 ? scenarios[0].Responses : [];
		var added = new List<ExampleScenario>();
		foreach (var group in suffixed.GroupBy(static c => c.Scenario!, StringComparer.OrdinalIgnoreCase))
		{
			var title = HumanizeExampleKey(group.Key);
			added.Add(new ExampleScenario
			{
				Title = title,
				TabId = UniqueTabId(ToTabId(title, scenarios.Count + added.Count), [.. scenarios, .. added]),
				CodeSamples = [.. group],
				CodeSamplesIncludeRequest = true,
				Responses = responses
			});
		}
		return added;
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
					RequestExternalValue = example.ExternalValue,
					RequestLine = example.RequestLine
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
					RequestExternalValue = example.ExternalValue,
					RequestLine = example.RequestLine
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

	private static int? FindScenarioForCodeSamples(IReadOnlyList<ExampleScenario> scenarios, IReadOnlyList<CodeSample> codeSamples)
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

		return null;
	}

	private static bool SamplesCarryABody(IReadOnlyList<CodeSample> codeSamples) =>
		codeSamples.Any(static s => s.IsConsole ? s.Source.Trim().Contains('\n') : s.Source.Contains(" -d ", StringComparison.Ordinal));

	private static string UniqueTabId(string id, IReadOnlyList<ExampleScenario> scenarios)
	{
		var candidate = id;
		for (var i = 1; scenarios.Any(s => s.TabId == candidate); i++)
			candidate = $"{id}-{i}";
		return candidate;
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
			var media = response?.Content?.FirstOrDefault().Value;
			var named = MapExamples(media?.Examples, renderMarkdown, statusCode);
			if (named.Count > 0)
			{
				list.AddRange(named);
				continue;
			}

			if (media?.Example is not { } example || JsonNullSentinel.IsJsonNullSentinel(example))
				continue;

			var json = example.ToString();
			if (string.IsNullOrWhiteSpace(json))
				continue;

			list.Add(new ExampleDisplay(statusCode, null, json, null, statusCode));
		}

		return list;
	}

	/// <summary>With <paramref name="endpoint"/>, a request description's <c>Run `METHOD path`</c> line follows the path's dominant method.</summary>
	private static IReadOnlyList<ExampleDisplay> MapExamples(
		IDictionary<string, IOpenApiExample>? examples,
		Func<string?, HtmlString> renderMarkdown,
		string? statusCode = null,
		OperationEndpoint? endpoint = null
	) =>
		examples is null
			? []
			: examples.Select(e =>
			{
				var description = string.IsNullOrWhiteSpace(e.Value?.Description) ? null : e.Value.Description.Trim();
				if (description is not null && endpoint is not null)
					description = SampleMethods.AlignDescription(description, endpoint);
				return new ExampleDisplay(
					string.IsNullOrEmpty(e.Value?.Summary) ? HumanizeExampleKey(e.Key) : e.Value.Summary,
					string.IsNullOrEmpty(description) ? null : renderMarkdown(description),
					e.Value?.Value?.ToString(),
					string.IsNullOrEmpty(e.Value?.ExternalValue) ? null : e.Value.ExternalValue,
					statusCode,
					description
				)
				{ RequestLine = GeneratedCodeSamples.ParseRequestLine(description) };
			}).ToArray();

	/// <summary>
	/// Spec example keys stand in for a missing summary: <c>executeBuiltinEsqlToolRequest</c> reads as
	/// "Execute builtin ES|QL tool". A trailing Request/Response/Example word is dropped so a request and its
	/// response example still pair up by title. Keys that already contain spaces are kept as written.
	/// </summary>
	public static string HumanizeExampleKey(string key)
	{
		if (string.IsNullOrWhiteSpace(key) || key.Contains(' ', StringComparison.Ordinal))
			return key;

		var words = CamelCaseBoundary().Split(key.Replace('_', ' ').Replace('-', ' ')).Where(static w => w.Length > 0).ToList();
		if (words.Count > 1 && words[^1] is "Request" or "Response" or "Example")
			words.RemoveAt(words.Count - 1);

		var text = string.Join(
			' ',
			words.Select(
				static (w, i) => w.Equals("esql", StringComparison.OrdinalIgnoreCase)
					? "ES|QL"
					: i == 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w.ToLowerInvariant()
			)
		);
		return text.Length == 0 ? key : text;
	}

	[GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|\s+", RegexOptions.CultureInvariant)]
	private static partial Regex CamelCaseBoundary();

	private static ApiPathParameter BuildPathParameter(
		IOpenApiParameter parameter,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder,
		ApiRenderContext context,
		ApiSupplementalDoc? supplemental,
		OperationEndpoint endpoint
	)
	{
		var schema = parameter.Schema;
		var typeInfo = analyzer.GetTypeInfo(schema);
		var description = supplemental?.ParameterOr(parameter.Name ?? "", parameter.Description) ?? parameter.Description;
		var type = schema is not null ? builder.DescribePathParameter(schema) : null;
		var optional = parameter.Name is { } name && endpoint.OptionalPathParameters.Contains(name);
		// The type chip already lists X | X[]; a One of row would repeat those alternatives.
		var alternativesInType = type?.Text.Contains(" | ", StringComparison.Ordinal) == true;
		return new ApiPathParameter
		{
			Parameter = parameter,
			Type = type,
			EnumValues = typeInfo.EnumValues ?? [],
			UnionOptions = alternativesInType ? [] : UnionBadges(typeInfo),
			UnionLabel = SchemaHelpers.UnionLabel(typeInfo.UnionKeyword),
			DescriptionHtml = ApiMarkdown.Render(context, description),
			DescriptionMarkdown = description,
			Optional = optional,
			Required = parameter.Required && !optional
		};
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
		var typeInfo = analyzer.GetTypeInfo(schema);
		var description = supplemental?.ParameterOr(parameter.Name ?? "", parameter.Description) ?? parameter.Description;
		return new ApiQueryParameter
		{
			Parameter = parameter,
			Type = schema is not null ? builder.Describe(schema) : null,
			Constraints = schema is not null ? ApiPropertyTreeBuilder.BuildConstraints(schema) : [],
			EnumValues = typeInfo.EnumValues ?? [],
			UnionOptions = UnionBadges(typeInfo),
			UnionLabel = SchemaHelpers.UnionLabel(typeInfo.UnionKeyword),
			DescriptionHtml = ApiMarkdown.Render(context, description),
			DescriptionMarkdown = description
		};
	}

	private static UnionBadge[] UnionBadges(TypeInfo typeInfo)
	{
		var names = (typeInfo.UnionOptions ?? []).Select(o => o.Name);
		return names
			.Where(n => !string.IsNullOrEmpty(n))
			.Select(n => new UnionBadge(n, ApiPropertyTreeBuilder.IsTypeOptionBadge(n)))
			.ToArray();
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
		var (properties, unionVariants) = BuildBodyContent(responseSchema, scope, analyzer, builder);
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

	/// <summary>
	/// The property list or the variant list of a request or response body. A union whose properties come only from an
	/// <c>allOf</c> lists its variants, which already carry those shared properties. A union that declares properties
	/// itself keeps listing them, as property rows do.
	/// </summary>
	internal static (ApiPropertyList? Properties, ApiUnionVariants? UnionVariants) BuildBodyContent(
		IOpenApiSchema schema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		var declaresProperties = (analyzer.ResolveSchema(schema) ?? schema).Properties is { Count: > 0 };
		var variants = analyzer.GetTypeInfo(schema).IsUnion && !declaresProperties
			? BuildTopLevelUnionVariants(schema, scope, analyzer, builder)
			: null;
		return variants is not null ? (null, variants) : (builder.BuildPropertyList(schema, scope), null);
	}

	internal static ApiUnionVariants? BuildTopLevelUnionVariants(
		IOpenApiSchema bodySchema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		var typeInfo = analyzer.GetTypeInfo(bodySchema);
		if (typeInfo is not { IsUnion: true, UnionOptions.Count: > 0 })
			return null;

		var options = typeInfo.UnionOptions.Where(static o => o.Schema is not null).Select(o => DescribeMember(o, analyzer)).ToList();
		var variants = options.Count == 0 ? null : builder.BuildUnionVariants(options, scope, analyzer.GetUnionDiscriminator(bodySchema));
		return variants is null ? null : variants with { Label = SchemaHelpers.UnionLabel(typeInfo.UnionKeyword) };
	}

	/// <summary>
	/// Classifies a union member the way a body-level list always has. A merged <c>allOf</c> variant has no <c>$ref</c> of its own,
	/// so it keeps the name and <c>$ref</c> of the option it came from; discriminator mappings match on that <c>$ref</c>.
	/// </summary>
	private static UnionOption DescribeMember(UnionOption option, SchemaAnalyzer analyzer)
	{
		if (option.Schema is MergedVariantSchema)
			return option;

		var info = analyzer.GetTypeInfo(option.Schema);
		return new UnionOption(info.TypeName, info.SchemaRef, info.IsObject, option.Schema, info.IsArray);
	}

	private static IReadOnlyList<string> NamesOf(IEnumerable<string?> names) => [.. names.OfType<string>().Where(static n => n.Length > 0)];

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
