// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Adapters.Search;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Records Ask AI message feedback to Elasticsearch.
/// </summary>
public sealed class ElasticsearchAskAiMessageFeedbackGateway : IAskAiMessageFeedbackGateway, IDisposable
{
	private readonly ElasticsearchClient _client;
	private readonly string _indexName;
	private readonly ILogger<ElasticsearchAskAiMessageFeedbackGateway> _logger;
	private bool _disposed;

	public ElasticsearchAskAiMessageFeedbackGateway(
		ElasticsearchOptions elasticsearchOptions,
		AppEnvironment appEnvironment,
		ILogger<ElasticsearchAskAiMessageFeedbackGateway> logger)
	{
		_logger = logger;
		_indexName = $"ask-ai-message-feedback-{appEnvironment.Current.ToStringFast(true)}";

		var nodePool = new SingleNodePool(new Uri(elasticsearchOptions.Url.Trim()));
		var clientSettings = new ElasticsearchClientSettings(
				nodePool,
				sourceSerializer: (_, settings) => new DefaultSourceSerializer(settings, MessageFeedbackJsonContext.Default)
			)
			.DefaultIndex(_indexName)
			.Authentication(new ApiKey(elasticsearchOptions.ApiKey));

		_client = new ElasticsearchClient(clientSettings);
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		(_client.Transport as IDisposable)?.Dispose();
		_disposed = true;
	}

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

		_logger.LogDebug("Indexing feedback with ID {FeedbackId} to index {IndexName}", feedbackId, _indexName);

		var response = await _client.IndexAsync<MessageFeedbackDocument>(document, idx => idx
			.Index(_indexName)
			.Id(feedbackId.ToString()), ctx);

		// MessageId and ConversationId are Guid types, so no sanitization needed
		if (!response.IsValidResponse)
		{
			_logger.LogWarning(
				"Failed to index message feedback for message {MessageId}: {Error}",
				record.MessageId,
				response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");
		}
		else
		{
			_logger.LogInformation(
				"Message feedback recorded: {Reaction} for message {MessageId} in conversation {ConversationId}. ES _id: {EsId}, Index: {Index}",
				record.Reaction,
				record.MessageId,
				record.ConversationId,
				response.Id,
				response.Index);
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
