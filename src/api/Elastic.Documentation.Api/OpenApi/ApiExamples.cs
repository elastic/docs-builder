// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.Documentation.Api.AskAi;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;

namespace Elastic.Documentation.Api.OpenApi;

/// <summary>
/// Adds examples to the OpenAPI document: one or more example requests per operation, each with its response and a curl
/// <c>x-codeSamples</c> entry, example values on the parameters, and a problem example on every error response.
/// Response examples are real replies, trimmed, kept in <c>OpenApi/Examples</c> and checked against the response types by tests.
/// </summary>
internal static class ApiExamples
{
	/// <summary>The public host the examples call.</summary>
	internal const string Server = "https://www.elastic.co";

	private const string ResourcePrefix = "openapi-examples/";

	/// <summary>The cookie www.elastic.co's CDN requires on the API until the gate is removed.</summary>
	internal const string GateCookie = "feature_search_or_askai_enabled=true";

	/// <summary>One example request of an operation, with what it answers.</summary>
	internal sealed record Example(string Title, string Method, string PathAndQuery)
	{
		/// <summary>The JSON request body, for POST and PUT.</summary>
		public JsonNode? Body { get; init; }

		/// <summary>The embedded response example, a file name in <c>OpenApi/Examples</c> without <c>.json</c>.</summary>
		public string? Response { get; init; }

		/// <summary>A response that isn't JSON, such as the Ask AI event stream.</summary>
		public string? ResponseText { get; init; }

		/// <summary>The released Elastic CLI command that sends the same request, if there is one.</summary>
		public string? Cli { get; init; }
	}

	private const string FeedbackId = "4f9d2c1a-7b3e-4c6d-9a8f-2e1b0c5d7a63";
	private const string MessageId = "0b6f1e2d-3c4a-4b5d-8e7f-9a0b1c2d3e4f";
	private const string ConversationId = "7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f";

	internal static readonly IReadOnlyDictionary<string, Example[]> ByOperation = new Dictionary<string, Example[]>(StringComparer.Ordinal)
	{
		["search"] =
		[
			new("Search the documentation", "GET", "/docs/_api/v1/search?q=cluster%20health&size=1")
			{
				Response = "search",
				Cli = "elastic docs search \"cluster health\""
			}
		],
		["navigation-search"] =
		[
			new("Search page titles", "GET", "/docs/_api/v1/navigation-search?q=cluster%20health") { Response = "navigation-search" }
		],
		["list-changes"] =
		[
			new("Pages changed since a date", "GET", "/docs/_api/v1/changes?since=2026-11-17T00:00:00Z&size=1")
			{
				Response = "list-changes"
			}
		],
		["ask-ai-stream"] =
		[
			new("Ask a question", "POST", "/docs/_api/v1/ask-ai/stream")
			{
				Body = new JsonObject { ["message"] = "How do I check the health of a cluster?", ["conversationId"] = null },
				ResponseText = AskAiStream(),
				Cli = "elastic docs ask \"How do I check the health of a cluster?\""
			}
		],
		["ask-ai-message-feedback"] =
		[
			new("Rate an answer", "POST", "/docs/_api/v1/ask-ai/message-feedback")
			{
				Body = new JsonObject { ["messageId"] = MessageId, ["conversationId"] = ConversationId, ["reaction"] = "thumbsUp" }
			}
		],
		["put-page-feedback"] =
		[
			new("Rate a page", "PUT", $"/docs/_api/v1/page-feedback/{FeedbackId}")
			{
				Body = new JsonObject
				{
					["pageUrl"] = "/docs/deploy-manage/monitor/cluster-health",
					["pageTitle"] = "Cluster health",
					["reaction"] = "thumbsDown",
					["reasons"] = new JsonArray("missingInformation"),
					["reasonSetVersion"] = 1,
					["comment"] = "An example of the yellow status would help."
				}
			}
		],
		["delete-page-feedback"] = [new("Withdraw page feedback", "DELETE", $"/docs/_api/v1/page-feedback/{FeedbackId}")],
		["list-apis"] = [new("Every API", "GET", "/docs/_api/v1/apis") { Response = "list-apis" }],
		["get-api"] = [new("One API", "GET", "/docs/_api/v1/apis/elasticsearch") { Response = "get-api" }],
		["list-api-groups"] = [new("Groups of an API", "GET", "/docs/_api/v1/apis/elasticsearch/groups") { Response = "list-api-groups" }],
		["list-api-operations"] =
		[
			new("Search an API", "GET", "/docs/_api/v1/apis/elasticsearch/operations?q=cluster%20health&size=1")
			{
				Response = "list-api-operations.search"
			},
			new("Look up an endpoint", "GET", "/docs/_api/v1/apis/elasticsearch/operations?endpoint=GET%20%2F_cluster%2Fhealth&view=full")
			{
				Response = "list-api-operations.endpoint"
			},
			new("List a group", "GET", "/docs/_api/v1/apis/elasticsearch/operations?group=Cluster%20-%20Health")
			{
				Response = "list-api-operations.group"
			}
		],
		["get-api-operation"] =
		[
			new("One operation", "GET", "/docs/_api/v1/apis/elasticsearch/operations/cluster-health") { Response = "get-api-operation" }
		]
	};

