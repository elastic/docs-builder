// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.OpenApi;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Documentation.Api.Tests.Fixtures;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.ApiReference;

namespace Elastic.Documentation.Api.Tests;

/// <summary>The examples in the OpenAPI document must stay true to the API they describe.</summary>
public class OpenApiExamplesTests
{
	private static readonly Lazy<ApiWebApplicationFactory> SharedFactory = new(() => new ApiWebApplicationFactory());

	[After(Class)]
	public static void DisposeSharedFactory()
	{
		if (SharedFactory.IsValueCreated)
			SharedFactory.Value.Dispose();
	}

	// The type each operation answers with on success.
	private static readonly Dictionary<string, Type> ResponseTypes = new(StringComparer.Ordinal)
	{
		["search"] = typeof(FullSearchResponse),
		["navigation-search"] = typeof(NavigationSearchResponse),
		["list-changes"] = typeof(ChangesResponse),
		["list-apis"] = typeof(ApiListResponse),
		["get-api"] = typeof(ApiSummary),
		["list-api-groups"] = typeof(ApiOperationListResponse),
		["list-api-operations"] = typeof(ApiOperationCollectionResponse),
		["get-api-operation"] = typeof(ApiOperationResponse)
	};

	private static readonly Dictionary<string, Type> RequestTypes = new(StringComparer.Ordinal)
	{
		["ask-ai-stream"] = typeof(AskAiRequest),
		["ask-ai-message-feedback"] = typeof(AskAiMessageFeedbackRequest),
		["put-page-feedback"] = typeof(PageFeedbackRequest)
	};

	// The API's own JSON options, refusing a property its types don't have, so an example can't carry a stale field.
	private static readonly JsonSerializerOptions Strict = new(ApiJsonContext.Default.Options)
	{
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
	};

