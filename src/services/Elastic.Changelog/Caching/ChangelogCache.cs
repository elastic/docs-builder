// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Changelog.GitHub;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Caching;

/// <summary>
/// What GitHub said about the pull requests of a commit. These facts do not change once a pull request is merged, so they are
/// kept between runs. Labels and titles do change, and are never kept here.
/// </summary>
/// <param name="Number">The pull request number in the repository the range was resolved against.</param>
/// <param name="Url">Its URL.</param>
/// <param name="Merged">Whether it is merged.</param>
/// <param name="BaseRefName">The branch it was merged into.</param>
/// <param name="MergeCommit">The commit that merging it made, when GitHub names one.</param>
/// <param name="BaseRepository">The repository it was merged into, as <c>owner/repo</c>.</param>
public sealed record CachedAssociation(
	int Number,
	string? Url,
	bool Merged,
	string? BaseRefName,
	string? MergeCommit,
	string? BaseRepository
);

/// <summary>
/// What an entry takes from the text of a pull request: the release note and the issues it fixes. The text is read again when the
/// pull request changes, and the pull request says when that was, so these stay valid until its <c>updatedAt</c> moves.
/// </summary>
/// <param name="ReleaseNote">The release note section of the body, when it has one.</param>
/// <param name="ReleaseNoteTruncated">Whether more than that section followed it.</param>
/// <param name="LinkedIssues">The issues the body says the pull request fixes, as URLs.</param>
public sealed record CachedPullRequestText(
	string? ReleaseNote,
	bool ReleaseNoteTruncated,
	IReadOnlyList<string> LinkedIssues,
	string? Impact = null,
	string? Action = null
);

/// <summary>
/// Where a backport came from, as found from its text, its commits and GitHub. A backport that no original was found for is
/// remembered too, because looking again at the same text finds the same.
/// </summary>
/// <param name="Roots">The original pull requests, or <c>null</c> when none was found.</param>
public sealed record CachedBackport(BackportRoots? Roots);

/// <summary>
/// A persistent store of what a run learned from GitHub, shared by every command that reads a range of commits. It is keyed by
/// repository, commit and pull request, and knows nothing of the product, the version or the command that asked, so a serverless
/// promotion, a unified release and an ad hoc range all reuse each other's answers.
/// <para>
/// It lives in a directory, <c>github/{owner}/{repo}/*.json</c>. A file that cannot be read, or was written by another schema
/// version, is ignored and written again: the cache can only make a run faster, never fail it. Entries that were not used for
/// <see cref="DefaultMaxAge"/> are dropped when it is saved.
/// </para>
/// </summary>
public sealed partial class ChangelogCache(string directory, ILogger logger, TimeProvider? time = null, TimeSpan? maxAge = null)
{
	// 2: pull request texts gained the Impact and Action sections.
	public const int SchemaVersion = 2;
	public static readonly TimeSpan DefaultMaxAge = TimeSpan.FromDays(30);

	private static readonly TimeSpan TouchAfter = TimeSpan.FromDays(1);

	/// <summary>
	/// How old an answer of "no merged pull request" has to be before it is believed. GitHub may not have linked a commit to its pull
	/// request yet, so a young commit is asked about again. One that is still without one after a day will stay without.
	/// </summary>
	public static readonly TimeSpan NoPullRequestSettlesAfter = TimeSpan.FromDays(1);

	private readonly TimeProvider _time = time ?? TimeProvider.System;
	private readonly TimeSpan _maxAge = maxAge ?? DefaultMaxAge;
	private readonly ConcurrentDictionary<string, Lazy<CommitStore>> _commits = [];
	private readonly ConcurrentDictionary<string, Lazy<VersionedStore<CachedPullRequestText>>> _pullRequestTexts = [];
	private readonly ConcurrentDictionary<string, Lazy<VersionedStore<CachedBackport>>> _backports = [];

	/// <summary>Where the cache lives unless the caller says otherwise: the per-user application data of docs-builder.</summary>
	public static string DefaultDirectory()
	{
		var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		// Containers often have no home directory, which leaves this empty.
		if (string.IsNullOrWhiteSpace(root))
			root = Path.GetTempPath();

		return Path.Combine(root, "docs-builder", "changelog-cache");
	}