	// The problem each error status answers with, unless the operation answers that status with its own type or detail
	// below. Clients match on the type, so an example shows a type the operation really returns. ApiProblems defines them.
	private static readonly Dictionary<int, (string Type, string Detail)> ProblemByStatus = new()
	{
		[StatusCodes.Status400BadRequest] = (ApiProblems.InvalidArguments, "A parameter is missing or does not parse."),
		[StatusCodes.Status404NotFound] = ("api-not-found", "No operations found for API 'nope'."),
		[StatusCodes.Status500InternalServerError] = (ApiProblems.InternalError, "An unexpected error occurred."),
		[StatusCodes.Status503ServiceUnavailable] =
			(ApiProblems.SearchUnavailable, "Search backend is temporarily unavailable (MaxTimeoutReached). Transient — retry in a few seconds.")
	};

	private const string FeedbackUnavailable = "The feedback could not be stored. Try again later.";

	private static readonly Dictionary<(string Operation, int Status), (string Type, string Detail)> ProblemByOperation = new()
	{
		[("search", 400)] = (ApiProblems.InvalidArguments, "Required parameter \"string query\" was not provided from query string."),
		[("navigation-search", 400)] =
			(ApiProblems.InvalidArguments, "api_key must be a lowercase API key, and api_tree 'main' or a major version with api_key."),
		[("list-changes", 400)] = (ApiProblems.InvalidArguments, "cursor is not a nextCursor that /changes returned."),
		[("ask-ai-stream", 400)] =
			(ApiProblems.InvalidArguments, "Failed to read parameter \"AskAiRequest askAiRequest\" from the request body as JSON."),
		[("ask-ai-message-feedback", 400)] =
			(ApiProblems.InvalidArguments, "Failed to read parameter \"AskAiMessageFeedbackRequest request\" from the request body as JSON."),
		[("ask-ai-message-feedback", 503)] = (ApiProblems.FeedbackUnavailable, FeedbackUnavailable),
		[("put-page-feedback", 400)] =
			(ApiProblems.InvalidArguments, "The page feedback is invalid. Check pageUrl, pageTitle, reaction, reasons and comment."),
		[("put-page-feedback", 503)] = (ApiProblems.FeedbackUnavailable, FeedbackUnavailable),
		[("delete-page-feedback", 503)] = (ApiProblems.FeedbackUnavailable, FeedbackUnavailable),
		[("get-api", 404)] = ("api-not-found", "No API 'nope'."),
		[("list-api-groups", 400)] = (ApiProblems.InvalidArguments, "Pass an API key, such as 'elasticsearch'."),
		[("list-api-operations", 400)] = (ApiProblems.InvalidArguments, "Pass q or endpoint, not both."),
		[("get-api-operation", 400)] = (ApiProblems.InvalidArguments, "Pass an API key, such as 'elasticsearch'."),
		[("get-api-operation", 404)] = ("operation-not-found", "No API operation matches nope in elasticsearch.")
	};

