// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
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
/// Fake-HTTP tests for <see cref="GitHubBackportService"/>: the original of a backport is only accepted when it is a
/// merged pull request on the default branch of the same repository, however it was named.
/// </summary>
public partial class GitHubBackportServiceTests() : ChangelogTestBase()
{
	private const string Owner = "elastic";
	private const string Repo = "widget";
	private const string TrailerSha = "abcdef0123456789abcdef0123456789abcdef01";

	[GeneratedRegex(@" (?<alias>[pv]\d+): pullRequest\(number: (?<n>\d+)\)")]
	private static partial Regex AliasRegex();

	/// <summary>What GitHub knows: backport details, and the pull requests the validation and trailer lookups find.</summary>
	private sealed partial class FakeGitHub
	{
		public Dictionary<int, (string Title, string? Body, string[] Messages)> Backports { get; } = [];
		public Dictionary<int, (bool Merged, string Branch, string Repo)> PullRequests { get; } = [];
		public Dictionary<string, int[]> TrailerPullRequests { get; } = [];
		public int GraphQlRequests { get; set; }
		/// <summary>An error added to the validation queries only, so tests can check that validation fails closed.</summary>
		public string? ValidationError { get; set; }
		public int ValidationRequests { get; set; }
		private const int FirstPage = 50;

		public string Respond(string query)
		{
			GraphQlRequests++;
			var fields = new List<string>();
			var errors = new List<string>();
			if (query.Contains("def: defaultBranchRef", StringComparison.Ordinal))
				fields.Add("\"def\": { \"name\": \"main\" }");

			var more = CommitPageRegex().Match(query);
			if (more.Success)
				fields.Add(
					$"\"p\": {{ \"commits\": {CommitPage(int.Parse(more.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(more.Groups[2].Value, CultureInfo.InvariantCulture), 100)} }}"
				);

			if (query.Contains(" v", StringComparison.Ordinal) && ValidationAliasRegex().IsMatch(query))
			{
				ValidationRequests++;
				if (ValidationError != null)
					errors.Add(ValidationError);
			}

			foreach (Match m in AliasRegex().Matches(query))
			{
				var n = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
				var alias = m.Groups["alias"].Value;
				if (alias[0] == 'v' && !PullRequests.ContainsKey(n))
					errors.Add(
						$$"""{ "type": "NOT_FOUND", "path": ["repository", "{{alias}}"], "message": "Could not resolve to a PullRequest with the number of {{n}}." }"""
					);
				fields.Add($"\"{alias}\": {(alias[0] == 'p' ? BackportNode(n) : PullRequestNode(n))}");
			}

			foreach (var (sha, prs) in TrailerPullRequests)
			{
				var index = Regex.Match(query, $"(c\\d+): object\\(oid: \"{sha}\"\\)");
				if (index.Success)
					fields.Add(
						$"\"{index.Groups[1].Value}\": {{ \"associatedPullRequests\": {{ \"nodes\": [{string.Join(",", prs.Select(PullRequestNode))}] }} }}"
					);
			}

			var errorJson = errors.Count > 0 ? $", \"errors\": [{string.Join(",", errors)}]" : string.Empty;
			return $"{{ \"data\": {{ \"repository\": {{ {string.Join(",", fields)} }} }}{errorJson} }}";
		}

		private string BackportNode(int n) =>
			Backports.TryGetValue(n, out var b)
				? $"{{ \"title\": {JsonSerializer.Serialize(b.Title)}, \"body\": {JsonSerializer.Serialize(b.Body)}, \"commits\": {CommitPage(n, 0, FirstPage)} }}"
				: "null";

		private string CommitPage(int n, int skip, int size)
		{
			var messages = Backports[n].Messages;
			var page = messages.Skip(skip).Take(size).ToList();
			var nodes = string.Join(",", page.Select(m => $"{{ \"commit\": {{ \"message\": {JsonSerializer.Serialize(m)} }} }}"));
			var next = skip + size;
			var pageInfo = next < messages.Length
				? $$"""{ "hasNextPage": true, "endCursor": "{{next}}" }"""
				: """{ "hasNextPage": false, "endCursor": null }""";
			return $"{{ \"pageInfo\": {pageInfo}, \"nodes\": [{nodes}] }}";
		}

