// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
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
	public const int SchemaVersion = 1;
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

	/// <summary>Writes every store that changed. A store that cannot be written is logged and skipped.</summary>
	public async Task SaveAsync(Cancel ctx)
	{
		foreach (var (key, store) in _commits)
		{
			if (!store.IsValueCreated || !store.Value.IsDirty)
				continue;

			await SaveAsync(FilePath(key, "commits.json"), store.Value.ToFile(_time.GetUtcNow(), _maxAge), ctx).ConfigureAwait(false);
		}
	}

	private string FilePath(string repository, string file) =>
		Path.Combine(directory, "github", repository.Replace('/', Path.DirectorySeparatorChar), file);

	private CommitStore LoadCommits(string repository)
	{
		var path = FilePath(repository, "commits.json");
		try
		{
			if (!File.Exists(path))
				return new CommitStore(_time, []);

			using var stream = File.OpenRead(path);
			var file = JsonSerializer.Deserialize(stream, ChangelogCacheJsonContext.Default.CommitsFile);
			if (file is { Version: SchemaVersion, Entries: { } entries })
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
	private async Task SaveAsync(string path, CommitsFile file, Cancel ctx)
	{
		var temp = $"{path}.{Guid.NewGuid():N}.tmp";
		try
		{
			_ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			await using (var stream = File.Create(temp))
				await JsonSerializer.SerializeAsync(stream, file, ChangelogCacheJsonContext.Default.CommitsFile, ctx).ConfigureAwait(false);

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
		/// counts only once it has stood for <see cref="NoPullRequestSettlesAfter"/>.
		/// </summary>
		public bool TryGet(string sha, out IReadOnlyList<CachedAssociation> pullRequests)
		{
			var now = _time.GetUtcNow().ToUnixTimeSeconds();
			if (
				!_entries.TryGetValue(sha, out var entry)
				|| (entry.PullRequests.Count == 0 && now - entry.FirstSeen < NoPullRequestSettlesAfter.TotalSeconds)
			)
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
			_ = _entries.AddOrUpdate(
				sha,
				_ => new CommitEntry { UsedAt = now, FirstSeen = now },
				(_, existing) => existing.PullRequests.Count == 0 ? existing : new CommitEntry { UsedAt = now, FirstSeen = now }
			);
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
	internal sealed partial class ChangelogCacheJsonContext : JsonSerializerContext;
}
