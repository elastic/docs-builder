// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// <see cref="GraphQlPullRequestSummaryProvider"/> reads the pull requests of a range 50 at a time, and keeps from the body
/// only what an entry needs: the release note and the issues the pull request fixes.
/// </summary>
public partial class GraphQlPullRequestSummaryProviderTests() : ChangelogTestBase()
{
	[GeneratedRegex(@" p(?<n>\d+): pullRequest", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex AliasRegex();

	private sealed class FakeGitHub
	{
		public Dictionary<int, (string Title, string? Body, string[] Labels)> PullRequests { get; } = [];
		public List<int> BatchSizes { get; } = [];
		public string? Error { get; set; }

		public string Respond(string query)
		{
			var more = Regex.Match(
				query,
				@"p: pullRequest\(number: (\d+)\) \{ labels\(first: 100, after: ""(\d+)""",
				RegexOptions.None,
				RegexTimeouts.MatchTimeout
			);
			if (more.Success)
				return $$"""{ "data": { "repository": { "p": { "labels": {{LabelPage(int.Parse(more.Groups[1].Value), int.Parse(more.Groups[2].Value))}} } } } }""";

			var fields = new List<string>();
			var errors = new List<string>();
			var numbers = AliasRegex().Matches(query).Select(m => int.Parse(m.Groups["n"].Value)).ToList();
			BatchSizes.Add(numbers.Count);
			foreach (var n in numbers)
			{
				if (PullRequests.TryGetValue(n, out var pr))
					fields.Add(
						$$"""
						"p{{n}}": { "number": {{n}}, "title": {{JsonSerializer.Serialize(pr.Title)}}, "body": {{JsonSerializer.Serialize(pr.Body)}}, "labels": {{LabelPage(n, 0)}} }
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

	private Task<IReadOnlyDictionary<int, PullRequestSummary?>?> GetAsync(FakeGitHub github, params int[] numbers) =>
		Provider(github).GetAsync(Collector, "elastic", "widget", numbers, TestContext.Current!.Execution.CancellationToken);

	[Test]
	public async Task Get_ReducesTheBodyToItsReleaseNoteAndLinkedIssues()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] =
			("Faster search", "Some context.\n\nFixes #123\n\n## Release Note\nSearch is faster.\n\nInternal details.", [
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
	}

	[Test]
	public async Task Get_BodyWithoutAReleaseNote_HasNoReleaseNote()
	{
		var github = new FakeGitHub();
		github.PullRequests[7] = ("Fix", "Just a description.", []);

		var result = await GetAsync(github, 7);

		result![7]!.ReleaseNote.Content.Should().BeNull();
		result[7]!.LinkedIssues.Should().BeEmpty();
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
		github.BatchSizes.Order().Should().Equal(20, 50, 50);
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
			[7],
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

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
