// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Reads version monikers: <c>main</c>, or a numeric major such as <c>9</c>.</summary>
public static class ApiVersions
{
	public static int? TryParseMajor(string moniker) => int.TryParse(moniker, out var major) ? major : null;

	/// <summary>The highest numeric major, or null when there is none.</summary>
	public static string? LatestMajor(IEnumerable<string> monikers) =>
		monikers.Where(static m => TryParseMajor(m) is not null).OrderByDescending(static m => TryParseMajor(m)).FirstOrDefault();
}
