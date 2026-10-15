// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Caching;
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
	public void DefaultDirectory_IsUnderTheApplicationDataOfDocsBuilder() =>
		ChangelogCache.DefaultDirectory().Should().EndWith(Path.Combine("docs-builder", "changelog-cache"));
}
