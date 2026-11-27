// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Reads version monikers: <c>main</c>, or a numeric major such as <c>9</c>.</summary>
public static class ApiVersions
{
	public static int? TryParseMajor(string moniker) => int.TryParse(moniker, out var major) ? major : null;

	/// <summary>
	/// The minor release a version-index branch names, such as <c>9.6</c>, or null for <c>main</c>, <c>master</c>,
	/// a bare major or anything with a patch segment. The branch is what the page renders from, so it is what the label shows.
	/// </summary>
	public static string? ReleaseOf(string? branch)
	{
		var parts = branch?.Split('.');
		var isRelease = parts is [var major, var minor] && IsNumber(major) && IsNumber(minor);
		return isRelease ? branch : null;
	}

	/// <summary>Digits only, unlike <see cref="int.TryParse(string?, out int)"/>, which also accepts a sign and surrounding white space.</summary>
	private static bool IsNumber(string part) => part.Length > 0 && part.All(char.IsAsciiDigit);

	/// <summary>Sort key that puts <c>main</c> first and numeric majors after it, newest first, when sorted descending.</summary>
	public static int Recency(string moniker) => moniker == "main" ? int.MaxValue : TryParseMajor(moniker) ?? 0;

	/// <summary>The highest numeric major, or null when there is none.</summary>
	public static string? LatestMajor(IEnumerable<string> monikers) =>
		monikers.Where(static m => TryParseMajor(m) is not null).OrderByDescending(static m => TryParseMajor(m)).FirstOrDefault();

	/// <summary>
	/// The tree an API serves readers who name no version: the highest numeric major, else <c>main</c>, else the first tree.
	/// The versionless URL, the explorer's canonical document and the search index's non-hidden documents all follow it.
	/// </summary>
	public static string? DefaultTree(IEnumerable<string> monikers)
	{
		var all = monikers.ToList();
		return LatestMajor(all) ?? (all.Contains("main") ? "main" : all.FirstOrDefault());
	}
}
