// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search;
using Microsoft.AspNetCore.Http;

namespace Elastic.Documentation.Api;

/// <summary>
/// The RFC 9457 problem types of the docs API. Every error answers <c>application/problem+json</c> whose
/// <c>type</c> is one of these, resolving to its section of the REST API page (docs/data/api.md).
/// </summary>
public static class ApiProblems
{
	private const string TypeBase = "https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/data/api#";

	public const string InvalidArguments = "invalid-arguments";
	public const string NotFound = "not-found";
	public const string InternalError = "internal-error";
	public const string SearchUnavailable = "search-unavailable";
	public const string FeedbackUnavailable = "feedback-unavailable";

	// A title is the same for every occurrence of a type (RFC 9457 §3.1.3); what varies goes in detail.
	private static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
	{
		[InvalidArguments] = "Invalid arguments",
		[NotFound] = "Not found",
		["operation-not-found"] = "API operation not found",
		["api-not-found"] = "API not found",
		["group-not-found"] = "API group not found",
		[InternalError] = "Internal error",
		[SearchUnavailable] = "Search unavailable",
		[FeedbackUnavailable] = "Feedback unavailable"
	};

	/// <summary>The absolute <c>type</c> URI of a problem type, such as <see cref="InvalidArguments"/>.</summary>
	public static string TypeUri(string type) => TypeBase + type;

	/// <summary>A problem response of a known type.</summary>
	public static IResult Problem(int statusCode, string type, string detail) =>
		Results.Problem(detail: detail, statusCode: statusCode, title: Titles[type], type: TypeUri(type));

	/// <summary>The status an unhandled exception answers with.</summary>
	public static int StatusCodeFor(Exception exception) => exception switch
	{
		// A parameter that does not bind, such as a missing q or a since that is not a date.
		BadHttpRequestException badRequest => badRequest.StatusCode,
		// Transient by construction (timeout, overload): tell clients and the CDN to retry.
		SearchUnavailableException => StatusCodes.Status503ServiceUnavailable,
		_ => StatusCodes.Status500InternalServerError
	};

	/// <summary>
	/// Gives the problems ASP.NET writes itself (unhandled exceptions, bodyless status codes) a type of this API.
	/// Problems this API builds with <see cref="Problem"/> already carry one and pass through unchanged.
	/// </summary>
	public static void Customize(ProblemDetailsContext context)
	{
		var problem = context.ProblemDetails;
		switch (context.Exception)
		{
			case BadHttpRequestException badRequest:
				Apply(problem, InvalidArguments, badRequest.Message);
				return;
			case SearchUnavailableException unavailable:
				Apply(problem, SearchUnavailable, unavailable.Message);
				context.HttpContext.Response.Headers.RetryAfter = "5";
				return;
			case not null:
				// Never the exception's own message: it can name internals.
				Apply(problem, InternalError, "An unexpected error occurred.");
				return;
		}

		if (problem.Type is not null && problem.Type.StartsWith(TypeBase, StringComparison.Ordinal))
			return;
		switch (problem.Status)
		{
			case StatusCodes.Status400BadRequest:
				Apply(problem, InvalidArguments, "A parameter is missing or does not parse.");
				break;
			case StatusCodes.Status404NotFound:
				Apply(problem, NotFound, "No endpoint matches this path.");
				break;
			case StatusCodes.Status500InternalServerError:
				Apply(problem, InternalError, "An unexpected error occurred.");
				break;
		}
	}

	private static void Apply(Microsoft.AspNetCore.Mvc.ProblemDetails problem, string type, string detail)
	{
		problem.Type = TypeUri(type);
		problem.Title = Titles[type];
		problem.Detail = detail;
	}
}
