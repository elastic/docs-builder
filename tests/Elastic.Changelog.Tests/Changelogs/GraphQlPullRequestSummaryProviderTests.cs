// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Changelog.Caching;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// <see cref="GraphQlPullRequestSummaryProvider"/> reads the pull requests of a range 50 at a time, and keeps from the body
/// only what an entry needs: the release note and the issues the pull request fixes.
/// </summary>
public partial class GraphQlPullRequestSummaryProviderTests() : ChangelogTestBase()
{
	[GeneratedRegex(@" p(?<n>\d+): pullRequest")]
	private static partial Regex AliasRegex();

	private sealed class FakeGitHub
	{
		public Dictionary<int, (string Title, string? Body, string[] Labels)> PullRequests { get; } = [];
		public List<int> BatchSizes { get; } = [];
		public List<string> Queries { get; } = [];
		public string? Error { get; set; }

		public string Respond(string query)
		{
			var more = Regex.Match(query, @"p: pullRequest\(number: (\d+)\) \{ labels\(first: 100, after: ""(\d+)""");
			if (more.Success)
				return $$"""{ "data": { "repository": { "p": { "labels": {{LabelPage(int.Parse(more.Groups[1].Value), int.Parse(more.Groups[2].Value))}} } } } }""";

			Queries.Add(query);
			var fields = new List<string>();
			var errors = new List<string>();
			var numbers = AliasRegex().Matches(query).Select(m => int.Parse(m.Groups["n"].Value)).ToList();
			BatchSizes.Add(numbers.Count);
			foreach (var n in numbers)
			{
				if (PullRequests.TryGetValue(n, out var pr))
					fields.Add(
						$$"""
						"p{{n}}": { "number": {{n}}, "title": {{JsonSerializer.Serialize(pr.Title)}}, "updatedAt": "2026-10-01T00:00:00Z", "body": {{JsonSerializer.Serialize(pr.Body)}}, "labels": {{LabelPage(n, 0)}} }
						"""
					);
				else
				{
					fields.Add($"\"p{n}\": null");
					errors.Add(
						$$"""{ "type": "NOT_FOUND", "path": ["repository", "p{{n}}"], "message": "Could not resolve to a PullRequest with the number of {{n}}." }"""
					);
				}
			}

			if (Error is not null)
				errors.Add(Error);

			var errorJson = errors.Count > 0 ? $", \"errors\": [{string.Join(",", errors)}]" : string.Empty;
			return $"{{ \"data\": {{ \"repository\": {{ {string.Join(",", fields)} }} }}{errorJson} }}";
		}

		private string LabelPage(int n, int skip)
		{
			var labels = PullRequests[n].Labels;
			var page = labels.Skip(skip).Take(100).ToList();
			var next = skip + 100;
			var pageInfo = next < labels.Length
				? $$"""{ "hasNextPage": true, "endCursor": "{{next}}" }"""
				: """{ "hasNextPage": false, "endCursor": null }""";
			return $$"""{ "pageInfo": {{pageInfo}}, "nodes": [{{string.Join(",", page.Select(l => $$"""{ "name": {{JsonSerializer.Serialize(l)}} }"""))}}] }""";
		}
	}

