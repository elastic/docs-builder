// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Globbing;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>
/// The decision of the sync planner with the I/O taken out: which local files are added, updated or skipped against what the remote listing holds, and
/// which remote-only keys are deleted. <see cref="AwsS3SyncPlanStrategy"/> lists, hashes and checks the file system, then asks this.
/// </summary>
public static class SyncPlanner
{
	/// <summary>
	/// Classifies every local file and remote key. A local file (by its destination path) is excluded, added (not remote), skipped (same ETag) or updated
	/// (different ETag); a remote key that is not excluded and has no local file is deleted.
	/// </summary>
	/// <param name="localFiles">The local files that are not symlinks: the full local path and the destination path (slash separated, relative to the output).</param>
	/// <param name="remoteETags">The remote listing: key to ETag (quotes already trimmed).</param>
	/// <param name="localETag">The ETag of a local file, asked by its local path only for a file that is also remote.</param>
	/// <param name="localExists">Whether a local file for a remote key exists (a symlink exists although it is not in <paramref name="localFiles"/>).</param>
	/// <param name="excludes">The exclude globs, already normalised.</param>
	public static (List<DeleteRequest> Deletes, List<AddRequest> Adds, List<UpdateRequest> Updates, List<SkipRequest> Skips) Compute(
		IEnumerable<(string LocalPath, string DestinationPath)> localFiles,
		IReadOnlyDictionary<string, string> remoteETags,
		Func<string, string> localETag,
		Func<string, bool> localExists,
		Glob[] excludes
	)
	{
		var deletes = new List<DeleteRequest>();
		var adds = new List<AddRequest>();
		var updates = new List<UpdateRequest>();
		var skips = new List<SkipRequest>();

		foreach (var (localPath, destinationPath) in localFiles)
		{
			if (IsExcluded(destinationPath, excludes))
				continue;

			if (remoteETags.TryGetValue(destinationPath, out var remoteETag))
			{
				if (localETag(localPath) == remoteETag)
					skips.Add(new SkipRequest { LocalPath = localPath, DestinationPath = destinationPath });
				else
					updates.Add(new UpdateRequest { LocalPath = localPath, DestinationPath = destinationPath });
			}
			else
				adds.Add(new AddRequest { LocalPath = localPath, DestinationPath = destinationPath });
		}

		foreach (var key in remoteETags.Keys)
		{
			if (IsExcluded(key, excludes) || localExists(key))
				continue;
			deletes.Add(new DeleteRequest { DestinationPath = key });
		}
		return (deletes, adds, updates, skips);
	}

	/// <summary>Whether any exclude glob matches the destination path.</summary>
	public static bool IsExcluded(string destinationPath, Glob[] globs) => globs.Length > 0 && globs.Any(g => g.IsMatch(destinationPath));

	/// <summary>
	/// Normalize an exclude pattern so it behaves like <c>aws s3 sync --exclude</c>, where
	/// <c>*</c> matches path separators (e.g. <c>_preview/*</c> matches
	/// <c>_preview/pr-42/index.html</c>).  A trailing <c>/*</c> is expanded to <c>/**</c> so
	/// DotNet.Glob treats it as a recursive wildcard.
	/// </summary>
	public static string NormalizePattern(string pattern) =>
		pattern.EndsWith("/*", StringComparison.Ordinal) ? string.Concat(pattern.AsSpan(0, pattern.Length - 2), "/**") : pattern;
}
