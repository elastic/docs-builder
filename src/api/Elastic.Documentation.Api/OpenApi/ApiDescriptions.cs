// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.PageFeedback;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Elastic.Documentation.Api.OpenApi;

/// <summary>
/// The prose of the OpenAPI document that doc comments can't carry: tags, operations, their parameters and responses, and the
/// problem schema. The endpoints are lambdas, and <see cref="ProblemDetails"/> belongs to ASP.NET; the response and request
/// types describe themselves through their doc comments.
/// </summary>
internal static class ApiDescriptions
{
	internal static readonly IReadOnlyDictionary<string, string> Tags = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["Search"] = "Search the documentation on elastic.co/docs, by full text or by page title.",
		["Ask AI"] = "Ask questions about Elastic in plain language and rate the answers.",
		["Changes"] = "Follow which documentation pages changed, to keep a copy or an index up to date.",
		["Page feedback"] = "Ratings that readers leave on documentation pages. Browser only: requests need an AWS WAF token.",
		["API reference"] = "The REST APIs that API Explorer documents, and their operations. Every operation lookup names its API: "
			+ "many operation ids and routes exist in several APIs, such as elasticsearch and elasticsearch-serverless."
	};

	internal static readonly IReadOnlyDictionary<string, string> Operations = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["search"] = "Searches every documentation page by full text, combining keyword and semantic ranking. "
			+ "Filter by type, navigation section, deployment, product or version, and sort by relevance, date or title. "
			+ "Matched words come back wrapped in `<mark>` tags.",
		["navigation-search"] = "Searches page titles and the navigation, as the docs site's search box does. "
			+ "On an API Explorer page, pass `api_key` and `api_tree` to rank that API's operations first.",
		["list-changes"] = "Lists the pages whose content changed since a date, a batch at a time. "
			+ "Pass the `nextCursor` of a reply as `cursor` to read the next batch, until `hasMore` is false.",
		["ask-ai-stream"] = "Answers a question with Elastic's documentation, as a stream of server-sent events. "
			+ "Each `data:` line is a JSON event: `conversation_start`, search and tool calls, `message_chunk` parts of the answer, "
			+ "and `conversation_end`. Pass the conversation id to ask a follow-up.",
		["ask-ai-message-feedback"] = "Records whether an Ask AI answer helped.",
		["put-page-feedback"] = "Records or replaces a reader's rating of a page. The id is chosen by the client, so sending it again "
			+ "updates the same feedback.",
		["delete-page-feedback"] = "Deletes a reader's rating of a page.",
		["list-apis"] = "Lists every API that API Explorer documents, with the keys and versions the other requests take.",
		["get-api"] = "Reads one API by its key.",
		["list-api-groups"] = "Counts an API's operations by group, without listing them. Deprecated operations count only with "
			+ "`include_deprecated=true`, as in a listing, so a group's count is its listing's `totalHits`.",
		["list-api-operations"] = "Reads an API's operations in one of three ways. With `q`, it searches them by words, a route such as "
			+ "`GET /_cluster/health`, or a field path. With `endpoint`, it finds the operations that serve a request, such as "
			+ "`GET /my-index/_search`, best route first; add `view=full` for their fields. With neither, it lists them by path, "
			+ "one `group` at a time if you like.",
		["get-api-operation"] = "Reads one operation of an API, with its routes, versions and every documented field."
	};

	// Parameters by name; an operation can override one for its own meaning.
	private static readonly Dictionary<string, string> Parameters = new(StringComparer.Ordinal)
	{
		["q"] = "The search terms.",
		["page"] = "The page to return, from 1.",
		["size"] = "The number of results per page, from 1 to 50.",
		["type"] = "Only results of this type, such as `docs` or `api`. Repeat it to allow several.",
		["section"] = "Only pages in this navigation section, such as `reference`. Repeat it to allow several.",
		["deployment"] = "Only pages that apply to this deployment type. Repeat it to allow several.",
		["product"] = "Only pages about this product id, such as `elasticsearch`. Repeat it to require several.",
		["version"] = "The version to read, such as `main` or `9`. Without it, the current version.",
		["sort"] = "`relevance` (the default), `recent` or `alpha`.",
		["api_key"] = "The API of the API Explorer page the search runs on, such as `elasticsearch`.",
		["api_tree"] = "The version tree of that page, `main` or a major such as `9`. It needs `api_key`.",
		["since"] = "Pages changed at or after this date and time, in ISO 8601.",
		["cursor"] = "The `nextCursor` of the previous reply.",
		["api"] = "The API key, such as `elasticsearch`. `GET /docs/_api/v1/apis` lists them.",
		["group"] = "Only the operations of this group, such as `Cluster`. Case doesn't matter.",
		["endpoint"] = "A request as you would send it, such as `GET /my-index/_search`, with or without the method.",
		["include_deprecated"] = "Include deprecated operations. The default is `false`.",
		["view"] = "`basic` (the default) or `full`, which adds each result's versions and fields. It needs `endpoint`.",
		["include_body"] = "Include the operation page's text. In a lookup by endpoint, it needs `view=full`.",
		["operationId"] = "The operation id, such as `cluster-health`, unique within the API.",
		["feedbackId"] = "The id of the feedback, a UUID the client chooses."
	};

	private static readonly Dictionary<(string Operation, string Parameter), string> ParameterOverrides = new()
	{
		[("list-api-operations", "q")] = "Words, a route such as `GET /_cluster/health`, or a field path. End a field with `.`, "
			+ "as in `retriever.`, to list its child fields.",
		[("list-api-operations", "size")] = "The number of operations per page, from 1 to 50.",
		[("list-api-operations", "page")] = "The page of a search or listing to return, from 1.",
		[("navigation-search", "type")] = "Only results of this type, such as `docs` or `api`.",
		[("list-changes", "size")] = "The number of pages per batch."
	};

	// A value for every parameter; an example request in ApiExamples replaces it with the value that request sends.
	private static readonly Dictionary<string, Func<JsonNode>> ParameterExamples = new(StringComparer.Ordinal)
	{
		["q"] = () => "cluster health",
		["page"] = () => 1,
		["size"] = () => 10,
		["type"] = () => "docs",
		["section"] = () => new JsonArray((JsonNode)"reference"),
		["deployment"] = () => new JsonArray((JsonNode)"self"),
		["product"] = () => new JsonArray((JsonNode)"elasticsearch"),
		["version"] = () => "9",
		["sort"] = () => "relevance",
		["api_key"] = () => "elasticsearch",
		["api_tree"] = () => "9",
		["since"] = () => "2026-11-17T00:00:00Z",
		["cursor"] = () => "WzE3OTQ4OTYyMTE3NzIsIi9kb2NzL3JlZmVyZW5jZS9vcGVudGVsZW1ldHJ5L2Vkb3Qtc2Rrcy9qYXZhL21pZ3JhdGlvbiJd",
		["api"] = () => "elasticsearch",
		["group"] = () => "Cluster",
		["endpoint"] = () => "GET /_cluster/health",
		["include_deprecated"] = () => false,
		["view"] = () => "full",
		["include_body"] = () => true,
		["operationId"] = () => "cluster-health",
		["feedbackId"] = () => "4f9d2c1a-7b3e-4c6d-9a8f-2e1b0c5d7a63"
	};

	// The search's type filter takes several values; navigation search's takes one.
	private static readonly Dictionary<(string Operation, string Parameter), Func<JsonNode>> ParameterExampleOverrides = new()
	{
		[("search", "type")] = () => new JsonArray((JsonNode)"docs")
	};

	private static readonly Dictionary<string, string> SuccessByOperation = new(StringComparer.Ordinal)
	{
		["search"] = "The matching pages, best first.",
		["navigation-search"] = "The matching pages and API operations, best first.",
		["list-changes"] = "A batch of changed pages.",
		["ask-ai-stream"] = "The answer, as server-sent events.",
		["ask-ai-message-feedback"] = "The rating was stored.",
		["put-page-feedback"] = "The feedback was stored.",
		["delete-page-feedback"] = "The feedback was deleted.",
		["list-apis"] = "Every API.",
		["get-api"] = "The API.",
		["list-api-groups"] = "The API's groups with their operation counts.",
		["list-api-operations"] = "A page of operations. A lookup that matches nothing is an empty page, not a 404.",
		["get-api-operation"] = "The operation with its fields."
	};

	private static readonly Dictionary<string, string> ErrorByStatus = new(StringComparer.Ordinal)
	{
		["400"] = "A parameter is missing, doesn't parse, or contradicts another one: an `invalid-arguments` problem.",
		["404"] = "Nothing matches: an `api-not-found`, `group-not-found` or `operation-not-found` problem.",
		["500"] = "An unexpected failure: an `internal-error` problem. Its detail never names the cause; quote the `traceId`.",
		["503"] = "A backend is temporarily unavailable: a `search-unavailable` or `feedback-unavailable` problem. "
			+ "Retry after the `Retry-After` seconds."
	};

	/// <summary>Adds the tag descriptions.</summary>
	public static void ApplyToDocument(OpenApiDocument document)
	{
		foreach (var tag in document.Tags ?? Enumerable.Empty<OpenApiTag>())
		{
			if (tag.Name is { } name && Tags.TryGetValue(name, out var description))
				tag.Description = description;
		}
	}

	/// <summary>Adds the operation, parameter and response descriptions.</summary>
	public static void ApplyToOperation(OpenApiOperation operation)
	{
		if (operation.OperationId is not { } id)
			return;
		if (Operations.TryGetValue(id, out var description))
			operation.Description = description;

		foreach (var parameter in (operation.Parameters ?? []).OfType<OpenApiParameter>())
		{
			if (parameter.Name is not { } name)
				continue;
			if (ParameterOverrides.TryGetValue((id, name), out var text) || Parameters.TryGetValue(name, out text))
				parameter.Description = text;
			if (ParameterExampleOverrides.TryGetValue((id, name), out var example) || ParameterExamples.TryGetValue(name, out example))
				parameter.Example = example();
		}

		foreach (var (status, response) in operation.Responses ?? [])
		{
			if (response is not OpenApiResponse openApiResponse)
				continue;
			if (status.StartsWith('2') && SuccessByOperation.TryGetValue(id, out var success))
				openApiResponse.Description = success;
			else if (ErrorByStatus.TryGetValue(status, out var error))
				openApiResponse.Description = error;
		}
	}

	// Examples for types whose properties can't carry an <example> doc comment: positional records and ASP.NET's
	// ProblemDetails. The values match the request examples in ApiExamples.
	private static readonly Dictionary<Type, Dictionary<string, JsonNode>> PropertyExamples = new()
	{
		[typeof(AskAiRequest)] = new()
		{
			["message"] = "How do I check the health of a cluster?",
			["conversationId"] = "7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f"
		},
		[typeof(AskAiMessageFeedbackRequest)] = new()
		{
			["messageId"] = "0b6f1e2d-3c4a-4b5d-8e7f-9a0b1c2d3e4f",
			["conversationId"] = "7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f"
		},
		[typeof(PageFeedbackRequest)] = new()
		{
			["pageUrl"] = "/docs/deploy-manage/monitor/cluster-health",
			["pageTitle"] = "Cluster health",
			["reasons"] = new JsonArray((JsonNode)"missingInformation"),
			["reasonSetVersion"] = 1,
			["comment"] = "An example of the yellow status would help."
		},
		[typeof(ProblemDetails)] = new()
		{
			["type"] = ApiProblems.TypeUri(ApiProblems.InvalidArguments),
			["title"] = ApiProblems.TitleOf(ApiProblems.InvalidArguments),
			["status"] = 400,
			["detail"] = "Pass q or endpoint, not both.",
			["instance"] = "/docs/_api/v1/apis/elasticsearch/operations"
		}
	};

	/// <summary>Adds the examples that doc comments can't carry, and describes the RFC 9457 problem every error answers with.</summary>
	public static Task ApplyToSchema(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken _)
	{
		if (schema.Properties is not { } properties)
			return Task.CompletedTask;
		if (PropertyExamples.TryGetValue(context.JsonTypeInfo.Type, out var examples))
		{
			foreach (var (name, example) in examples)
			{
				if (properties.TryGetValue(name, out var property) && property is OpenApiSchema propertySchema)
					propertySchema.Examples = [example.DeepClone()];
			}
		}
		if (context.JsonTypeInfo.Type != typeof(ProblemDetails))
			return Task.CompletedTask;
		schema.Description = "An RFC 9457 problem. Match on `type`; `title` and `detail` are for people.";
		Describe(properties, "type", "The problem type, a URI that resolves to its description on the REST API page.");
		Describe(properties, "title", "The problem type's name; the same for every occurrence.");
		Describe(properties, "status", "The HTTP status code of the response.");
		Describe(properties, "detail", "What went wrong in this request.");
		Describe(properties, "instance", "The request the problem is about, when known.");
		return Task.CompletedTask;
	}

	private static void Describe(IDictionary<string, IOpenApiSchema> properties, string name, string description)
	{
		if (properties.TryGetValue(name, out var property) && property is OpenApiSchema schema)
			schema.Description = description;
	}
}
