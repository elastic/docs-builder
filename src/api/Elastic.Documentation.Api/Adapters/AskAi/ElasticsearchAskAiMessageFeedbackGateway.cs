// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Api;
using Elastic.Documentation.Api.AskAi;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Adapters.AskAi;

/// <summary>
/// Records Ask AI message feedback to Elasticsearch.
/// </summary>
public sealed class ElasticsearchAskAiMessageFeedbackGateway(
	ITransport transport,
	AppEnvironment appEnvironment,
	ILogger<ElasticsearchAskAiMessageFeedbackGateway> logger
) : IAskAiMessageFeedbackService
{
	private readonly string _indexName = $"ask-ai-message-feedback-{appEnvironment.Current.ToStringFast(true)}";

	public async Task RecordFeedbackAsync(AskAiMessageFeedbackRecord record, CancellationToken ctx)
	{
		var feedbackId = Guid.NewGuid();
		var document = new MessageFeedbackDocument
		{
			FeedbackId = feedbackId.ToString(),
			MessageId = record.MessageId.ToString(),
			ConversationId = record.ConversationId.ToString(),
			Reaction = record.Reaction.ToString().ToLowerInvariant(),
			Euid = record.Euid,
			Timestamp = DateTimeOffset.UtcNow
		};

		logger.LogDebug("Indexing feedback with ID {FeedbackId} to index {IndexName}", feedbackId, _indexName);
		var json = JsonSerializer.Serialize(document, MessageFeedbackJsonContext.Default.MessageFeedbackDocument);
		var response = await transport.PutAsync<StringResponse>(
			$"{_indexName}/_doc/{feedbackId}",
			PostData.String(json),
			ctx);

		// MessageId and ConversationId are Guid types, so no sanitization needed
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			logger.LogWarning(
				"Failed to index message feedback for message {MessageId}: HTTP {StatusCode}",
				record.MessageId,
				response.ApiCallDetails.HttpStatusCode);
		}
		else
		{
			logger.LogInformation(
				"Message feedback recorded: {Reaction} for message {MessageId} in conversation {ConversationId}. ES _id: {EsId}, Index: {Index}",
				record.Reaction,
				record.MessageId,
				record.ConversationId,
				feedbackId,
				_indexName);
		}
	}
}

internal sealed record MessageFeedbackDocument
{
	[JsonPropertyName("feedback_id")]
	public required string FeedbackId { get; init; }

	[JsonPropertyName("message_id")]
	public required string MessageId { get; init; }

	[JsonPropertyName("conversation_id")]
	public required string ConversationId { get; init; }

	[JsonPropertyName("reaction")]
	public required string Reaction { get; init; }

	[JsonPropertyName("euid")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Euid { get; init; }

	[JsonPropertyName("@timestamp")]
	public required DateTimeOffset Timestamp { get; init; }
}

[JsonSerializable(typeof(MessageFeedbackDocument))]
internal sealed partial class MessageFeedbackJsonContext : JsonSerializerContext;
