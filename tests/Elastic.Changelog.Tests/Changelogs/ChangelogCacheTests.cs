// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Caching;
using Elastic.Changelog.GitHub;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// The cache only makes a run faster, so it must never be able to fail one: an unreadable file is ignored, a save writes
/// a whole file or none, and what nobody used for a month is dropped.
/// </summary>
public class ChangelogCacheTests : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), "docs-builder-cache-tests", Guid.NewGuid().ToString("N"));

	private static readonly CachedAssociation Squashed = new(
		11,
		"https://github.com/elastic/widget/pull/11",
		true,
		"main",
		"aaa",
		"elastic/widget"
	);

	private ChangelogCache Cache(TimeProvider? time = null, TimeSpan? maxAge = null) => new(_directory, NullLogger.Instance, time, maxAge);

	private string CommitsFile(string repository = "elastic/widget") => Path.Combine(_directory, "github", repository, "commits.json");

	public void Dispose()
	{
		if (Directory.Exists(_directory))
			Directory.Delete(_directory, recursive: true);

		GC.SuppressFinalize(this);
	}

	[Test]
	public async Task Commits_SavedByOneRun_AreFoundByTheNext()
	{
		var first = Cache();
		first.Commits("elastic", "widget").Set("sha1", [Squashed]);
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		var found = Cache().Commits("elastic", "widget").TryGet("sha1", out var pullRequests);

		found.Should().BeTrue();
		pullRequests.Should().Equal(Squashed);
	}

	[Test]
	public void Commits_NeverSeen_AreNotFound()
	{
		Cache().Commits("elastic", "widget").TryGet("nope", out var pullRequests).Should().BeFalse();

		pullRequests.Should().BeEmpty();
	}

	[Test]
	public async Task Commits_OfAnotherRepository_AreNotMixedUp()
	{
		var first = Cache();
		first.Commits("elastic", "widget").Set("sha1", [Squashed]);
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		Cache().Commits("elastic", "gadget").TryGet("sha1", out _).Should().BeFalse();
		File.Exists(CommitsFile("elastic/widget")).Should().BeTrue();
		File.Exists(CommitsFile("elastic/gadget")).Should().BeFalse();
	}

	[Test]
	public async Task Save_NothingChanged_WritesNothing()
	{
		var cache = Cache();
		_ = cache.Commits("elastic", "widget").TryGet("sha1", out _);

		await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		Directory.Exists(_directory).Should().BeFalse();
	}

	[Test]
	[Arguments("this is not json")]
	[Arguments("")]
	[Arguments("""{ "version": 999, "commits": { "sha1": { "prs": [], "used": 1 } } }""")]
	[Arguments("""{ "version": 1, "commits": "oops" }""")]
	public async Task Load_UnreadableOrForeignFile_IsIgnoredAndWrittenAgain(string content)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(CommitsFile())!);
		await File.WriteAllTextAsync(CommitsFile(), content, TestContext.Current!.Execution.CancellationToken);

		var cache = Cache();
		cache.Commits("elastic", "widget").TryGet("sha1", out _).Should().BeFalse();
		cache.Commits("elastic", "widget").Set("sha2", [Squashed]);
		await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		Cache().Commits("elastic", "widget").TryGet("sha2", out _).Should().BeTrue();
	}

	[Test]
	public async Task Save_EntriesNobodyUsedForTheMaxAge_AreDropped()
	{
		var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
		var first = Cache(time, TimeSpan.FromDays(30));
		first.Commits("elastic", "widget").Set("old", [Squashed]);
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		time.Advance(TimeSpan.FromDays(20));
		var second = Cache(time, TimeSpan.FromDays(30));
		second.Commits("elastic", "widget").Set("recent", [Squashed]);
		await second.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		time.Advance(TimeSpan.FromDays(15));
		var third = Cache(time, TimeSpan.FromDays(30));
		third.Commits("elastic", "widget").Set("newest", [Squashed]);
		await third.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		var last = Cache(time, TimeSpan.FromDays(30)).Commits("elastic", "widget");
		last.TryGet("old", out _).Should().BeFalse("it is 35 days old");
		last.TryGet("recent", out _).Should().BeTrue("it is 15 days old");
		last.TryGet("newest", out _).Should().BeTrue();
	}

	[Test]
	public async Task Save_AnEntryThatIsUsedKeepsLiving()
	{
		var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
		var first = Cache(time, TimeSpan.FromDays(30));
		first.Commits("elastic", "widget").Set("sha1", [Squashed]);
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		// Every 20 days a run uses it, which counts as new, so it lives on past 30 days after it was first written.
		for (var run = 0; run < 3; run++)
		{
			time.Advance(TimeSpan.FromDays(20));
			var cache = Cache(time, TimeSpan.FromDays(30));
			cache.Commits("elastic", "widget").TryGet("sha1", out _).Should().BeTrue($"run {run + 1}");
			await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);
		}
	}

	[Test]
	public async Task Save_LeavesNoTemporaryFile()
	{
		var cache = Cache();
		cache.Commits("elastic", "widget").Set("sha1", [Squashed]);

		await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		Directory.GetFiles(Path.GetDirectoryName(CommitsFile())!).Should().ContainSingle().Which.Should().EndWith("commits.json");
	}

	[Test]
	public async Task Set_FromManyThreads_KeepsEveryCommit()
	{
		var cache = Cache();
		var store = cache.Commits("elastic", "widget");

		await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
		{
			for (var i = 0; i < 500; i++)
				store.Set($"t{t}-{i}", [Squashed with { Number = i }]);
		})));
		await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		store.Count.Should().Be(4000);
		var loaded = Cache().Commits("elastic", "widget");
		loaded.Count.Should().Be(4000);
		loaded.TryGet("t7-499", out var found).Should().BeTrue();
		found.Single().Number.Should().Be(499);
	}

	[Test]
	public async Task NoPullRequest_IsBelievedOnlyOnceItIsADayOld()
	{
		var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
		var first = Cache(time);
		first.Commits("elastic", "widget").SetNoPullRequest("direct");
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		var young = Cache(time).Commits("elastic", "widget");
		young.TryGet("direct", out _).Should().BeFalse("GitHub may not have linked a young commit yet");

		time.Advance(TimeSpan.FromHours(25));
		var old = Cache(time).Commits("elastic", "widget");
		old.TryGet("direct", out var pullRequests).Should().BeTrue();
		pullRequests.Should().BeEmpty();
	}

	[Test]
	public void NoPullRequest_AskedAgainAndStillNone_KeepsTheTimeOfTheFirstAnswer()
	{
		var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
		var store = Cache(time).Commits("elastic", "widget");
		store.SetNoPullRequest("direct");

		time.Advance(TimeSpan.FromHours(20));
		store.SetNoPullRequest("direct");
		time.Advance(TimeSpan.FromHours(5));

		store.TryGet("direct", out _).Should().BeTrue("25 hours have passed since the first answer, not since the last");
	}

	[Test]
	public void NoPullRequest_ThenAMergedOne_IsReplacedByIt()
	{
		var store = Cache().Commits("elastic", "widget");
		store.SetNoPullRequest("sha1");

		store.Set("sha1", [Squashed]);

		store.TryGet("sha1", out var pullRequests).Should().BeTrue();
		pullRequests.Should().Equal(Squashed);
	}

	private static readonly DateTimeOffset Updated = new(2026, 10, 1, 12, 30, 15, TimeSpan.Zero);

	[Test]
	public async Task PullRequestText_IsFoundOnlyForTheStateItWasStoredFor()
	{
		var first = Cache();
		first.PullRequestTexts("elastic", "widget").Set(
			7,
			Updated,
			new CachedPullRequestText("Search is faster.", true, ["https://github.com/elastic/widget/issues/1"])
		);
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		var store = Cache().PullRequestTexts("elastic", "widget");
		store.TryGet(7, Updated, out var text).Should().BeTrue();
		text.ReleaseNote.Should().Be("Search is faster.");
		text.ReleaseNoteTruncated.Should().BeTrue();
		text.LinkedIssues.Should().Equal("https://github.com/elastic/widget/issues/1");

		store.TryGet(7, Updated.AddSeconds(1), out _).Should().BeFalse("the pull request changed after it was stored");
		store.TryGet(8, Updated, out _).Should().BeFalse();
	}

	[Test]
	public void PullRequestText_StoredAgainForANewerState_ReplacesTheOldOne()
	{
		var store = Cache().PullRequestTexts("elastic", "widget");
		store.Set(7, Updated, new CachedPullRequestText("Old.", false, []));

		store.Set(7, Updated.AddHours(1), new CachedPullRequestText("New.", false, []));

		store.TryGet(7, Updated, out _).Should().BeFalse();
		store.TryGet(7, Updated.AddHours(1), out var text).Should().BeTrue();
		text.ReleaseNote.Should().Be("New.");
	}

	[Test]
	public async Task PullRequestText_WithoutAReleaseNote_IsRemembered()
	{
		var first = Cache();
		first.PullRequestTexts("elastic", "widget").Set(7, Updated, new CachedPullRequestText(null, false, []));
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		Cache().PullRequestTexts("elastic", "widget").TryGet(7, Updated, out var text).Should().BeTrue();
		text.ReleaseNote.Should().BeNull();
	}

	[Test]
	public async Task Backport_WithAnOriginal_AndWithoutOne_AreBothRemembered()
	{
		var first = Cache();
		var store = first.Backports("elastic", "widget");
		store.Set(20, Updated, new CachedBackport(new BackportRoots([5, 6], BackportRootRoute.CherryPickTrailer, RoutesDisagree: true)));
		store.Set(21, Updated, new CachedBackport(null));
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		var loaded = Cache().Backports("elastic", "widget");
		loaded.TryGet(20, Updated, out var found).Should().BeTrue();
		found.Roots!.Roots.Should().Equal(5, 6);
		found.Roots.Route.Should().Be(BackportRootRoute.CherryPickTrailer);
		found.Roots.RoutesDisagree.Should().BeTrue();
		loaded.TryGet(21, Updated, out var none).Should().BeTrue("looking again at the same text finds the same");
		none.Roots.Should().BeNull();
		loaded.TryGet(20, Updated.AddMinutes(1), out _).Should().BeFalse();
	}

	[Test]
	public async Task VersionedStores_UnusedForTheMaxAge_AreDropped()
	{
		var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
		var first = Cache(time, TimeSpan.FromDays(30));
		first.PullRequestTexts("elastic", "widget").Set(7, Updated, new CachedPullRequestText(null, false, []));
		first.Backports("elastic", "widget").Set(7, Updated, new CachedBackport(null));
		await first.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		time.Advance(TimeSpan.FromDays(31));
		var second = Cache(time, TimeSpan.FromDays(30));
		second.PullRequestTexts("elastic", "widget").Set(8, Updated, new CachedPullRequestText(null, false, []));
		second.Backports("elastic", "widget").Set(8, Updated, new CachedBackport(null));
		await second.SaveAsync(TestContext.Current!.Execution.CancellationToken);

		var last = Cache(time, TimeSpan.FromDays(30));
		last.PullRequestTexts("elastic", "widget").TryGet(7, Updated, out _).Should().BeFalse();
		last.PullRequestTexts("elastic", "widget").TryGet(8, Updated, out _).Should().BeTrue();
		last.Backports("elastic", "widget").TryGet(7, Updated, out _).Should().BeFalse();
		last.Backports("elastic", "widget").TryGet(8, Updated, out _).Should().BeTrue();
	}

	[Test]
	[Arguments("pulls.json")]
	[Arguments("backports.json")]
	public async Task VersionedStores_UnreadableFile_IsIgnored(string fileName)
	{
		var path = Path.Combine(_directory, "github", "elastic/widget", fileName);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		await File.WriteAllTextAsync(path, "garbage", TestContext.Current!.Execution.CancellationToken);

		var cache = Cache();
		cache.PullRequestTexts("elastic", "widget").TryGet(7, Updated, out _).Should().BeFalse();
		cache.Backports("elastic", "widget").TryGet(7, Updated, out _).Should().BeFalse();
	}

	[Test]
	public void DefaultDirectory_IsUnderTheApplicationDataOfDocsBuilder() =>
		ChangelogCache.DefaultDirectory().Should().EndWith(Path.Combine("docs-builder", "changelog-cache"));
}
