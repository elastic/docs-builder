// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.RegularExpressions;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Merges parent bundle entries with amend files (exclusions first, then additions per amend).
/// </summary>
public static partial class BundleAmendMerger
{
	[GeneratedRegex(@"\.amend-(\d+)(\.ya?ml)$", RegexOptions.IgnoreCase)]
	private static partial Regex AmendFileRegex();

	/// <summary>Whether a path is an amend sidecar (<c>{name}.amend-{N}.yaml</c>).</summary>
	public static bool IsAmendFile(string filePath) => AmendFileRegex().IsMatch(filePath);

	/// <summary>Numeric suffix from an amend file path; <c>0</c> when not an amend file.</summary>
	public static int GetAmendFileNumber(string filePath)
	{
		var match = AmendFileRegex().Match(filePath);
		return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
			? number
			: 0;
	}

	/// <summary>
	/// Parent bundle path for an amend sidecar, keeping the amend's extension
	/// (<c>repo-9.3.0.amend-1.yaml</c> → <c>repo-9.3.0.yaml</c>); null when
	/// <paramref name="filePath"/> is not an amend file. Works on bare file names and full paths alike.
	/// </summary>
	public static string? GetParentBundlePath(string filePath)
	{
		var match = AmendFileRegex().Match(filePath);
		return match.Success
			? string.Concat(filePath.AsSpan(0, match.Index), match.Groups[2].Value)
			: null;
	}

	/// <summary>
	/// Applies amend bundles in order to parent entries and returns the effective entry list.
	/// </summary>
	public static List<BundledEntry> MergeEntries(
		IReadOnlyList<BundledEntry> parentEntries,
		IReadOnlyList<Bundle> amendBundlesInOrder)
	{
		var current = parentEntries.ToList();
		foreach (var amend in amendBundlesInOrder)
			current = ApplySingleAmend(current, amend);
		return current;
	}

	/// <summary>
	/// Collects all exclusion keys already applied by prior amend files.
	/// </summary>
	public static HashSet<string> CollectAppliedExclusionKeys(
		IReadOnlyList<Bundle> amendBundlesInOrder)
	{
		var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var amend in amendBundlesInOrder)
		{
			foreach (var exclusion in amend.ExcludeEntries)
				_ = keys.Add(BuildExclusionKey(exclusion));
		}
		return keys;
	}

	/// <summary>Whether a bundled entry matches an exclusion record.</summary>
	public static bool EntryMatchesExclusion(BundledEntry entry, BundledEntry exclusion)
	{
		var entryFileName = NormalizeFileName(entry.File?.Name);
		var exclusionFileName = NormalizeFileName(exclusion.File?.Name);
		if (string.IsNullOrEmpty(entryFileName) || string.IsNullOrEmpty(exclusionFileName))
			return false;

		if (!string.Equals(entryFileName, exclusionFileName, StringComparison.OrdinalIgnoreCase))
			return false;

		if (string.IsNullOrWhiteSpace(exclusion.File?.Checksum))
			return true;

		return string.Equals(entry.File?.Checksum, exclusion.File.Checksum, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Builds a stable key for an exclusion record (file name + checksum).</summary>
	public static string BuildExclusionKey(BundledEntry exclusion)
	{
		var name = NormalizeFileName(exclusion.File?.Name) ?? string.Empty;
		var checksum = exclusion.File?.Checksum ?? string.Empty;
		return $"{name}|{checksum}";
	}

	private static List<BundledEntry> ApplySingleAmend(IReadOnlyList<BundledEntry> entries, Bundle amend)
	{
		var result = ApplyExclusions(entries, amend.ExcludeEntries);
		if (amend.Entries.Count > 0)
			result.AddRange(amend.Entries);
		return result;
	}

	private static List<BundledEntry> ApplyExclusions(
		IReadOnlyList<BundledEntry> entries,
		IReadOnlyList<BundledEntry> exclusions)
	{
		if (exclusions.Count == 0)
			return entries.ToList();

		return entries
			.Where(entry => !exclusions.Any(exclusion => EntryMatchesExclusion(entry, exclusion)))
			.ToList();
	}

	private static string? NormalizeFileName(string? fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
			return null;

		var normalized = fileName.Replace('\\', '/');
		var lastSlash = normalized.LastIndexOf('/');
		return lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
	}
}
