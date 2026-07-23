// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Documentation.Api.Tests.Fixtures;
using FakeItEasy;

namespace Elastic.Documentation.Api.Tests;

public class PageFeedbackEndpointTests
{
	[Fact]
	public async Task Put_ValidFeedback_RecordsFeedbackWithEuid()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		PageFeedbackRecord? recorded = null;
		A.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._))
			.Invokes((PageFeedbackRecord record, CancellationToken _) => recorded = record)
			.Returns(Task.FromResult(true));
		using var factory = ApiWebApplicationFactory.WithMockedServices(replacements => replacements.Replace(feedbackService));
		using var client = factory.CreateClient();
		var feedbackId = Guid.NewGuid();
		using var request = CreateRequest(feedbackId, ValidPayload);
		request.Headers.Add("Cookie", "euid=test-euid");

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		recorded.Should().NotBeNull();
		recorded.FeedbackId.Should().Be(feedbackId);
		recorded.PageUrl.Should().Be("/docs/test-page");
		recorded.Reaction.Should().Be(PageFeedbackReaction.ThumbsUp);
		recorded.Comment.Should().Be("Useful page");
		recorded.Euid.Should().Be("test-euid");
	}

	[Fact]
	public async Task Put_CommentExceedsLimit_ReturnsBadRequest()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		using var factory = ApiWebApplicationFactory.WithMockedServices(replacements => replacements.Replace(feedbackService));
		using var client = factory.CreateClient();
		var payload = $$"""
			{
				"pageUrl": "/docs/test-page",
				"pageTitle": "Test page",
				"reaction": "thumbsDown",
				"comment": "{{new string('x', 2001)}}"
			}
			""";
		using var request = CreateRequest(Guid.NewGuid(), payload);

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		A.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Put_PersistenceFails_ReturnsServiceUnavailable()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		A.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._))
			.Returns(Task.FromResult(false));
		using var factory = ApiWebApplicationFactory.WithMockedServices(replacements => replacements.Replace(feedbackService));
		using var client = factory.CreateClient();
		using var request = CreateRequest(Guid.NewGuid(), ValidPayload);

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
	}

	[Fact]
	public async Task Delete_ExistingFeedback_DeletesFeedback()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		var feedbackId = Guid.NewGuid();
		A.CallTo(() => feedbackService.DeleteFeedbackAsync(feedbackId, A<CancellationToken>._))
			.Returns(Task.FromResult(true));
		using var factory = ApiWebApplicationFactory.WithMockedServices(replacements => replacements.Replace(feedbackService));
		using var client = factory.CreateClient();

		using var response = await client.DeleteAsync(
			$"/docs/_api/v1/page-feedback/{feedbackId}",
			TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		A.CallTo(() => feedbackService.DeleteFeedbackAsync(feedbackId, A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	private const string ValidPayload = /*lang=json,strict*/
		"""
		{
			"pageUrl": "/docs/test-page",
			"pageTitle": "Test page",
			"reaction": "thumbsUp",
			"comment": " Useful page "
		}
		""";

	private static HttpRequestMessage CreateRequest(Guid feedbackId, string payload) =>
		new(HttpMethod.Put, $"/docs/_api/v1/page-feedback/{feedbackId}")
		{
			Content = new StringContent(payload, Encoding.UTF8, "application/json")
		};
}
