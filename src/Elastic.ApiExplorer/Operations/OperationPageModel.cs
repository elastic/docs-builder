// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Site.ApplicabilityBadges;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// Everything structural an operation page renders, precomputed before the view runs.
/// Scalar values (summary, descriptions, parameter names) are read off the raw operation in the view.
/// </summary>
public partial record OperationPageModel
{
	public required ApplicableToViewModel? Availability { get; init; }
	public required ExternalDocLink? ExternalDocs { get; init; }
	public required IList<OpenApiServer>? Servers { get; init; }
	/// <summary>Every route and method of the operation, merged into display rows.</summary>
	public required OperationEndpoint Endpoint { get; init; }
	public required ApiPropertyList PathParameters { get; init; }
	public required ApiPropertyList QueryParameters { get; init; }
	public required ApiPropertyList HeaderParameters { get; init; }

	public IReadOnlyList<string> PathParameterNames => NamesOf(PathParameters.Items.Select(static p => p.Name));

	public IReadOnlyList<string> QueryParameterNames => NamesOf(QueryParameters.Items.Select(static q => q.Name));

	public IReadOnlyList<string> HeaderParameterNames => NamesOf(HeaderParameters.Items.Select(static h => h.Name));

	/// <summary>The parameter sections the page lists, in page order; a location with no parameters has none.</summary>
	public IReadOnlyList<ParameterListSection> ParameterSections =>
		[
			.. new[]
			{
				new ParameterListSection(
					ParameterLocation.Path,
					new ParamSectionHeader("Path parameters", "path-params", PathParameterNames, SummaryStyle.List),
					"path-params-list",
					PathParameters,
					"Path parameters"
				),
				new ParameterListSection(
					ParameterLocation.Query,
					new ParamSectionHeader("Query parameters", "query-params", QueryParameterNames, SummaryStyle.List),
					"query-params-list",
					QueryParameters,
					"Query string parameters"
				),
				new ParameterListSection(
					ParameterLocation.Header,
					new ParamSectionHeader("Header parameters", "header-params", HeaderParameterNames, SummaryStyle.List),
					"header-params-list",
					HeaderParameters,
					"Header parameters"
				)
			}.Where(static s => s.Parameters.Items.Count > 0)
		];

	/// <summary>
	/// A union body's names are its variants, a JSON body's its fields as an object, an array body's its items' fields, and any
	/// other body's a plain list.
	/// </summary>
	public SummaryStyle RequestSummaryStyle =>
		RequestProperties is null && RequestUnionVariants is { Variants.Count: > 0 }
			? SummaryStyle.Variants
			: PrimaryRequest is { } primary && RequestBodyExample.IsJson(primary.ContentType)
				? primary.IsArray ? SummaryStyle.ArrayOfFields : SummaryStyle.Fields
				: SummaryStyle.List;

	public IReadOnlyList<string> RequestPropertyNames =>
		NamesOf(
			RequestProperties is not null
				? RequestProperties.Items.Select(static p => p.Name)
				: ApiBodyContent.VariantNames(RequestUnionVariants)
		);

	public required string? DescriptionMarkdown { get; init; }
	public required IReadOnlyList<ApiPostSection> PostSections { get; init; }

	/// <summary>Every content type the request body declares; the first is the one the examples, samples, and field names follow.</summary>
	public required IReadOnlyList<ApiMediaContent> RequestContents { get; init; }

	/// <summary>The content type the examples, samples, and field names follow.</summary>
	public ApiMediaContent? PrimaryRequest => RequestContents is [var first, ..] ? first : null;

	public ApiPropertyList? RequestProperties => PrimaryRequest?.Properties;

	public ApiUnionVariants? RequestUnionVariants => PrimaryRequest?.UnionVariants;

	public required IReadOnlyList<ApiResponse> Responses { get; init; }
	public required IReadOnlyList<CodeSample> CodeSamples { get; init; }
	public required IReadOnlyList<ExampleDisplay> RequestExamples { get; init; }
	public required IReadOnlyList<ExampleDisplay> ResponseExamples { get; init; }
	public required bool ShowRequestExamples { get; init; }
	public required bool ShowResponseExamples { get; init; }
	public required IReadOnlyList<ExampleScenario> Scenarios { get; init; }

	/// <summary>The alternative ways to authenticate. Empty when the spec declares no schemes.</summary>
	public required IReadOnlyList<AuthRequirement> AuthRequirements { get; init; }

