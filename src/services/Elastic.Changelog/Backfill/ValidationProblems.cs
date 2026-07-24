// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Helper for building readable validation messages. Nested parts (an entry inside a
/// release inside a document) validate themselves without knowing where they sit; the
/// containing part then prefixes the new messages with its position, so the final message
/// reads like <c>releases[2]: entries[0]: An entry needs a non-empty title.</c>
/// </summary>
internal static class ValidationProblems
{
	/// <summary>Prefixes every problem added at or after <paramref name="firstNewIndex"/> with <paramref name="prefix"/>.</summary>
	public static void PrefixNew(IList<string> problems, int firstNewIndex, string prefix)
	{
		for (var i = firstNewIndex; i < problems.Count; i++)
			problems[i] = $"{prefix}: {problems[i]}";
	}
}
