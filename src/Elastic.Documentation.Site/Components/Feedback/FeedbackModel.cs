// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Feedback;

public sealed record FeedbackReason(string Value, string Label, string? Description = null);

public enum FeedbackSurface
{
	Docs,
	Api
}

/// <summary>The reasons offered after a reaction. The copy for the API surface speaks about endpoints and schemas.</summary>
public static class FeedbackReasons
{
	public const int Version = 2;
	public const int CommentMaxLength = 2000;

	public static IReadOnlyList<FeedbackReason> Positive(FeedbackSurface surface) =>
		surface == FeedbackSurface.Api ? ApiPositive : DocsPositive;

	public static IReadOnlyList<FeedbackReason> Negative(FeedbackSurface surface) =>
		surface == FeedbackSurface.Api ? ApiNegative : DocsNegative;

	private static readonly FeedbackReason[] DocsPositive =
	[
		new("solvedProblem", "Solved my problem", "Helped me resolve an issue."),
		new("easyToUnderstand", "Easy to understand", "Clear and easy to follow."),
		new("accurate", "Accurate", "The page is correct about how the product works."),
		new("helpfulExamples", "Helpful examples", "The examples helped me complete my task."),
		new("easyToFind", "Easy to find", "I found the information I was looking for quickly."),
		new("anotherReason", "Another reason")
	];

	private static readonly FeedbackReason[] ApiPositive =
	[
		new("solvedProblem", "Solved my problem", "Helped me call the endpoint or use the schema."),
		new("easyToUnderstand", "Easy to understand", "Parameters, types, or errors were clear."),
		new("accurate", "Accurate", "The reference is correct about how the API works."),
		new("helpfulExamples", "Helpful examples", "The request or response examples helped."),
		new("easyToFind", "Easy to find", "I found the endpoint or schema I needed quickly."),
		new("anotherReason", "Another reason")
	];

	private static readonly FeedbackReason[] DocsNegative =
	[
		new("outOfDate", "Out of date", "The page describes an older version of the product."),
		new("hardToUnderstand", "Hard to understand", "Too complicated or unclear."),
		new("inaccurate", "Technically incorrect", "The page is wrong about how the product works."),
		new("codeSampleErrors", "Code sample errors", "One or more code samples are incorrect."),
		new("missingInformation", "Couldn't find what I needed", "Missing important information."),
		new("anotherReason", "Another reason")
	];

	private static readonly FeedbackReason[] ApiNegative =
	[
		new("outOfDate", "Out of date", "The reference describes an older version of the API."),
		new("hardToUnderstand", "Hard to understand", "Too complicated or unclear."),
		new("inaccurate", "Technically incorrect", "The reference is wrong about how the API works."),
		new("codeSampleErrors", "Example errors", "A request or response example is wrong."),
		new("missingInformation", "Couldn't find what I needed", "Missing a parameter, field, status, or auth detail."),
		new("anotherReason", "Another reason")
	];
}

public sealed record FeedbackModel
{
	public required string PageUrl { get; init; }
	public required string PageTitle { get; init; }

	/// <summary>The base of the docs API, such as <c>/docs/_api</c>. The script sends feedback to <c>{ApiBase}/v1/page-feedback/{id}</c>.</summary>
	public required string ApiBase { get; init; }

	public FeedbackSurface Surface { get; init; } = FeedbackSurface.Docs;

	/// <summary>Prefix for element ids, so two widgets on one page (as in the style guide) never share one.</summary>
	public string IdPrefix => $"feedback-{Surface.ToString().ToLowerInvariant()}";
}
