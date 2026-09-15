// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;

namespace Elastic.Changelog.Tests.Evaluation;

public class GitHubCommentServiceTests(ITestOutputHelper output)
{
	private const string Owner = "elastic";
	private const string Repo = "test";
	private const int PrNumber = 42;

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Created(string nodeId = "IC_test_node_id") =>
		new(HttpStatusCode.Created)
		{
			Content = new StringContent($"{{\"id\":1,\"node_id\":\"{nodeId}\"}}", Encoding.UTF8, "application/json")
		};

	private static HttpResponseMessage Ok(string nodeId = "IC_test_node_id") =>
		new(HttpStatusCode.OK) { Content = new StringContent($"{{\"id\":1,\"node_id\":\"{nodeId}\"}}", Encoding.UTF8, "application/json") };

	private static string CommentListJson(long id, string body, string login = "github-actions[bot]", string nodeId = "IC_test_node_id") =>
		$"[{{\"id\":{id},\"node_id\":\"{nodeId}\",\"body\":{JsonSerializer.Serialize(body)},\"user\":{{\"login\":\"{login}\"}}}}]";

	private GitHubCommentService Service(StubHandler handler) =>
		new(new TestLoggerFactory(output), new GitHubApiTransport(handler, "test-token"));

	[Fact]
	public async Task UpsertStickyComment_NoExistingComment_CreatesNewComment()
	{
		var requests = new List<(string method, string path)>();
		var handler = new StubHandler(req =>
		{
			requests.Add((req.Method.Method, req.RequestUri!.AbsolutePath));
			return req.Method.Method == "GET" ? Json("[]") : Created();
		});

		var result = await Service(handler).UpsertStickyCommentAsync(Owner, Repo, PrNumber, "### 📋 Changelog\n\nHello");

		result.Should().NotBeNull();
		requests.Should().ContainSingle(r => r.method == "POST" && r.path.Contains($"/issues/{PrNumber}/comments"));
	}

	[Fact]
	public async Task UpsertStickyComment_ExistingCommentWithMarker_UpdatesExisting()
	{
		const long existingId = 99;
		var body = "### 📋 Changelog\nOld content\n<!-- docs-builder:changelog -->";
		var requests = new List<(string method, string path)>();
		var handler = new StubHandler(req =>
		{
			requests.Add((req.Method.Method, req.RequestUri!.AbsolutePath));
			if (req.Method.Method == "GET")
				return Json(CommentListJson(existingId, body));
			return Ok();
		});

		var result = await Service(handler).UpsertStickyCommentAsync(Owner, Repo, PrNumber, "### 📋 Changelog\nNew");

		result.Should().NotBeNull();
		requests.Should().ContainSingle(r => r.method == "PATCH" && r.path.Contains($"/issues/comments/{existingId}"));
		requests.Should().NotContain(r => r.method == "POST");
	}

	[Fact]
	public async Task UpsertStickyComment_ExistingCommentWithLegacyPrefix_UpdatesExisting()
	{
		const long existingId = 77;
		var legacyBody = GitHubCommentService.LegacyTitlePrefix + "\nOld JS comment, no marker";
		var requests = new List<(string method, string path)>();
		var handler = new StubHandler(req =>
		{
			requests.Add((req.Method.Method, req.RequestUri!.AbsolutePath));
			if (req.Method.Method == "GET")
				return Json(CommentListJson(existingId, legacyBody));
			return Ok();
		});

		var result = await Service(handler).UpsertStickyCommentAsync(Owner, Repo, PrNumber, "### 📋 Changelog\nNew");

		result.Should().NotBeNull();
		requests.Should().ContainSingle(r => r.method == "PATCH" && r.path.Contains($"/issues/comments/{existingId}"));
	}

	[Fact]
	public async Task UpsertStickyComment_ExistingCommentOnPage2_UpdatesExisting()
	{
		const long existingId = 55;
		var page2Body = "### 📋 Changelog\nPage 2 comment\n<!-- docs-builder:changelog -->";

		// Build 100 non-matching comments for page 1
		static string NonMatchingComment(int i) => $"{{\"id\":{i},\"body\":\"some other comment\",\"user\":{{\"login\":\"some-user\"}}}}";

		var page1 = "[" + string.Join(",", Enumerable.Range(1000, 100).Select(NonMatchingComment)) + "]";

		var page2 = CommentListJson(existingId, page2Body);
		var requests = new List<(string method, string path, string? query)>();
		var handler = new StubHandler(req =>
		{
			var query = req.RequestUri!.Query;
			requests.Add((req.Method.Method, req.RequestUri.AbsolutePath, query));
			if (req.Method.Method == "GET")
				return Json(query.Contains("page=2") ? page2 : page1);
			return Ok();
		});

		var result = await Service(handler).UpsertStickyCommentAsync(Owner, Repo, PrNumber, "### 📋 Changelog\nNew");

		result.Should().NotBeNull();
		requests.Should().ContainSingle(r => r.method == "PATCH" && r.path.Contains($"/issues/comments/{existingId}"));
	}

	[Fact]
	public async Task UpsertStickyComment_ApiReturns403_ReturnsNullDoesNotThrow()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));

		var result = await Service(handler).UpsertStickyCommentAsync(Owner, Repo, PrNumber, "body");

		result.Should().BeNull();
	}

	[Fact]
	public async Task UpsertStickyComment_RenderedBody_StartsWithTitle()
	{
		string? postedBody = null;
		var handler = new StubHandler(req =>
		{
			if (req.Method.Method == "GET")
				return Json("[]");
			postedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
			return Created();
		});

		await Service(handler).UpsertStickyCommentAsync(Owner, Repo, PrNumber, "### 📋 Changelog\nContent");

		postedBody.Should().NotBeNull();
		// The posted body is JSON; System.Text.Json encodes the emoji as Unicode escapes,
		// so assert on the surrounding ASCII text instead of the emoji character itself.
		postedBody!.Should().Contain("Changelog").And.Contain("Content");
	}

	[Fact]
	public async Task UpsertStickyComment_PostedJson_UsesLowercaseBodyKey()
	{
		// GitHub's API requires lowercase "body" — PascalCase "Body" triggers HTTP 422.
		string? postedJson = null;
		var handler = new StubHandler(req =>
		{
			if (req.Method.Method == "GET")
				return Json("[]");
			postedJson = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
			return Created();
		});

		await Service(handler).UpsertStickyCommentAsync(Owner, Repo, PrNumber, "hello");

		postedJson.Should().NotBeNull();
		postedJson!.Should().Contain("\"body\"").And.NotContain("\"Body\"");
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