	private static async Task<IReadOnlyList<(string Method, string Path, JsonElement Operation)>> Operations()
	{
		using var client = SharedFactory.Value.CreateClient();
		using var response = await client.GetAsync("/docs/_api/v1/openapi.json", TestContext.Current!.Execution.CancellationToken);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var document = JsonElement.Parse(await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken));
		return [
			.. document
				.GetProperty("paths")
				.EnumerateObject()
				.SelectMany(
					path => path
						.Value
						.EnumerateObject()
						.Where(op => op.Name is "get" or "post" or "put" or "delete")
						.Select(op => (op.Name.ToUpperInvariant(), path.Name, op.Value))
				)
		];
	}

	private static IEnumerable<JsonElement> ExamplesOf(JsonElement media) =>
		media.TryGetProperty("example", out var example)
			? [example]
			: media.TryGetProperty("examples", out var examples) ? examples.EnumerateObject().Select(e => e.Value.GetProperty("value")) : [];

	[Test]
	public async Task EveryOperation_HasACurlSampleForItsOwnRoute()
	{
		foreach (var (method, path, operation) in await Operations())
		{
			var samples = operation.GetProperty("x-codeSamples").EnumerateArray().ToList();
			var curls = samples
				.Where(s => s.GetProperty("lang").GetString() == "curl")
				.Select(s => s.GetProperty("source").GetString()!)
				.ToList();
			curls.Should().NotBeEmpty(path);

			// The route template, with each {parameter} as one segment, against the public server.
			var route = new System.Text.RegularExpressions.Regex(
				"^curl" + (method == "GET" ? "" : $" -X {method}") + " '" + System
					.Text
					.RegularExpressions
					.Regex
					.Escape(ApiExamples.Server) + System
					.Text
					.RegularExpressions
					.Regex
					.Replace(System.Text.RegularExpressions.Regex.Escape(path), @"\\\{[^/]+?}", "[^/?']+") + @"(\?[^']*)?'"
			);
			curls.Should().AllSatisfy(curl => route.IsMatch(curl).Should().BeTrue($"{curl} calls {method} {path}"));
		}
	}

	[Test]
	public async Task EveryResponseExample_ReadsAsItsOperationsResponseType()
	{
		var checkedOperations = new List<string>();
		foreach (var (_, _, operation) in await Operations())
		{
			var id = operation.GetProperty("operationId").GetString()!;
			if (
				!operation.GetProperty("responses").TryGetProperty("200", out var ok)
				|| !ok.TryGetProperty("content", out var content)
				|| !content.TryGetProperty("application/json", out var media)
			)
				continue;

			ResponseTypes.Should().ContainKey(id, "every JSON response needs an example type to check it against");
			var examples = ExamplesOf(media).ToList();
			examples.Should().NotBeEmpty(id);
			foreach (var example in examples)
			{
				var read = () => example.Deserialize(Strict.GetTypeInfo(ResponseTypes[id]));
				read.Should().NotThrow($"the {id} example must match {ResponseTypes[id].Name}").Which.Should().NotBeNull();
			}
			checkedOperations.Add(id);
		}
		checkedOperations.Should().BeEquivalentTo(ResponseTypes.Keys);
	}

	[Test]
	public async Task EveryRequestBodyExample_ReadsAsItsRequestType()
	{
		var checkedOperations = new List<string>();
		foreach (var (_, _, operation) in await Operations())
		{
			if (!operation.TryGetProperty("requestBody", out var body))
				continue;
			var id = operation.GetProperty("operationId").GetString()!;
			var example = body.GetProperty("content").GetProperty("application/json").GetProperty("example");
			var read = () => example.Deserialize(Strict.GetTypeInfo(RequestTypes[id]));
			read.Should().NotThrow($"the {id} body must match {RequestTypes[id].Name}");
			checkedOperations.Add(id);
		}
		checkedOperations.Should().BeEquivalentTo(RequestTypes.Keys);
	}

	[Test]
	public async Task AskAiStreamExample_IsServerSentEventsTheApiWrites()
	{
		var stream =
			(await Operations())
				.Single(o => o.Operation.GetProperty("operationId").GetString() == "ask-ai-stream")
				.Operation
				.GetProperty("responses")
				.GetProperty("200")
				.GetProperty("content")
				.GetProperty("text/event-stream")
				.GetProperty("example")
				.GetString()!;

		var events = stream.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
		events.Should().NotBeEmpty();
		events.Should().AllSatisfy(e => e.Should().StartWith("data: "));
		var parsed = events.Select(
			e => JsonSerializer.Deserialize(e["data: ".Length..], AskAiEventJsonContext.Default.AskAiEvent)
		).ToList();
		parsed[0].Should().BeOfType<AskAiEvent.ConversationStart>();
		parsed[^1].Should().BeOfType<AskAiEvent.ConversationEnd>();
	}

	[Test]
	public async Task EveryErrorExample_IsAProblemOfThisApiWithItsStatus()
	{
		foreach (var (_, path, operation) in await Operations())
		{
			foreach (var response in operation.GetProperty("responses").EnumerateObject().Where(r => r.Name[0] is '4' or '5'))
			{
				var problem = response.Value.GetProperty("content").GetProperty("application/problem+json").GetProperty("example");
				problem.GetProperty("type").GetString().Should().StartWith(ApiProblems.TypeUri(""), path);
				problem
					.GetProperty("status")
					.GetInt32()
					.Should()
					.Be(int.Parse(response.Name, System.Globalization.CultureInfo.InvariantCulture), path);
			}
		}
	}

	[Test]
	[Arguments("search", "q", "cluster health")]
	[Arguments("get-api-operation", "operationId", "cluster-health")]
	[Arguments("put-page-feedback", "feedbackId", "4f9d2c1a-7b3e-4c6d-9a8f-2e1b0c5d7a63")]
	public async Task Parameters_TakeTheirExampleFromTheFirstExampleRequest(string operationId, string parameter, string value)
	{
		var operation = (await Operations()).Single(o => o.Operation.GetProperty("operationId").GetString() == operationId).Operation;

		operation
			.GetProperty("parameters")
			.EnumerateArray()
			.Single(p => p.GetProperty("name").GetString() == parameter)
			.GetProperty("example")
			.GetString()
			.Should()
			.Be(value);
	}

	[Test]
	public async Task ReleasedCliCommands_AreSamplesOfTheirOperation()
	{
		var operations = await Operations();

		string? Cli(string id) =>
			operations
				.Single(o => o.Operation.GetProperty("operationId").GetString() == id)
				.Operation
				.GetProperty("x-codeSamples")
				.EnumerateArray()
				.SingleOrDefault(s => s.GetProperty("label").GetString() == "Elastic CLI") is { ValueKind: JsonValueKind.Object } s
				? s.GetProperty("source").GetString()
				: null;

		Cli("search").Should().Be("elastic docs search \"cluster health\"");
		Cli("ask-ai-stream").Should().StartWith("elastic docs ask ");
		Cli("list-api-operations").Should().BeNull("elastic docs api is not released yet");
	}
}