	/// <summary>Privileges the operation needs, from <c>x-req-auth</c> or lifted out of the description.</summary>
	public required IReadOnlyList<PrivilegeRow> RequiredPrivileges { get; init; }

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
			DesignSystem = context.BuildContext.Configuration.Features.NavigationPreviewEnabled,
			SchemaResolveCache = context.SchemaResolveCache
		};
		var builder = new ApiPropertyTreeBuilder(document, options);

		var siblings = context.CurrentNavigation is OperationNavigationItem { Siblings: var collapsed } ? collapsed : [];
		var endpoint = OperationEndpoint.Build(
			apiOperation,
			supplemental?.DescriptionOr(operation.Description) ?? operation.Description,
			siblings
		);
		var privileges = RequiredPrivilegesExtractor.Extract(operation, endpoint.Description, context.ApiExplorerLog, apiOperation.Route);
		var requestContentEntry = PrimaryRequestContent(operation.RequestBody);
		// Examples may sit on another content type than the schema does, as when the spec lists one only to attach them.
		var requestContentTypes = RequestContentTypes(operation.RequestBody, requestContentEntry);
		var requestExamples = MapRequestExamples(requestContentTypes, options.RenderMarkdown, endpoint);
		var codeSamples = SampleMethods.Align(
			ExampleCodeSamples.Combine(OpenApiExtensionReader.ParseCodeSamples(operation), requestContentTypes),
			endpoint
		);
		var servers = operation.Servers is { Count: > 0 } ? operation.Servers : document.Servers;
		var parameters = apiOperation.Parameters;
		var requestSchema = requestContentEntry?.Value?.Schema;
		var target = GeneratedCodeSamples.SampleTargetFor(context.Product?.Id);
		if (codeSamples.Count == 0)
		{
			// Built around the first example's body, so the samples attach to that example and carry its request.
			var body = requestExamples.FirstOrDefault(static e => !string.IsNullOrWhiteSpace(e.JsonValue))?.JsonValue;
			// Without an example the body comes from the schema, so the sample of an operation that takes one is not a bare request.
			body ??= RequestBodyExample.Build(analyzer, requestContentEntry?.Key, requestSchema);
			codeSamples = SyntheticCodeSamples.Create(new SyntheticSampleInput(apiOperation.OperationType, apiOperation.Route, operation)
			{
				Parameters = parameters,
				Servers = servers,
				HostVariable = target.HostVariable,
				Body = body,
				Security = OpenApiAuthSchemeResolver.CurlSchemes(operation, document),
				ConsolePrefix = target.ConsolePrefix
			});
		}

		var responseExamples = MapResponseExamples(operation.Responses, options.RenderMarkdown, analyzer);
		var scenarios = WithClientDocs(
			WithOperationIdentity(
				GeneratedCodeSamples.Fill(
					EnsureResponseTabs(BuildExampleScenarios(requestExamples, responseExamples, codeSamples), operation.Responses),
					codeSamples,
					endpoint,
					target.ConsolePrefix
				),
				apiOperation.OperationType.ToString().ToLowerInvariant(),
				apiOperation.Route
			),
			context.Product?.Id
		);
		ExternalDocLink? externalDocs = null;
		if (operation.ExternalDocs?.Url is not null)
		{
			var url = operation.ExternalDocs.Url.ToString();
			externalDocs = new ExternalDocLink(url, ApiPropertyTreeBuilder.IsElasticDocsUrl(url), operation.ExternalDocs.Description);
		}

		var propertyTrees = BuildPropertyTrees(apiOperation, analyzer, new ParameterRowSource(builder, supplemental, endpoint));

		return new OperationPageModel
		{
			Availability = AvailabilityBadge.FromOperation(
				operation,
				context.BuildContext.VersionsConfiguration,
				designSystem: context.BuildContext.Configuration.Features.NavigationPreviewEnabled
			),
			ExternalDocs = externalDocs,
			Servers = servers,
			Endpoint = endpoint,
			PathParameters = propertyTrees.Path,
			QueryParameters = propertyTrees.Query,
			HeaderParameters = propertyTrees.Header,
			RequestContents = propertyTrees.Request,
			DescriptionMarkdown = privileges.Description,
			PostSections = ApiPostSection.From(context, supplemental?.PostSections ?? []),
			Responses = propertyTrees.Responses,
			CodeSamples = codeSamples,
			RequestExamples = requestExamples,
			ResponseExamples = responseExamples,
			ShowRequestExamples = requestExamples.Count > 0 && scenarios.Any(static s => s.ShowRequest),
			ShowResponseExamples = responseExamples.Count > 0,
			Scenarios = scenarios,
			RequiredPrivileges = privileges.Rows,
			AuthRequirements = OpenApiAuthSchemeResolver.Resolve(operation, document)
		};
	}

	/// <summary>The request content entry with a schema, else the first entry, so a leading example-only entry does not hide the body.</summary>
	internal static KeyValuePair<string, IOpenApiMediaType>? PrimaryRequestContent(IOpenApiRequestBody? requestBody) =>
		requestBody?.Content is { Count: > 0 } content ? SchemaFirst(content).First() : null;

	private static IReadOnlyList<string> NamesOf(IEnumerable<string?> names) => [.. names.OfType<string>().Where(static n => n.Length > 0)];
}
