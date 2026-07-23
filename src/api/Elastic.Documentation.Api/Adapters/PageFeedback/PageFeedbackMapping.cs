// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Mapping;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

public sealed record PageFeedbackDocument
{
	[Id]
	[Keyword]
	[JsonPropertyName("feedback_id")]
	public required string FeedbackId { get; init; }

	[Keyword(IgnoreAbove = 2048)]
	[JsonPropertyName("page_url")]
	public required string PageUrl { get; init; }

	[Keyword(IgnoreAbove = 500)]
	[JsonPropertyName("page_title")]
	public required string PageTitle { get; init; }

	[Keyword]
	[JsonPropertyName("reaction")]
	public required PageFeedbackReaction Reaction { get; init; }

	[Keyword]
	[JsonPropertyName("reason")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PageFeedbackReason? Reason { get; init; }

	[JsonPropertyName("reason_set_version")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? ReasonSetVersion { get; init; }

	[Text]
	[JsonPropertyName("comment")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Comment { get; init; }

	[Keyword(IgnoreAbove = 256)]
	[JsonPropertyName("euid")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Euid { get; init; }

	[Timestamp]
	[JsonPropertyName("@timestamp")]
	public required DateTimeOffset Timestamp { get; init; }
}

[JsonSerializable(typeof(PageFeedbackDocument))]
[JsonSerializable(typeof(PageFeedbackReaction))]
[JsonSerializable(typeof(PageFeedbackReason))]
internal sealed partial class PageFeedbackJsonContext : JsonSerializerContext;

[ElasticsearchMappingContext(JsonContext = typeof(PageFeedbackJsonContext))]
[Index<PageFeedbackDocument>(
	NameTemplate = "page-feedback-v1-{env}",
	Dynamic = false,
	MappingVersionFromAssembly = true
)]
internal static partial class PageFeedbackMappingContext;

internal sealed class PageFeedbackIndex(AppEnvironment appEnvironment)
{
	public ElasticsearchTypeContext MappingContext { get; } =
		PageFeedbackMappingContext.PageFeedbackDocument.CreateContext(env: appEnvironment.Current.ToStringFast(true));

	public string Name => MappingContext.IndexStrategy?.WriteTarget
		?? throw new InvalidOperationException("Page feedback index mapping has no write target.");
}
