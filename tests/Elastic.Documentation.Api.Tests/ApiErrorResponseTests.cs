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
			response.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(5));
			body.GetProperty("errorType").GetString().Should().Be("search_unavailable");
			body.GetProperty("message").GetString().Should().Contain("MaxTimeoutReached");
		}
	}

	[Test]
	public async Task Search_UnexpectedFailure_IsA500WithABodyThatHidesTheException()
	{
		var (response, body) = await SearchThrowing(new InvalidOperationException("secret detail"));

		using (response)
		{
			response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
			response.Headers.RetryAfter.Should().BeNull();
			body.GetProperty("errorType").GetString().Should().Be("internal_error");
			body.GetProperty("message").GetString().Should().NotContain("secret detail");
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
	}
}