		private string PullRequestNode(int n) =>
			PullRequests.TryGetValue(n, out var pr)
				? $"{{ \"number\": {n}, \"merged\": {(pr.Merged ? "true" : "false")}, \"baseRefName\": \"{pr.Branch}\", \"baseRepository\": {{ \"nameWithOwner\": \"{pr.Repo}\" }} }}"
				: "null";

		[GeneratedRegex(@"p: pullRequest\(number: (\d+)\) \{ commits\(first: 100, after: \""(\d+)\""")]
		private static partial Regex CommitPageRegex();

		[GeneratedRegex(@" v\d+: pullRequest")]
		private static partial Regex ValidationAliasRegex();
	}

	private static (bool Merged, string Branch, string Repo) Merged(string branch = "main", string repo = $"{Owner}/{Repo}") =>
		(true, branch, repo);

	private static GitHubBackportService Service(FakeGitHub github, string token = "test-token") =>
		new(
			new TestLoggerFactory(),
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
				token
			)
		);

	private async Task<IReadOnlyDictionary<int, BackportRoots?>?> ResolveAsync(FakeGitHub github, params int[] numbers) =>
		await Service(github).ResolveRootsAsync(
			Collector,
			Owner,
			Repo,
			numbers,
			new Dictionary<int, DateTimeOffset>(),
			TestContext.Current!.Execution.CancellationToken
		);

	[Test]
	public async Task ResolveRoots_TitleNamesMergedPullRequest_ReturnsItAsRoot()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Fix the thing (#100)", null, []);
		github.PullRequests[100] = Merged();

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200]!.Roots.Should().Equal(100);
		result[200]!.Route.Should().Be(BackportRootRoute.Title);
		Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task ResolveRoots_CherryPickTrailer_PrefersTheCommitsPullRequest()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Backport of something", null, [$"Fix it\n\n(cherry picked from commit {TrailerSha})"]);
		github.TrailerPullRequests[TrailerSha] = [100];
		github.PullRequests[100] = Merged();

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200]!.Roots.Should().Equal(100);
		result[200]!.Route.Should().Be(BackportRootRoute.CherryPickTrailer);
	}

	[Test]
	public async Task ResolveRoots_TrailerPullRequestOnOtherBranch_IsNotARoot()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Backport of something", null, [$"Fix it\n\n(cherry picked from commit {TrailerSha})"]);
		github.TrailerPullRequests[TrailerSha] = [100, 101];
		github.PullRequests[100] = Merged("9.4");
		github.PullRequests[101] = Merged();

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200]!.Roots.Should().Equal(101);
	}

	[Test]
	[Arguments("issue")]
	[Arguments("unmerged")]
	[Arguments("other-branch")]
	[Arguments("other-repo")]
	public async Task ResolveRoots_ReferencedNumberIsNotAValidRoot_ReturnsNull(string kind)
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Unmute test (#150)", null, []);
		switch (kind)
		{
			case "unmerged":
				github.PullRequests[150] = (false, "main", $"{Owner}/{Repo}");
				break;
			case "other-branch":
				github.PullRequests[150] = Merged("9.5");
				break;
			case "other-repo":
				github.PullRequests[150] = Merged(repo: "elastic/other");
				break;
		}

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200].Should().BeNull();
		Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task ResolveRoots_BackportWithManyCommits_ReadsEveryPage()
	{
		var github = new FakeGitHub();
		// 120 cherry-picked changes: the original of the last one is only on the third page.
		var messages = Enumerable.Range(1, 120).Select(i => $"Change {i} (#{i})").ToArray();
		github.Backports[500] = ("Backport of changes", null, messages);
		foreach (var i in Enumerable.Range(1, 120))
			github.PullRequests[i] = Merged();

		var result = await ResolveAsync(github, 500);

		result.Should().NotBeNull();
		result[500]!.Roots.Should().HaveCount(120).And.Contain(120);
	}

	[Test]
	public async Task ResolveRoots_TitleNamesFewerThanTheCommits_KeepsTheOtherOriginals()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Backport (#20)", null, ["First change (#20)", "Second change (#21)"]);
		github.PullRequests[20] = Merged();
		github.PullRequests[21] = Merged();

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200]!.Roots.Should().Equal(20, 21);
	}

	[Test]
	public async Task ResolveRoots_ChainedBackportSubject_KeepsTheOriginalWhenTheIntermediateIsNotValid()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("First (#20)", null, ["First (#20)", "Second (#21) (#201)"]);
		github.PullRequests[20] = Merged();
		github.PullRequests[21] = Merged();
		github.PullRequests[201] = Merged("9.4");

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200]!.Roots.Should().Equal(20, 21);
	}

	[Test]
	public async Task ResolveRoots_SubjectOnlyMentionsAnEarlierPullRequest_DoesNotAddIt()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Feature (#20)", null, ["Follow up on #10", "Feature (#20)"]);
		github.PullRequests[10] = Merged();
		github.PullRequests[20] = Merged();

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200]!.Roots.Should().Equal(20);
	}

	[Test]
	public async Task ResolveRoots_OversizedNumber_IsIgnoredAndTheOtherReferenceUsed()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Fix #2147483648 (#100)", null, []);
		github.PullRequests[100] = Merged();

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result[200]!.Roots.Should().Equal(100);
		Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task ResolveRoots_ValidationReturnsAnotherError_FailsInsteadOfGuessing()
	{
		var github = new FakeGitHub
		{
			ValidationError = """{ "type": "INTERNAL", "path": ["repository", "v100"], "message": "Something went wrong" }"""
		};
		github.Backports[200] = ("Fix the thing (#100)", null, []);
		github.PullRequests[100] = Merged();

		var result = await ResolveAsync(github, 200);

		result.Should().BeNull();
		github.ValidationRequests.Should().Be(1, "the error must come from the validation query, not an earlier one");
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("Something went wrong"));
	}

	[Test]
	public async Task ResolveRoots_NoNumberAnywhere_ReturnsNullEntry()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Bump dependency", "Automated change", []);

		var result = await ResolveAsync(github, 200);

		result.Should().NotBeNull();
		result.Should().ContainKey(200);
		result[200].Should().BeNull();
	}

	[Test]
	public async Task ResolveRoots_ManyBackports_AreQueriedInBatches()
	{
		var github = new FakeGitHub();
		var numbers = Enumerable.Range(1000, 45).ToArray();
		foreach (var n in numbers)
		{
			github.Backports[n] = ($"Change {n} (#{n - 500})", null, []);
			github.PullRequests[n - 500] = Merged();
		}

		var result = await ResolveAsync(github, numbers);

		result.Should().NotBeNull();
		result.Should().HaveCount(45);
		result.Values.Should().OnlyContain(r => r != null);
		result[1044]!.Roots.Should().Equal(544);
		// default branch + 3 batches of backports (20) + 2 validation batches (25)
		github.GraphQlRequests.Should().Be(6);
	}

	[Test]
	public async Task ResolveRoots_NothingRequested_DoesNotCallGitHub()
	{
		var github = new FakeGitHub();

		var result = await ResolveAsync(github);

		result.Should().NotBeNull();
		result.Should().BeEmpty();
		github.GraphQlRequests.Should().Be(0);
	}

	[Test]
	public async Task ResolveRoots_WithoutToken_EmitsErrorAndReturnsNull()
	{
		var github = new FakeGitHub();

		var result = await Service(github, token: "").ResolveRootsAsync(
			Collector,
			Owner,
			Repo,
			[200],
			new Dictionary<int, DateTimeOffset>(),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("GITHUB_TOKEN"));
		github.GraphQlRequests.Should().Be(0);
	}

	// ── where a backport came from is kept until the backport changes ──

	private static readonly DateTimeOffset UpdatedAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
	private readonly string _cacheDirectory = Path.Combine(
		Path.GetTempPath(),
		"docs-builder-backport-cache-tests",
		Guid.NewGuid().ToString("N")
	);

	public override void Dispose()
	{
		if (Directory.Exists(_cacheDirectory))
			Directory.Delete(_cacheDirectory, recursive: true);

		base.Dispose();
		GC.SuppressFinalize(this);
	}

	private async Task<IReadOnlyDictionary<int, BackportRoots?>?> ResolveWithCacheAsync(
		FakeGitHub github,
		Dictionary<int, DateTimeOffset> updatedAt,
		params int[] numbers
	)
	{
		var cache = new ChangelogCache(_cacheDirectory, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
		var service = new GitHubBackportService(
			new TestLoggerFactory(),
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
				"test-token"
			),
			cache
		);

		var result = await service.ResolveRootsAsync(
			Collector,
			Owner,
			Repo,
			numbers,
			updatedAt,
			TestContext.Current!.Execution.CancellationToken
		);
		await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);
		return result;
	}

	[Test]
	public async Task ResolveRoots_WithACache_AnUnchangedBackportIsNotLookedAtAgain()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Fix the thing (#100)", null, []);
		github.PullRequests[100] = Merged();
		var updatedAt = new Dictionary<int, DateTimeOffset> { [200] = UpdatedAt };

		var first = await ResolveWithCacheAsync(github, updatedAt, 200);
		var requestsAfterFirst = github.GraphQlRequests;
		var second = await ResolveWithCacheAsync(github, updatedAt, 200);

		first![200]!.Roots.Should().Equal(100);
		second![200]!.Roots.Should().Equal(100);
		second[200]!.Route.Should().Be(BackportRootRoute.Title);
		github.GraphQlRequests.Should().Be(requestsAfterFirst, "the second run asked GitHub nothing");
	}

	[Test]
	public async Task ResolveRoots_WithACache_ABackportThatChangedIsResolvedAgain()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Fix the thing (#100)", null, []);
		github.PullRequests[100] = Merged();
		github.PullRequests[101] = Merged();
		_ = await ResolveWithCacheAsync(github, new Dictionary<int, DateTimeOffset> { [200] = UpdatedAt }, 200);

		github.Backports[200] = ("Fix the thing (#101)", null, []);
		var second = await ResolveWithCacheAsync(github, new Dictionary<int, DateTimeOffset> { [200] = UpdatedAt.AddMinutes(1) }, 200);

		second![200]!.Roots.Should().Equal(101);
	}

	[Test]
	public async Task ResolveRoots_WithACache_ABackportWithoutAnOriginalIsRememberedToo()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Bump dependency", null, []);
		var updatedAt = new Dictionary<int, DateTimeOffset> { [200] = UpdatedAt };

		_ = await ResolveWithCacheAsync(github, updatedAt, 200);
		var requestsAfterFirst = github.GraphQlRequests;
		var second = await ResolveWithCacheAsync(github, updatedAt, 200);

		second![200].Should().BeNull();
		github.GraphQlRequests.Should().Be(requestsAfterFirst);
	}

	[Test]
	public async Task ResolveRoots_WithACache_OnlyTheNewBackportsAreLookedAt()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Fix one (#100)", null, []);
		github.Backports[201] = ("Fix two (#101)", null, []);
		github.PullRequests[100] = Merged();
		github.PullRequests[101] = Merged();
		var updatedAt = new Dictionary<int, DateTimeOffset> { [200] = UpdatedAt, [201] = UpdatedAt };
		_ = await ResolveWithCacheAsync(github, updatedAt, 200);

		var second = await ResolveWithCacheAsync(github, updatedAt, 200, 201);

		second![200]!.Roots.Should().Equal(100);
		second[201]!.Roots.Should().Equal(101);
	}

	[Test]
	public async Task ResolveRoots_WithACacheButNoUpdateTime_IsResolvedEveryTime()
	{
		var github = new FakeGitHub();
		github.Backports[200] = ("Fix the thing (#100)", null, []);
		github.PullRequests[100] = Merged();

		_ = await ResolveWithCacheAsync(github, [], 200);
		var requestsAfterFirst = github.GraphQlRequests;
		_ = await ResolveWithCacheAsync(github, [], 200);

		github.GraphQlRequests.Should().BeGreaterThan(requestsAfterFirst);
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