	/// <summary>
	/// The cache for a run: in <paramref name="directory"/>, or in <see cref="DefaultDirectory"/> when none is given, and none at all
	/// when <paramref name="disabled"/>.
	/// </summary>
	public static ChangelogCache? Create(DirectoryInfo? directory, bool disabled, ILogger logger)
	{
		if (disabled)
			return null;

		var path = directory?.FullName ?? DefaultDirectory();
		logger.LogInformation("Changelog cache: {Directory}", path);
		return new ChangelogCache(path, logger);
	}

	/// <summary>The commit-to-pull-request facts of a repository.</summary>
	public CommitStore Commits(string owner, string repo) =>
		_commits.GetOrAdd($"{owner}/{repo}", key => new Lazy<CommitStore>(() => LoadCommits(key))).Value;

	/// <summary>The release notes and linked issues of the pull requests of a repository, valid until a pull request changes.</summary>
	public VersionedStore<CachedPullRequestText> PullRequestTexts(string owner, string repo) =>
		_pullRequestTexts.GetOrAdd(
			$"{owner}/{repo}",
			key => new Lazy<VersionedStore<CachedPullRequestText>>(
				() => LoadVersioned(key, "pulls.json", ChangelogCacheJsonContext.Default.VersionedFileCachedPullRequestText)
			)
		).Value;

	/// <summary>The originals of the backports of a repository, valid until a backport changes.</summary>
	public VersionedStore<CachedBackport> Backports(string owner, string repo) =>
		_backports.GetOrAdd(
			$"{owner}/{repo}",
			key => new Lazy<VersionedStore<CachedBackport>>(
				() => LoadVersioned(key, "backports.json", ChangelogCacheJsonContext.Default.VersionedFileCachedBackport)
			)
		).Value;

	/// <summary>Writes every store that changed. A store that cannot be written is logged and skipped.</summary>
	public async Task SaveAsync(Cancel ctx)
	{
		foreach (var (key, store) in _commits)
		{
			if (!store.IsValueCreated || !store.Value.IsDirty)
				continue;

			await SaveAsync(
				FilePath(key, "commits.json"),
				store.Value.ToFile(_time.GetUtcNow(), _maxAge),
				ChangelogCacheJsonContext.Default.CommitsFile,
				ctx
			).ConfigureAwait(false);
		}

		foreach (var (key, store) in _pullRequestTexts)
		{
			if (store.IsValueCreated && store.Value.IsDirty)
				await SaveAsync(
					FilePath(key, "pulls.json"),
					store.Value.ToFile(_time.GetUtcNow(), _maxAge),
					ChangelogCacheJsonContext.Default.VersionedFileCachedPullRequestText,
					ctx
				).ConfigureAwait(false);
		}

		foreach (var (key, store) in _backports)
		{
			if (store.IsValueCreated && store.Value.IsDirty)
				await SaveAsync(
					FilePath(key, "backports.json"),
					store.Value.ToFile(_time.GetUtcNow(), _maxAge),
					ChangelogCacheJsonContext.Default.VersionedFileCachedBackport,
					ctx
				).ConfigureAwait(false);
		}
	}

	private string FilePath(string repository, string file) =>
		Path.Combine(directory, "github", repository.Replace('/', Path.DirectorySeparatorChar), file);

