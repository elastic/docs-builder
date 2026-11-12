// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Documentation.Api.Tests.Fixtures;
using FakeItEasy;
using Nullean.Snapshots;
using Nullean.Snapshots.Http;
using Nullean.Snapshots.Json;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Api.Tests;

// The tests share one host and install their own feedback service on it, so they run one after another.
[NotInParallel(nameof(PageFeedbackEndpointTests))]
public class PageFeedbackEndpointTests
{
	private static readonly SwappableService<IPageFeedbackService> FeedbackService = new();

	private static readonly Lazy<ApiWebApplicationFactory> SharedFactory = new(
		() => ApiWebApplicationFactory.WithMockedServices(
			replacements => replacements.Replace<IPageFeedbackService>(_ => FeedbackService.Current)
		)
	);

	[After(Class)]
	public static void DisposeSharedFactory()
	{
		if (SharedFactory.IsValueCreated)
			SharedFactory.Value.Dispose();
	}

	// The id is replaced by its own text, so a record stored under another id than the request's fails the snapshot.
	private static SnapshotOptions ScrubFeedbackId(Guid feedbackId) =>
		new SnapshotOptions().Scrub(text => text.Replace(feedbackId.ToString(), "{feedback-id}", StringComparison.OrdinalIgnoreCase));

	private static HttpClient CreateClient(IPageFeedbackService feedbackService)
	{
		FeedbackService.Current = feedbackService;
		return SharedFactory.Value.CreateClient();
	}