	private GraphQlPullRequestSummaryProvider Provider(FakeGitHub github, string token = "test-token") =>
		new(
			LoggerFactory,
			new GitHubApiTransport(
				new StubHandler(req =>
				{
					var query =
						JsonDocument
							.Parse(req.Content!.ReadAsStringAsync().GetAwaiter().GetResult())
							.RootElement
							.GetProperty("query")
							.GetString()!;
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(github.Respond(query), Encoding.UTF8, "application/json")
					};
				}),
				token,
				(_, _) => Task.CompletedTask
			)
		);

	private static CommitRangePullRequest FromRange(
		int number,
		string? title = null,
		string[]? labels = null,
		DateTimeOffset? updatedAt = null
	) =>
		new()
		{
			Number = number,
			Url = $"https://github.com/elastic/widget/pull/{number}",
			CommitShas = [],
			Title = title,
			Labels = labels ?? [],
			UpdatedAt = updatedAt
		};

	private Task<IReadOnlyDictionary<int, PullRequestSummary?>?> GetAsync(FakeGitHub github, params int[] numbers) =>
		Provider(github).GetAsync(
			Collector,
			"elastic",
			"widget",
			[.. numbers.Select(n => FromRange(n))],
			TestContext.Current!.Execution.CancellationToken
		);

	[Test]
	public async Task Get_ReducesTheBodyToItsReleaseNoteAndLinkedIssues()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] =
			("Faster search", "Some context.\n\nFixes #123\n\n## Release Note\nSearch is faster.\n\nInternal details.\n\n## Impact\nSlow queries time out sooner.\n\n## Action\nRaise the timeout.", [
				">enhancement",
				"v9.5.0"
			]);

		var result = await GetAsync(github, 7);

		result.Should().NotBeNull();
		var summary = result[7]!;
		summary.Title.Should().Be("Faster search");
		summary.Labels.Should().Equal(">enhancement", "v9.5.0");
		summary.ReleaseNote.Content.Should().Be("Search is faster.");
		summary.ReleaseNote.WasTruncated.Should().BeTrue();
		summary.LinkedIssues.Should().Equal("https://github.com/elastic/widget/issues/123");
		summary.Impact.Should().Be("Slow queries time out sooner.");
		summary.Action.Should().Be("Raise the timeout.");
	}

	[Test]
	public async Task Get_BodyWithoutAReleaseNote_HasNoReleaseNote()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("Fix", "Just a description.", []);

		var result = await GetAsync(github, 7);

		result![7]!.ReleaseNote.Content.Should().BeNull();
		result[7]!.LinkedIssues.Should().BeEmpty();
		result[7]!.Impact.Should().BeNull();
		result[7]!.Action.Should().BeNull();
	}

	[Test]
	public async Task Get_ManyPullRequests_AreReadFiftyAtATime()
	{
		var github = new FakeGitHub();
		var numbers = Enumerable.Range(1, 120).ToArray();
		foreach (var n in numbers)
			github.PullRequests[n] = ($"PR {n}", null, []);

		var result = await GetAsync(github, numbers);

		result.Should().NotBeNull();
		result.Should().HaveCount(120);
		result.Values.Should().OnlyContain(s => s != null);
		// Their title, labels and update time are read first and their body second, 50 at a time each.
		github.BatchSizes.Order().Should().Equal(20, 20, 50, 50, 50, 50);
	}

	[Test]
	public async Task Get_PullRequestWithMoreThanAPageOfLabels_ReadsEveryLabel()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("Labelled", null, [.. Enumerable.Range(1, 230).Select(i => $"label-{i}")]);

		var result = await GetAsync(github, 7);

		result![7]!.Labels.Should().HaveCount(230).And.EndWith("label-230");
	}

	[Test]
	public async Task Get_NumberThatIsNotAPullRequest_IsNullAndTheRestStillComes()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("Real", null, []);

		var result = await GetAsync(github, 7, 8);

		result.Should().NotBeNull();
		result[7].Should().NotBeNull();
		result[8].Should().BeNull();
		Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task Get_AnotherGraphQlError_FailsTheRead()
	{
		var github = new FakeGitHub { Error = """{ "type": "INTERNAL", "message": "Something went wrong" }""" };
		github.PullRequests[7] = ("Real", null, []);

		var result = await GetAsync(github, 7);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("Something went wrong"));
	}

	[Test]
	public async Task Get_WithoutAToken_IsAnError()
	{
		var result = await Provider(new FakeGitHub(), token: "").GetAsync(
			Collector,
			"elastic",
			"widget",
			[FromRange(7)],
			TestContext.Current!.Execution.CancellationToken
		);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("GITHUB_TOKEN"));
	}

	[Test]
	public async Task Get_NothingRequested_AsksNothing()
	{
		var github = new FakeGitHub();

		var result = await GetAsync(github);

		result.Should().BeEmpty();
		github.BatchSizes.Should().BeEmpty();
	}

	// ── what is derived from the body is kept until the pull request changes ──

	private static readonly DateTimeOffset UpdatedAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
	private readonly string _cacheDirectory = Path.Combine(
		Path.GetTempPath(),
		"docs-builder-summary-cache-tests",
		Guid.NewGuid().ToString("N")
	);

	public override void Dispose()
	{
		if (Directory.Exists(_cacheDirectory))
			Directory.Delete(_cacheDirectory, recursive: true);

		base.Dispose();
		GC.SuppressFinalize(this);
	}

	private async Task<IReadOnlyDictionary<int, PullRequestSummary?>?> GetWithCacheAsync(
		FakeGitHub github,
		params CommitRangePullRequest[] pullRequests
	)
	{
		var cache = new ChangelogCache(_cacheDirectory, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
		var provider = new GraphQlPullRequestSummaryProvider(
			LoggerFactory,
			new GitHubApiTransport(
				new StubHandler(req =>
				{
					var query =
						JsonDocument
							.Parse(req.Content!.ReadAsStringAsync().GetAwaiter().GetResult())
							.RootElement
							.GetProperty("query")
							.GetString()!;
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(github.Respond(query), Encoding.UTF8, "application/json")
					};
				}),
				"test-token",
				(_, _) => Task.CompletedTask
			),
			cache
		);

		var result = await provider.GetAsync(
			Collector,
			"elastic",
			"widget",
			pullRequests,
			TestContext.Current!.Execution.CancellationToken
		);
		await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);
		return result;
	}

	[Test]
	public async Task Get_APullRequestOfARange_ReadsOnlyItsBodyAndUsesTheTitleAndLabelsItBrings()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("Title on GitHub", "Fixes #9\n\n## Release Note\nSearch is faster.", ["label-on-github"]);

		var result = await GetWithCacheAsync(github, FromRange(7, "Title of the range", ["label-of-the-range"], UpdatedAt));

		var summary = result![7]!;
		summary.Title.Should().Be("Title of the range");
		summary.Labels.Should().Equal("label-of-the-range");
		summary.ReleaseNote.Content.Should().Be("Search is faster.");
		summary.LinkedIssues.Should().Equal("https://github.com/elastic/widget/issues/9");
		github.Queries.Should().ContainSingle().Which.Should().NotContain("labels(").And.NotContain("title");
	}

	[Test]
	public async Task Get_WithACache_TheSecondRunDoesNotReadTheBodyAgain()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("T", "## Release Note\nSearch is faster.", []);

		_ = await GetWithCacheAsync(github, FromRange(7, "T", [], UpdatedAt));
		var requestsAfterFirst = github.Queries.Count;
		var second = await GetWithCacheAsync(github, FromRange(7, "T", ["new-label"], UpdatedAt));

		requestsAfterFirst.Should().Be(1);
		github.Queries.Should().HaveCount(1, "the body is known for this state of the pull request");
		second![7]!.ReleaseNote.Content.Should().Be("Search is faster.");
		second[7]!.Labels.Should().Equal("new-label");
	}

	[Test]
	public async Task Get_ThePullRequestChanged_ReadsTheBodyAgain()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("T", "## Release Note\nOld note.", []);
		_ = await GetWithCacheAsync(github, FromRange(7, "T", [], UpdatedAt));

		github.PullRequests[7] = ("T", "## Release Note\nFixed note.", []);
		var second = await GetWithCacheAsync(github, FromRange(7, "T", [], UpdatedAt.AddMinutes(5)));

		github.Queries.Should().HaveCount(2);
		second![7]!.ReleaseNote.Content.Should().Be("Fixed note.");
	}

	[Test]
	public async Task Get_WithoutAnUpdateTime_TheBodyIsReadEveryTime()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("T", "Body.", []);

		_ = await GetWithCacheAsync(github, FromRange(7, "T", []));
		_ = await GetWithCacheAsync(github, FromRange(7, "T", []));

		github.Queries.Should().HaveCount(2, "nothing says that the body is still the one that was read");
	}

	[Test]
	public async Task Get_OnlyTheChangedPullRequestsAreRead()
	{
		var github = new FakeGitHub();
		foreach (var n in new[] { 1, 2, 3 })
			github.PullRequests[n] = ($"PR {n}", $"## Release Note\nNote {n}.", []);
		_ = await GetWithCacheAsync(
			github,
			FromRange(1, "PR 1", [], UpdatedAt),
			FromRange(2, "PR 2", [], UpdatedAt),
			FromRange(3, "PR 3", [], UpdatedAt)
		);

		github.PullRequests[2] = ("PR 2", "## Release Note\nNote 2 fixed.", []);
		github.BatchSizes.Clear();
		var second = await GetWithCacheAsync(
			github,
			FromRange(1, "PR 1", [], UpdatedAt),
			FromRange(2, "PR 2", [], UpdatedAt.AddMinutes(1)),
			FromRange(3, "PR 3", [], UpdatedAt)
		);

		github.BatchSizes.Should().Equal(1);
		second!.Values.Select(v => v!.ReleaseNote.Content).Should().Equal("Note 1.", "Note 2 fixed.", "Note 3.");
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