	private VersionedStore<TValue> LoadVersioned<TValue>(
		string repository,
		string fileName,
		System.Text.Json.Serialization.Metadata.JsonTypeInfo<VersionedFile<TValue>> typeInfo
	) where TValue : class
	{
		var path = FilePath(repository, fileName);
		try
		{
			if (!File.Exists(path))
				return new VersionedStore<TValue>(_time, []);

			using var stream = File.OpenRead(path);
			var file = JsonSerializer.Deserialize(stream, typeInfo);
			if (file is { Version: SchemaVersion, Entries: { } entries } && entries.Values.All(e => e?.Value is not null))
				return new VersionedStore<TValue>(_time, entries);

			logger.LogInformation("Ignoring the cache file {File}: it was written by another version", path);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
		{
			logger.LogWarning("Ignoring the cache file {File}: {Reason}", path, e.Message);
		}

		return new VersionedStore<TValue>(_time, []);
	}

	private CommitStore LoadCommits(string repository)
	{
		var path = FilePath(repository, "commits.json");
		try
		{
			if (!File.Exists(path))
				return new CommitStore(_time, []);

			using var stream = File.OpenRead(path);
			var file = JsonSerializer.Deserialize(stream, ChangelogCacheJsonContext.Default.CommitsFile);
			if (
				file is { Version: SchemaVersion, Entries: { } entries }
				&& entries.Values.All(e => e is { PullRequests: { } prs } && prs.All(p => p is not null))
			)
				return new CommitStore(_time, entries);

			logger.LogInformation("Ignoring the cache file {File}: it was written by another version", path);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
		{
			logger.LogWarning("Ignoring the cache file {File}: {Reason}", path, e.Message);
		}

		return new CommitStore(_time, []);
	}

	/// <summary>Writes to a temporary file and moves it into place, so a reader or a crash never sees half a file.</summary>
	private async Task SaveAsync<TFile>(
		string path,
		TFile file,
		System.Text.Json.Serialization.Metadata.JsonTypeInfo<TFile> typeInfo,
		Cancel ctx
	)
	{
		var temp = $"{path}.{Guid.NewGuid():N}.tmp";
		try
		{
			_ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			await using (var stream = File.Create(temp))
				await JsonSerializer.SerializeAsync(stream, file, typeInfo, ctx).ConfigureAwait(false);

			File.Move(temp, path, overwrite: true);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			logger.LogWarning("Could not write the cache file {File}: {Reason}", path, e.Message);
			TryDelete(temp);
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			// A leftover temporary file is harmless.
		}
	}

	/// <summary>The commit-to-pull-request facts of one repository.</summary>
	public sealed class CommitStore
	{
		private readonly TimeProvider _time;
		private readonly ConcurrentDictionary<string, CommitEntry> _entries;
		private int _dirty;

		internal CommitStore(TimeProvider time, Dictionary<string, CommitEntry> entries)
		{
			_time = time;
			_entries = new ConcurrentDictionary<string, CommitEntry>(entries);
		}

		public int Count => _entries.Count;

		internal bool IsDirty => Volatile.Read(ref _dirty) == 1;

		/// <summary>
		/// The pull requests of a commit, when an earlier run found them. An empty list is the answer "no merged pull request", and it
		/// counts only once a later answer, given after <see cref="NoPullRequestSettlesAfter"/>, has confirmed it.
		/// </summary>
		public bool TryGet(string sha, out IReadOnlyList<CachedAssociation> pullRequests)
		{
			var now = _time.GetUtcNow().ToUnixTimeSeconds();
			if (!_entries.TryGetValue(sha, out var entry) || (entry.PullRequests.Count == 0 && !entry.Confirmed))
			{
				pullRequests = [];
				return false;
			}

			if (now - Volatile.Read(ref entry.UsedAt) > TouchAfter.TotalSeconds)
			{
				Volatile.Write(ref entry.UsedAt, now);
				_ = Interlocked.Exchange(ref _dirty, 1);
			}

			pullRequests = entry.PullRequests;
			return true;
		}

		/// <summary>Remembers the merged pull requests of a commit. That fact does not change.</summary>
		public void Set(string sha, IReadOnlyList<CachedAssociation> pullRequests)
		{
			var now = _time.GetUtcNow().ToUnixTimeSeconds();
			_entries[sha] = new CommitEntry { PullRequests = [.. pullRequests], UsedAt = now, FirstSeen = now };
			_ = Interlocked.Exchange(ref _dirty, 1);
		}

		/// <summary>
		/// Remembers that a commit has no merged pull request. The time of the first such answer is kept, because that is what decides
		/// when it is believed.
		/// </summary>
		public void SetNoPullRequest(string sha)
		{
			var now = _time.GetUtcNow().ToUnixTimeSeconds();
			_ = _entries.AddOrUpdate(sha, _ => new CommitEntry { UsedAt = now, FirstSeen = now }, (_, existing) =>
			{
				if (existing.PullRequests.Count > 0)
					return new CommitEntry { UsedAt = now, FirstSeen = now };

				// Only a fresh answer given after the settling interval confirms the earlier negative.
				if (now - existing.FirstSeen >= NoPullRequestSettlesAfter.TotalSeconds)
					existing.Confirmed = true;
				existing.UsedAt = now;
				return existing;
			});
			_ = Interlocked.Exchange(ref _dirty, 1);
		}

		internal CommitsFile ToFile(DateTimeOffset now, TimeSpan maxAge)
		{
			var oldest = now.Subtract(maxAge).ToUnixTimeSeconds();
			return new CommitsFile
			{
				Version = SchemaVersion,
				Entries = _entries.Where(e => Volatile.Read(ref e.Value.UsedAt) >= oldest).ToDictionary(
					e => e.Key,
					e => e.Value,
					StringComparer.Ordinal
				)
			};
		}
	}

	/// <summary>
	/// What was learned about the pull requests of one repository, each valid for one state of the pull request. The state is its
	/// <c>updatedAt</c>: an answer is found only when GitHub still reports the time it was stored with.
	/// </summary>
	public sealed class VersionedStore<TValue> where TValue : class
	{
		private readonly TimeProvider _time;
		private readonly ConcurrentDictionary<int, VersionedEntry<TValue>> _entries;
		private int _dirty;

		internal VersionedStore(TimeProvider time, Dictionary<int, VersionedEntry<TValue>> entries)
		{
			_time = time;
			_entries = new ConcurrentDictionary<int, VersionedEntry<TValue>>(entries);
		}

		public int Count => _entries.Count;

		internal bool IsDirty => Volatile.Read(ref _dirty) == 1;

		/// <summary>The answer stored for the pull request, when it was stored for exactly this <paramref name="updatedAt"/>.</summary>
		public bool TryGet(int number, DateTimeOffset updatedAt, out TValue value)
		{
			if (
				_entries.TryGetValue(number, out var entry) && entry.Value is { } stored && entry.UpdatedAt == updatedAt.ToUnixTimeSeconds()
			)
			{
				var now = _time.GetUtcNow().ToUnixTimeSeconds();
				if (now - Volatile.Read(ref entry.UsedAt) > TouchAfter.TotalSeconds)
				{
					Volatile.Write(ref entry.UsedAt, now);
					_ = Interlocked.Exchange(ref _dirty, 1);
				}

				value = stored;
				return true;
			}

			value = null!;
			return false;
		}

		/// <summary>Stores the answer for the state of the pull request that <paramref name="updatedAt"/> names, in place of an older one.</summary>
		public void Set(int number, DateTimeOffset updatedAt, TValue value)
		{
			_entries[number] = new VersionedEntry<TValue>
			{
				UpdatedAt = updatedAt.ToUnixTimeSeconds(),
				UsedAt = _time.GetUtcNow().ToUnixTimeSeconds(),
				Value = value
			};
			_ = Interlocked.Exchange(ref _dirty, 1);
		}

		internal VersionedFile<TValue> ToFile(DateTimeOffset now, TimeSpan maxAge)
		{
			var oldest = now.Subtract(maxAge).ToUnixTimeSeconds();
			return new VersionedFile<TValue>
			{
				Version = SchemaVersion,
				Entries = _entries.Where(e => Volatile.Read(ref e.Value.UsedAt) >= oldest).ToDictionary(e => e.Key, e => e.Value)
			};
		}
	}

	internal sealed class VersionedEntry<TValue> where TValue : class
	{
		/// <summary>The <c>updatedAt</c> the value was read for, in Unix seconds.</summary>
		[JsonPropertyName("updated")]
		public long UpdatedAt;

		/// <summary>When a run last used the entry, in Unix seconds. Entries nobody uses for a month are dropped.</summary>
		[JsonPropertyName("used")]
		public long UsedAt;

		[JsonPropertyName("value")]
		public TValue? Value { get; set; }
	}

	internal sealed class VersionedFile<TValue> where TValue : class
	{
		[JsonPropertyName("version")]
		public int Version { get; set; }

		[JsonPropertyName("entries")]
		public Dictionary<int, VersionedEntry<TValue>>? Entries { get; set; }
	}

	internal sealed class CommitEntry
	{
		[JsonPropertyName("prs")]
		public List<CachedAssociation> PullRequests { get; set; } = [];

		/// <summary>When a run last used the entry, in Unix seconds. Entries nobody uses for a month are dropped.</summary>
		[JsonPropertyName("used")]
		public long UsedAt;

		/// <summary>When the entry was first written, in Unix seconds.</summary>
		[JsonPropertyName("seen")]
		public long FirstSeen;

		/// <summary>Whether a run asked GitHub again after the settling interval and still found no pull request.</summary>
		[JsonPropertyName("confirmed")]
		public bool Confirmed;
	}

	internal sealed class CommitsFile
	{
		[JsonPropertyName("version")]
		public int Version { get; set; }

		[JsonPropertyName("commits")]
		public Dictionary<string, CommitEntry>? Entries { get; set; }
	}

	[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, IncludeFields = true)]
	[JsonSerializable(typeof(CommitsFile))]
	[JsonSerializable(typeof(VersionedFile<CachedPullRequestText>))]
	[JsonSerializable(typeof(VersionedFile<CachedBackport>))]
	internal sealed partial class ChangelogCacheJsonContext : JsonSerializerContext;
}