	// The requests and the responses they must get are in http/page-feedback.http. The service accepts everything, so
	// what the file shows is the endpoint's own validation and status codes.
	[Test]
	[HttpFiles("http")]
	public async Task Run(SnapshotFile file)
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		A.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._)).Returns(Task.FromResult(true));
		A.CallTo(() => feedbackService.DeleteFeedbackAsync(A<Guid>._, A<CancellationToken>._)).Returns(Task.FromResult(true));
		using var client = CreateClient(feedbackService);

		await HttpFileTest.RunAsync(
			file,
			new HttpFileSnapshotOptions { Client = client },
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);
	}

	[Test]
	public async Task Put_ValidFeedback_RecordsFeedbackWithEuid()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		PageFeedbackRecord? recorded = null;
		A
			.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._))
			.Invokes((PageFeedbackRecord record, CancellationToken _) => recorded = record)
			.Returns(Task.FromResult(true));
		using var client = CreateClient(feedbackService);
		var feedbackId = Guid.NewGuid();
		using var request = CreateRequest(feedbackId, ValidPayload);
		request.Headers.Add("Cookie", "euid=test-euid");

		using var response = await client.SendAsync(request, TestContext.Current!.Execution.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		recorded.Should().NotBeNull();
		recorded.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "FeedbackId": "{feedback-id}",
			  "PageUrl": "/docs/test-page",
			  "PageTitle": "Test page",
			  "Reaction": "thumbsUp",
			  "Reasons": [
			    "accurate"
			  ],
			  "ReasonSetVersion": 2,
			  "Comment": "Useful page",
			  "Euid": "test-euid"
			}
			""",
			options: ScrubFeedbackId(feedbackId)
		);
	}

	[Test]
	public async Task Put_ReactionOnly_RecordsFeedbackWithoutDetails()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		PageFeedbackRecord? recorded = null;
		A
			.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._))
			.Invokes((PageFeedbackRecord record, CancellationToken _) => recorded = record)
			.Returns(Task.FromResult(true));
		using var client = CreateClient(feedbackService);
		const string payload = /*lang=json,strict*/
			"""
			{
				"pageUrl": "/docs/test-page",
				"pageTitle": "Test page",
				"reaction": "thumbsDown"
			}
			""";
		var feedbackId = Guid.NewGuid();
		using var request = CreateRequest(feedbackId, payload);

		using var response = await client.SendAsync(request, TestContext.Current!.Execution.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		recorded.Should().NotBeNull();
		recorded.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "FeedbackId": "{feedback-id}",
			  "PageUrl": "/docs/test-page",
			  "PageTitle": "Test page",
			  "Reaction": "thumbsDown",
			  "Reasons": null,
			  "ReasonSetVersion": null,
			  "Comment": null,
			  "Euid": null
			}
			""",
			options: ScrubFeedbackId(feedbackId)
		);
	}

	[Test]
	public async Task Put_MultipleReasons_RecordsAllReasons()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		PageFeedbackRecord? recorded = null;
		A
			.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._))
			.Invokes((PageFeedbackRecord record, CancellationToken _) => recorded = record)
			.Returns(Task.FromResult(true));
		using var client = CreateClient(feedbackService);
		const string payload = /*lang=json,strict*/
			"""
			{
				"pageUrl": "/docs/test-page",
				"pageTitle": "Test page",
				"reaction": "thumbsDown",
				"reasons": ["inaccurate", "outOfDate"],
				"reasonSetVersion": 2
			}
			""";
		var feedbackId = Guid.NewGuid();
		using var request = CreateRequest(feedbackId, payload);

		using var response = await client.SendAsync(request, TestContext.Current!.Execution.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		recorded.Should().NotBeNull();
		recorded.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "FeedbackId": "{feedback-id}",
			  "PageUrl": "/docs/test-page",
			  "PageTitle": "Test page",
			  "Reaction": "thumbsDown",
			  "Reasons": [
			    "inaccurate",
			    "outOfDate"
			  ],
			  "ReasonSetVersion": 2,
			  "Comment": null,
			  "Euid": null
			}
			""",
			options: ScrubFeedbackId(feedbackId)
		);
	}

	[Test]
	public async Task Put_CommentExceedsLimit_ReturnsBadRequest()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		using var client = CreateClient(feedbackService);
		var payload =
			$$"""
			{
				"pageUrl": "/docs/test-page",
				"pageTitle": "Test page",
				"reaction": "thumbsDown",
				"reasons": ["inaccurate"],
				"reasonSetVersion": 2,
				"comment": "{{new string('x', 2001)}}"
			}
			""";
		using var request = CreateRequest(Guid.NewGuid(), payload);

		using var response = await client.SendAsync(request, TestContext.Current!.Execution.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		A.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	[Test]
	[Arguments(
	/*lang=json,strict*/
	"""
		{
			"pageUrl": "/docs/test-page",
			"pageTitle": "Test page",
			"reaction": "thumbsUp",
			"reasons": ["inaccurate"],
			"reasonSetVersion": 2
		}
		""")]
	[Arguments(
	/*lang=json,strict*/
	"""
		{
			"pageUrl": "/docs/test-page",
			"pageTitle": "Test page"
		}
		""")]
	[Arguments(
	/*lang=json,strict*/
	"""
		{
			"pageUrl": "/docs/test-page",
			"pageTitle": "Test page",
			"reaction": "thumbsDown",
			"comment": "Missing a required reason"
		}
		""")]
	[Arguments(
	/*lang=json,strict*/
	"""
		{
			"pageUrl": "/docs/test-page",
			"pageTitle": "Test page",
			"reaction": "thumbsDown",
			"reasons": ["inaccurate"]
		}
		""")]
	public async Task Put_InvalidFeedback_ReturnsBadRequest(string payload)
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		using var client = CreateClient(feedbackService);
		using var request = CreateRequest(Guid.NewGuid(), payload);

		using var response = await client.SendAsync(request, TestContext.Current!.Execution.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		A.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	[Test]
	public async Task Put_PersistenceFails_ReturnsServiceUnavailable()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		A.CallTo(() => feedbackService.UpsertFeedbackAsync(A<PageFeedbackRecord>._, A<CancellationToken>._)).Returns(
			Task.FromResult(false)
		);
		using var client = CreateClient(feedbackService);
		using var request = CreateRequest(Guid.NewGuid(), ValidPayload);

		using var response = await client.SendAsync(request, TestContext.Current!.Execution.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
	}

	[Test]
	public async Task Delete_ExistingFeedback_DeletesFeedback()
	{
		var feedbackService = A.Fake<IPageFeedbackService>();
		var feedbackId = Guid.NewGuid();
		A.CallTo(() => feedbackService.DeleteFeedbackAsync(feedbackId, A<CancellationToken>._)).Returns(Task.FromResult(true));
		using var client = CreateClient(feedbackService);

		using var response = await client.DeleteAsync(
			$"/docs/_api/v1/page-feedback/{feedbackId}",
			TestContext.Current!.Execution.CancellationToken
		);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		A.CallTo(() => feedbackService.DeleteFeedbackAsync(feedbackId, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
	}

	private const string ValidPayload = /*lang=json,strict*/
		"""
		{
			"pageUrl": "/docs/test-page",
			"pageTitle": "Test page",
			"reaction": "thumbsUp",
			"reasons": ["accurate"],
			"reasonSetVersion": 2,
			"comment": " Useful page "
		}
		""";

	private static HttpRequestMessage CreateRequest(Guid feedbackId, string payload) =>
		new(HttpMethod.Put, $"/docs/_api/v1/page-feedback/{feedbackId}")
		{
			Content = new StringContent(payload, Encoding.UTF8, "application/json")
		};
}
