// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Api.PageFeedback;

public record PageFeedbackRequest(
	string PageUrl,
	string PageTitle,
	PageFeedbackReaction Reaction,
	PageFeedbackReason? Reason,
	int? ReasonSetVersion,
	string? Comment
);

[JsonConverter(typeof(JsonStringEnumConverter<PageFeedbackReaction>))]
public enum PageFeedbackReaction
{
	[JsonStringEnumMemberName("unspecified")]
	Unspecified,

	[JsonStringEnumMemberName("thumbsUp")]
	ThumbsUp,

	[JsonStringEnumMemberName("thumbsDown")]
	ThumbsDown
}

[JsonConverter(typeof(JsonStringEnumConverter<PageFeedbackReason>))]
public enum PageFeedbackReason
{
	[JsonStringEnumMemberName("accurate")]
	Accurate,

	[JsonStringEnumMemberName("solvedProblem")]
	SolvedProblem,

	[JsonStringEnumMemberName("easyToUnderstand")]
	EasyToUnderstand,

	[JsonStringEnumMemberName("helpfulExamples")]
	HelpfulExamples,

	[JsonStringEnumMemberName("inaccurate")]
	Inaccurate,

	[JsonStringEnumMemberName("missingInformation")]
	MissingInformation,

	[JsonStringEnumMemberName("hardToUnderstand")]
	HardToUnderstand,

	[JsonStringEnumMemberName("codeSampleErrors")]
	CodeSampleErrors,

	[JsonStringEnumMemberName("anotherReason")]
	AnotherReason
}
