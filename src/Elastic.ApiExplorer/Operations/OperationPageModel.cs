// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

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

	/// <summary>The request body is a union, so <see cref="RequestPropertyNames"/> names its variants, not its fields.</summary>
	public bool RequestNamesAreVariants => RequestProperties is null && RequestUnionVariants is { Variants.Count: > 0 };

	public IReadOnlyList<string> RequestPropertyNames =>
		NamesOf(
			RequestProperties is not null
				? RequestProperties.Items.Select(static p => p.Name)
				: ApiBodyContent.VariantNames(RequestUnionVariants)
		);

	public required string? DescriptionMarkdown { get; init; }
	public required IReadOnlyList<ApiPostSection> PostSections { get; init; }
	public required string RequestContentType { get; init; }
	public required ApiPropertyList? RequestProperties { get; init; }

	/// <summary>The variants of a request body that is itself a <c>oneOf</c>/<c>anyOf</c>; listed below <see cref="RequestProperties"/> when it has both.</summary>
	public ApiUnionVariants? RequestUnionVariants { get; init; }

	/// <summary>Which of the request body's fields it needs, when its <c>oneOf</c>/<c>anyOf</c> only lists <c>required</c> sets.</summary>
	public RequiredAlternatives? RequestRequires { get; init; }

	/// <summary>Whether the request body lists properties, variants, or both, rather than only its type.</summary>
	public bool HasRequestBody => RequestProperties is not null || RequestUnionVariants is { Variants.Count: > 0 };
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
		var request = requestSchema is not null
			? ApiBodyContent.Build(requestSchema, requestScope, analyzer, builder)
			: ApiBodyContent.Empty;

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
			RequestProperties = request.Properties,
			RequestUnionVariants = request.UnionVariants,
			RequestRequires = request.Requires,
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

	private static IReadOnlyList<string> NamesOf(IEnumerable<string?> names) => [.. names.OfType<string>().Where(static n => n.Length > 0)];
}
