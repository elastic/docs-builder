// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.AskAi;
using Elastic.Documentation.Api.AskAi;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Api.Infrastructure.Tests.Adapters.AskAi;

/// <summary>
/// Test helper to parse AskAiEvents from transformer output streams.
/// Uses the production SseParser to read SSE format.
/// </summary>
internal static class StreamTransformerTestHelpers
{
	/// <summary>
	/// Parse AskAiEvents from a transformer output stream for test assertions.
	/// </summary>
	public static async Task<List<AskAiEvent>> ParseAskAiEventsAsync(Stream stream)
	{
		var events = new List<AskAiEvent>();
		var reader = PipeReader.Create(stream);
		await foreach (var sseEvent in SseParser.ParseAsync(reader, CancellationToken.None))
		{
			var evt = JsonSerializer.Deserialize<AskAiEvent>(sseEvent.Data, AskAiEventJsonContext.Default.AskAiEvent);
			if (evt != null)
				events.Add(evt);
		}
		return events;
	}

	/// <summary>The transformer output exactly as the client receives it: SSE text.</summary>
	public static async Task<string> ReadSseAsync(Stream stream)
	{
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return await reader.ReadToEndAsync();
	}

	/// <summary>Replaces what changes per run: generated conversation ids and millisecond timestamps taken from the clock.</summary>
	public static SnapshotOptions Scrub() =>
		new SnapshotOptions().Scrub(Scrubbers.Guids).Scrub(new Regex(@"""timestamp"":\d{13}"), "\"timestamp\":{now}");
}

public class AgentBuilderStreamTransformerTests
{
	private readonly AgentBuilderStreamTransformer _transformer;

	public AgentBuilderStreamTransformerTests() =>
		_transformer = new AgentBuilderStreamTransformer(NullLogger<AgentBuilderStreamTransformer>.Instance);

	[Test]
	public async Task TransformAsyncWithRealAgentBuilderPayloadParsesAllEventTypes()
	{
		// Arrange - Real Agent Builder SSE stream
		var sseData =
			"""
			event: conversation_id_set
			data: {"data":{"conversation_id":"360222c5-76aa-405a-8316-703e1061b621"}}

			: keepalive

			event: reasoning
			data: {"data":{"reasoning":"Searching for relevant documents..."}}

			event: tool_call
			data: {"data":{"tool_call_id":"tooluse_abc123","tool_id":"docs-esql","params":{"keyword_query":"semantic search","abstract_query":"natural language understanding vector search embeddings similarity"}}}

			event: tool_result
			data: {"data":{"tool_call_id":"tooluse_abc123","tool_id":"docs-esql","results":[{"type":"query","data":{"esql":"FROM semantic-docs-prod-latest | WHERE MATCH(title.semantic_text, \"semantic search\")"},"tool_result_id":"result1"}]}}

			event: message_chunk
			data: {"data":{"text_chunk":"Hello"}}

			event: message_chunk
			data: {"data":{"text_chunk":" world"}}

			event: message_complete
			data: {"data":{"message_content":"Hello world"}}

			event: round_complete
			data: {"data":{}}

			""";

		var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

		// Act - No generated ID (Agent Builder handles IDs in stream)
		var outputStream = await _transformer.TransformAsync(inputStream, generatedConversationId: null, null, CancellationToken.None);
		var output = await StreamTransformerTestHelpers.ReadSseAsync(outputStream);

		// Assert - every event type of the Agent Builder stream, in order, with its content
		// (ConversationStart comes from the stream for Agent Builder)
		output.Should().MatchInlineSnapshot(
			options: StreamTransformerTestHelpers.Scrub(),
			expected: """
			data: {"type":"conversation_start","conversationId":"Guid_1","id":"Guid_2","timestamp":{now}}

			data: {"type":"reasoning","message":"Searching for relevant documents...","id":"Guid_3","timestamp":{now}}

			data: {"type":"search_tool_call","toolCallId":"tooluse_abc123","searchQuery":"semantic search","id":"Guid_4","timestamp":{now}}

			data: {"type":"tool_result","toolCallId":"tooluse_abc123","result":"[{\u0022type\u0022:\u0022query\u0022,\u0022data\u0022:{\u0022esql\u0022:\u0022FROM semantic-docs-prod-latest | WHERE MATCH(title.semantic_text, \\\u0022semantic search\\\u0022)\u0022},\u0022tool_result_id\u0022:\u0022result1\u0022}]","id":"Guid_5","timestamp":{now}}

			data: {"type":"message_chunk","content":"Hello","id":"Guid_6","timestamp":{now}}

			data: {"type":"message_chunk","content":" world","id":"Guid_7","timestamp":{now}}

			data: {"type":"message_complete","fullContent":"Hello world","id":"Guid_8","timestamp":{now}}

			data: {"type":"conversation_end","id":"Guid_9","timestamp":{now}}
			"""
		);
	}

	[Test]
	public async Task TransformAsyncWithKeepAliveCommentsSkipsThem()
	{
		// Arrange
		var sseData =
			"""
			: 000000000000000000

			event: message_chunk
			data: {"data":{"text_chunk":"test"}}

			: keepalive

			event: round_complete
			data: {"data":{}}

			""";

		var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

		// Act
		var outputStream = await _transformer.TransformAsync(inputStream, generatedConversationId: null, null, CancellationToken.None);
		var output = await StreamTransformerTestHelpers.ReadSseAsync(outputStream);

		// Assert - the keepalive comments are dropped
		output.Should().MatchInlineSnapshot(
			options: StreamTransformerTestHelpers.Scrub(),
			expected: """
			data: {"type":"message_chunk","content":"test","id":"Guid_1","timestamp":{now}}

			data: {"type":"conversation_end","id":"Guid_2","timestamp":{now}}
			"""
		);
	}

	[Test]
	public async Task TransformAsyncWithMultilineDataFieldsAccumulatesCorrectly()
	{
		// Arrange
		var sseData =
			"""
			event: message_chunk
			data: {"data":
			data: {"text_chunk":
			data: "multiline"}}

			""";

		var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

		// Act
		var outputStream = await _transformer.TransformAsync(inputStream, generatedConversationId: null, null, CancellationToken.None);
		var output = await StreamTransformerTestHelpers.ReadSseAsync(outputStream);

		// This test has malformed SSE data (missing proper blank line terminator). The old assertion
		// (HaveCountGreaterThanOrEqualTo(0)) could not fail; the snapshot pins what is actually emitted.
		output.Should().MatchInlineSnapshot(
			options: StreamTransformerTestHelpers.Scrub(),
			expected: "data: {\"type\":\"message_chunk\",\"content\":\"multiline\",\"id\":\"Guid_1\",\"timestamp\":{now}}"
		);
	}
}

public class LlmGatewayStreamTransformerTests
{
	private readonly LlmGatewayStreamTransformer _transformer;

	public LlmGatewayStreamTransformerTests() =>
		_transformer = new LlmGatewayStreamTransformer(NullLogger<LlmGatewayStreamTransformer>.Instance);

	[Test]
	public async Task TransformAsyncWithRealLlmGatewayPayloadParsesAllEventTypes()
	{
		// Arrange - Real LLM Gateway SSE stream
		var sseData =
			"""
			event: agent_stream_output
			data: [null, {"type":"agent_start","id":"1","timestamp":1234567890,"data":{}}]

			event: agent_stream_output
			data: [null, {"type":"ai_message_chunk","id":"2","timestamp":1234567891,"data":{"content":"Hello"}}]

			event: agent_stream_output
			data: [null, {"type":"ai_message_chunk","id":"3","timestamp":1234567892,"data":{"content":" world"}}]

			event: agent_stream_output
			data: [null, {"type":"tool_call","id":"4","timestamp":1234567893,"data":{"toolCalls":[{"id":"tool1","name":"ragSearch","args":{"searchQuery":"Index Lifecycle Management (ILM) Elasticsearch documentation"}}]}}]

			event: agent_stream_output
			data: [null, {"type":"tool_message","id":"5","timestamp":1234567894,"data":{"toolCallId":"tool1","result":"Found 10 docs"}}]

			event: agent_stream_output
			data: [null, {"type":"ai_message","id":"6","timestamp":1234567895,"data":{"content":"Hello world"}}]

			event: agent_stream_output
			data: [null, {"type":"agent_end","id":"7","timestamp":1234567896,"data":{}}]

			""";

		var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

		// Act - Simulate new conversation to get ConversationStart event
		var testConversationId = Guid.NewGuid().ToString();
		var outputStream = await _transformer.TransformAsync(inputStream, Guid.Parse(testConversationId), null, CancellationToken.None);
		var output = await StreamTransformerTestHelpers.ReadSseAsync(outputStream);

		// Assert - ConversationStart (generated by the transformer), both chunks, the search tool call with its extracted query,
		// the tool result with the payload stripped on purpose (CloudFront OAC buffering), the complete message and the end
		output.Should().MatchInlineSnapshot(
			options: StreamTransformerTestHelpers.Scrub(),
			expected: """
			data: {"type":"conversation_start","conversationId":"Guid_1","id":"Guid_2","timestamp":{now}}

			data: {"type":"message_chunk","content":"Hello","id":"2","timestamp":1234567891}

			data: {"type":"message_chunk","content":" world","id":"3","timestamp":1234567892}

			data: {"type":"search_tool_call","toolCallId":"tool1","searchQuery":"Index Lifecycle Management (ILM) Elasticsearch documentation","id":"4","timestamp":1234567893}

			data: {"type":"tool_result","toolCallId":"tool1","result":"","id":"5","timestamp":1234567894}

			data: {"type":"message_complete","fullContent":"Hello world","id":"6","timestamp":1234567895}

			data: {"type":"conversation_end","id":"7","timestamp":1234567896}
			"""
		);
	}

	[Test]
	public async Task TransformAsyncWithEmptyDataLinesSkipsThem()
	{
		// Arrange
		var sseData =
			"""
			event: agent_stream_output
			data:

			event: agent_stream_output
			data: [null, {"type":"agent_start","id":"1","timestamp":1234567890,"data":{}}]

			event: agent_stream_output
			data:

			event: agent_stream_output
			data: [null, {"type":"agent_end","id":"2","timestamp":1234567891,"data":{}}]

			""";

		var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

		// Act - Simulate new conversation
		var testConversationId = Guid.NewGuid().ToString();
		var outputStream = await _transformer.TransformAsync(inputStream, Guid.Parse(testConversationId), null, CancellationToken.None);
		var output = await StreamTransformerTestHelpers.ReadSseAsync(outputStream);

		// Assert - the empty data lines are skipped: only ConversationStart and ConversationEnd remain
		output.Should().MatchInlineSnapshot(
			options: StreamTransformerTestHelpers.Scrub(),
			expected: """
			data: {"type":"conversation_start","conversationId":"Guid_1","id":"Guid_2","timestamp":{now}}

			data: {"type":"conversation_end","id":"2","timestamp":1234567891}
			"""
		);
	}

	[Test]
	public async Task TransformAsyncSkipsModelLifecycleEvents()
	{
		// Arrange
		var sseData =
			"""
			data: [null, {"type":"chat_model_start","id":"1","timestamp":1234567890,"data":{}}]

			data: [null, {"type":"ai_message_chunk","id":"2","timestamp":1234567891,"data":{"content":"test"}}]

			data: [null, {"type":"chat_model_end","id":"3","timestamp":1234567892,"data":{}}]

			""";

		var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

		// Act - Simulate new conversation
		var testConversationId = Guid.NewGuid().ToString();
		var outputStream = await _transformer.TransformAsync(inputStream, Guid.Parse(testConversationId), null, CancellationToken.None);
		var output = await StreamTransformerTestHelpers.ReadSseAsync(outputStream);

		// Assert - the model lifecycle events are skipped: only ConversationStart and the message chunk remain
		output.Should().MatchInlineSnapshot(
			options: StreamTransformerTestHelpers.Scrub(),
			expected: """
			data: {"type":"conversation_start","conversationId":"Guid_1","id":"Guid_2","timestamp":{now}}

			data: {"type":"message_chunk","content":"test","id":"2","timestamp":1234567891}
			"""
		);
	}
}

/// <summary>
/// Parameterized tests for common behaviors expected from all stream transformers.
/// These tests ensure consistency across different transformer implementations.
/// Adding a new transformer? Just add it to StreamTransformerTestCases() and these tests will automatically run against it.
/// </summary>
public class StreamTransformerCommonBehaviorTests
{
	// The SSE text is an argument of the test, and the snapshot file name carries a hash of the arguments. A checkout on Windows turns the
	// line endings of this file into "\r\n", so the text is normalised, or the hash and the file name would differ per platform.
	public static IEnumerable<Func<(string, IStreamTransformer, string)>> StreamTransformerTestCases()
	{
		yield return () =>
			("AgentBuilderStreamTransformer", new AgentBuilderStreamTransformer(NullLogger<AgentBuilderStreamTransformer>.Instance),
			// Agent Builder SSE format for conversation_id_set
			"""
			event: conversation_id_set
			data: {"data":{"conversation_id":"360222c5-76aa-405a-8316-703e1061b621"}}

			event: message_chunk
			data: {"data":{"text_chunk":"test"}}

			""".ReplaceLineEndings(
				"\n"
			));
		yield return () =>
			("LlmGatewayStreamTransformer", new LlmGatewayStreamTransformer(NullLogger<LlmGatewayStreamTransformer>.Instance),
			// LLM Gateway SSE format - minimal events
			"""
			event: agent_stream_output
			data: [null, {"type":"ai_message_chunk","id":"1","timestamp":1234567890,"data":{"content":"test"}}]

			""".ReplaceLineEndings(
				"\n"
			));
	}

	/// <summary>
	/// A new conversation starts with a ConversationStart event that has a conversation id, an event id and a clock
	/// timestamp (scrubbed to {now}, which also fails on a zero timestamp), whatever the transformer.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(StreamTransformerTestCases))]
	public async Task NewConversationEmitsConversationStart(string transformerName, IStreamTransformer transformer, string sseData)
	{
		var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));
		var testConversationId = Guid.NewGuid().ToString();

		var outputStream = await transformer.TransformAsync(inputStream, Guid.Parse(testConversationId), null, CancellationToken.None);
		var output = await StreamTransformerTestHelpers.ReadSseAsync(outputStream);

		output.Should().MatchSnapshot(name: transformerName, options: StreamTransformerTestHelpers.Scrub());
	}
}
