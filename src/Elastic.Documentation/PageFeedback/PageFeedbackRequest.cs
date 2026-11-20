// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Nullean.Entities;

namespace Elastic.Documentation.PageFeedback;

/// <summary>A reader's rating of a documentation page.</summary>
/// <param name="PageUrl">The page's path, starting with <c>/</c>, at most 2048 characters.</param>
/// <param name="PageTitle">The page's title, at most 500 characters.</param>
/// <param name="Reaction"><c>thumbsUp</c> or <c>thumbsDown</c>.</param>
/// <param name="Reasons">Why, from the reasons that fit the reaction; omit with <c>reasonSetVersion</c> for a reaction alone.</param>
/// <param name="ReasonSetVersion">The version of the reason set the reasons come from; required with <c>reasons</c>.</param>
/// <param name="Comment">Free text, at most 2000 characters; it needs <c>reasons</c>.</param>
public record PageFeedbackRequest(
	string PageUrl,
	string PageTitle,
	PageFeedbackReaction Reaction,
	IReadOnlyList<PageFeedbackReason>? Reasons,
	int? ReasonSetVersion,
	string? Comment
) : IHaveConstraints<PageFeedbackRequest>
{
	public static ConstraintSet<PageFeedbackRequest> Constraints => PageFeedbackRequestConstraints.All;
}

/// <summary>How a reader rated a page: <c>thumbsUp</c> or <c>thumbsDown</c>.</summary>
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

/// <summary>
/// Why a reader rated a page. For <c>thumbsUp</c>: <c>accurate</c>, <c>solvedProblem</c>, <c>easyToUnderstand</c>,
/// <c>helpfulExamples</c>, <c>easyToFind</c>. For <c>thumbsDown</c>: <c>inaccurate</c>, <c>missingInformation</c>,
/// <c>hardToUnderstand</c>, <c>codeSampleErrors</c>, <c>outOfDate</c>. <c>anotherReason</c> fits either.
/// </summary>
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

	[JsonStringEnumMemberName("easyToFind")]
	EasyToFind,

	[JsonStringEnumMemberName("inaccurate")]
	Inaccurate,

	[JsonStringEnumMemberName("missingInformation")]
	MissingInformation,

	[JsonStringEnumMemberName("hardToUnderstand")]
	HardToUnderstand,

	[JsonStringEnumMemberName("codeSampleErrors")]
	CodeSampleErrors,

	[JsonStringEnumMemberName("outOfDate")]
	OutOfDate,

	[JsonStringEnumMemberName("anotherReason")]
	AnotherReason
}
