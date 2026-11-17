// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.Tests.Fixtures;
using Elastic.Documentation.Search;
using FakeItEasy;

namespace Elastic.Documentation.Api.Tests;

// The tests share one host and swap the services it resolves, so they run one after another.
[NotInParallel(nameof(ApiErrorResponseTests))]
public class ApiErrorResponseTests
{
	private static readonly SwappableService<IFullSearchService> Search = new();
	private static readonly SwappableService<IAskAiMessageFeedbackService> Feedback = new();
	private static readonly SwappableService<IChangesService> Changes = new();

	private static readonly Lazy<ApiWebApplicationFactory> SharedFactory = new(() => ApiWebApplicationFactory.WithMockedServices(replacements =>
	{
		_ = replacements.Replace<IFullSearchService>(_ => Search.Current);
		_ = replacements.Replace<IAskAiMessageFeedbackService>(_ => Feedback.Current);
	}));

	[After(Class)]
	public static void DisposeSharedFactory()
	{
		if (SharedFactory.IsValueCreated)
			SharedFactory.Value.Dispose();
	}

	private static CancellationToken Ct => TestContext.Current!.Execution.CancellationToken;

	private static async Task<(HttpResponseMessage Response, JsonElement Body)> SearchThrowing(Exception exception)
	{
		var search = A.Fake<IFullSearchService>();
		_ = A.CallTo(() => search.SearchAsync(A<FullSearchRequest>._, A<CancellationToken>._)).ThrowsAsync(exception);
		Search.Current = search;
		using var client = SharedFactory.Value.CreateClient();
		var response = await client.GetAsync("/docs/_api/v1/search/?q=health", Ct);
		return (response, JsonElement.Parse(await response.Content.ReadAsStringAsync(Ct)));
	}

	[Test]
	public async Task Search_BackendUnavailable_IsARetryable503()
	{
		var (response, body) = await SearchThrowing(SearchUnavailableException.Transient("MaxTimeoutReached"));

		using (response)
		{
			response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
			response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
			response.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(5));
			body.GetProperty("status").GetInt32().Should().Be(503);
			body.GetProperty("title").GetString().Should().Be("Search unavailable");
			body.GetProperty("type").GetString().Should().Be(ApiProblems.TypeUri("search-unavailable"));
			body.GetProperty("detail").GetString().Should().Contain("MaxTimeoutReached");
		}
	}

	[Test]
	public async Task Search_UnexpectedFailure_IsA500ProblemThatHidesTheException()
	{
		var (response, body) = await SearchThrowing(new InvalidOperationException("secret detail"));

		using (response)
		{
			response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
			response.Headers.RetryAfter.Should().BeNull();
			body.GetProperty("type").GetString().Should().Be(ApiProblems.TypeUri("internal-error"));
			body.GetProperty("detail").GetString().Should().NotContain("secret detail");
		}
	}

	[Test]
	[Arguments(true, HttpStatusCode.NoContent)]
	[Arguments(false, HttpStatusCode.ServiceUnavailable)]
	public async Task MessageFeedback_ReportsWhetherTheFeedbackWasStored(bool stored, HttpStatusCode expected)
	{
		var feedback = A.Fake<IAskAiMessageFeedbackService>();
		_ = A.CallTo(() => feedback.RecordFeedbackAsync(A<AskAiMessageFeedbackRecord>._, A<CancellationToken>._)).Returns(stored);
		Feedback.Current = feedback;
		using var client = SharedFactory.Value.CreateClient();

		using var response = await client.PostAsJsonAsync(
			"/docs/_api/v1/ask-ai/message-feedback",
			new AskAiMessageFeedbackRequest(Guid.NewGuid(), Guid.NewGuid(), Reaction.ThumbsUp),
			ApiJsonContext.Default.AskAiMessageFeedbackRequest,
			Ct
		);

		response.StatusCode.Should().Be(expected);
		A.CallTo(() => feedback.RecordFeedbackAsync(A<AskAiMessageFeedbackRecord>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
		if (!stored)
			JsonElement
				.Parse(await response.Content.ReadAsStringAsync(Ct))
				.GetProperty("type")
				.GetString()
				.Should()
				.Be(ApiProblems.TypeUri("feedback-unavailable"));
	}

	private static async Task<(HttpStatusCode Status, JsonElement Body)> Get(string pathAndQuery)
	{
		Search.Current = A.Fake<IFullSearchService>();
		Changes.Current = A.Fake<IChangesService>();
		using var client = SharedFactory.Value.CreateClient();
		using var response = await client.GetAsync($"/docs/_api/v1{pathAndQuery}", Ct);
		return (response.StatusCode, JsonElement.Parse(await response.Content.ReadAsStringAsync(Ct)));
	}

	[Test]
	[Arguments("/search")]
	[Arguments("/search?q=x&size=many")]
	[Arguments("/changes?since=yesterday")]
	[Arguments("/changes")]
	public async Task ParameterThatDoesNotBind_IsA400Problem(string pathAndQuery)
	{
		var (status, body) = await Get(pathAndQuery);

		status.Should().Be(HttpStatusCode.BadRequest);
		body.GetProperty("type").GetString().Should().Be(ApiProblems.TypeUri("invalid-arguments"));
		body.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
	}

	[Test]
	public async Task Changes_CursorItDidNotReturn_IsA400InsteadOfTheFirstPage()
	{
		var (status, body) = await Get("/changes?since=2026-01-01T00:00:00Z&cursor=not-a-cursor");

		status.Should().Be(HttpStatusCode.BadRequest);
		body.GetProperty("detail").GetString().Should().Be("cursor is not a nextCursor that /changes returned.");
		A.CallTo(Changes.Current).MustNotHaveHappened();
	}
}