	/// <summary>The operation transformer that adds the examples.</summary>
	public static Task Apply(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken _)
	{
		AddProblemExamples(operation, operation.OperationId);
		if (operation.OperationId is not { } id || !ByOperation.TryGetValue(id, out var examples))
			return Task.CompletedTask;

		AddParameterExamples(operation, context.Description.RelativePath, examples[0].PathAndQuery);
		AddRequestBodyExamples(operation, examples);
		AddResponseExamples(operation, examples);
		operation.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
		operation.Extensions["x-codeSamples"] = new JsonNodeExtension(CodeSamples(examples));
		return Task.CompletedTask;
	}

	/// <summary>The <c>x-codeSamples</c> of the examples: a curl per example, and the Elastic CLI command where one exists.</summary>
	internal static JsonArray CodeSamples(IReadOnlyList<Example> examples)
	{
		var samples = new JsonArray();
		foreach (var example in examples)
		{
			// API Explorer ties a curl to its example by label once the operation has more than one.
			// JsonNode, not a generic Add<T>: the generic one can serialize any type, which trimming can't follow.
			samples.Add((JsonNode)new JsonObject { ["lang"] = "curl", ["label"] = example.Title, ["source"] = Curl(example) });
			if (example.Cli is { } cli)
				samples.Add((JsonNode)new JsonObject { ["lang"] = "Shell", ["label"] = "Elastic CLI", ["source"] = cli });
		}
		return samples;
	}

	/// <summary>A curl command for an example request, against <see cref="Server"/>.</summary>
	internal static string Curl(Example example)
	{
		var curl = new StringBuilder("curl");
		if (example.Method != "GET")
			_ = curl.Append(" -X ").Append(example.Method);
		_ = curl.Append(" '").Append(Server).Append(example.PathAndQuery).Append('\'');
		// shortcut: www.elastic.co's CDN still gates the API on this cookie; drop it once elastic/docs-infra#523 is applied.
		_ = curl.Append(" \\\n  -b '").Append(GateCookie).Append('\'');
		if (example.Body is { } body)
			_ = curl.Append(" \\\n  -H 'Content-Type: application/json' \\\n  -d '").Append(body.ToJsonString()).Append('\'');
		return curl.ToString();
	}

	/// <summary>Reads a response example from the files embedded from <c>OpenApi/Examples</c>.</summary>
	internal static JsonNode ReadResponse(string name)
	{
		using var stream = typeof(ApiExamples).Assembly.GetManifestResourceStream(ResourcePrefix + name + ".json")
			?? throw new InvalidOperationException($"No OpenAPI response example '{name}' is embedded.");
		return JsonNode.Parse(stream) ?? throw new InvalidOperationException($"The OpenAPI response example '{name}' is empty.");
	}

	private static void AddParameterExamples(OpenApiOperation operation, string? template, string pathAndQuery)
	{
		if (operation.Parameters is not { Count: > 0 } parameters)
			return;
		var separator = pathAndQuery.IndexOf('?');
		var path = separator < 0 ? pathAndQuery : pathAndQuery[..separator];
		var values = separator < 0
			? []
			: QueryHelpers.ParseQuery(pathAndQuery[separator..]).ToDictionary(p => p.Key, p => p.Value.ToString());

		// Route values sit where the template has {name} or {name:constraint}.
		var templateSegments = (template ?? "").Trim('/').Split('/');
		var pathSegments = path.Trim('/').Split('/');
		for (var i = 0; i < templateSegments.Length && i < pathSegments.Length; i++)
		{
			if (templateSegments[i] is ['{', .., '}'] segment)
				values[segment[1..^1].Split(':')[0]] = Uri.UnescapeDataString(pathSegments[i]);
		}

		foreach (var parameter in parameters.OfType<OpenApiParameter>())
		{
			if (parameter.Name is { } name && values.TryGetValue(name, out var value))
				parameter.Example = int.TryParse(value, out var number) ? JsonValue.Create(number) : JsonValue.Create(value);
		}
	}

	private static void AddRequestBodyExamples(OpenApiOperation operation, IReadOnlyList<Example> examples)
	{
		if (operation.RequestBody?.Content is not { } content || !content.TryGetValue("application/json", out var media))
			return;
		if (examples.FirstOrDefault(e => e.Body is not null)?.Body is { } body && media is OpenApiMediaType mediaType)
			mediaType.Example = body.DeepClone();
	}

	private static void AddResponseExamples(OpenApiOperation operation, IReadOnlyList<Example> examples)
	{
		if (operation.Responses is null)
			return;
		foreach (var (status, response) in operation.Responses)
		{
			if (!status.StartsWith('2') || response.Content is not { } content)
				continue;
			foreach (var (contentType, media) in content)
			{
				if (media is not OpenApiMediaType mediaType)
					continue;
				var values = examples
					.Select(
						e =>
							(e.Title, Value: contentType == "application/json" && e.Response is { } file
								? ReadResponse(file)
								: e.ResponseText is { } text ? JsonValue.Create(text) : null)
					)
					.Where(e => e.Value is not null)
					.ToList();
				if (values.Count == 1)
					mediaType.Example = values[0].Value;
				else if (values.Count > 1)
				{
					mediaType.Examples = new Dictionary<string, IOpenApiExample>(StringComparer.Ordinal);
					foreach (var (title, value) in values)
						mediaType.Examples[title] = new OpenApiExample { Summary = title, Value = value };
				}
			}
		}
	}

	private static void AddProblemExamples(OpenApiOperation operation, string? operationId)
	{
		if (operation.Responses is null)
			return;
		foreach (var (status, response) in operation.Responses)
		{
			if (!int.TryParse(status, out var code) || response.Content is not { } content)
				continue;
			if (
				!(operationId is not null && ProblemByOperation.TryGetValue((operationId, code), out var problem))
				&& !ProblemByStatus.TryGetValue(code, out problem)
			)
				continue;
			if (content.TryGetValue("application/problem+json", out var media) && media is OpenApiMediaType mediaType)
				mediaType.Example = Problem(code, problem.Type, problem.Detail);
		}
	}

	/// <summary>An RFC 9457 problem as the API writes it.</summary>
	internal static JsonObject Problem(int status, string type, string detail) =>
		new()
		{
			["type"] = ApiProblems.TypeUri(type),
			["title"] = ApiProblems.TitleOf(type),
			["status"] = status,
			["detail"] = detail,
			["traceId"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
		};

	// The events a short answer streams, written by the same JSON context as the API's stream transformer.
	private static string AskAiStream()
	{
		AskAiEvent[] events =
		[
			new AskAiEvent.ConversationStart("1", 1763380000000, ConversationId),
			new AskAiEvent.SearchToolCall("2", 1763380000400, "call_1", "check cluster health"),
			new AskAiEvent.MessageChunk("3", 1763380001200, "Call `GET /_cluster/health`. "),
			new AskAiEvent.MessageChunk("4", 1763380001300, "A `green` status means every shard is assigned."),
			new AskAiEvent.ConversationEnd("5", 1763380001500)
		];
		return string.Concat(events.Select(e => $"data: {JsonSerializer.Serialize(e, AskAiEventJsonContext.Default.AskAiEvent)}\n\n"));
	}
}
