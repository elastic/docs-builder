// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Nullean.Snapshots;

namespace Elastic.Documentation.Testing.Linked;

/// <summary>
/// Keeps the names of private repositories out of a snapshot. The names are not listed anywhere: a test derives them from the
/// <c>private: true</c> repositories of the assembler config it loaded and passes them to <see cref="For"/>, so the frozen
/// configuration copies and the verified files never carry a real name. Also scrubs values that change from run to run.
/// Self-contained on purpose, so it does not collide with other scrubbers added for the configuration goldens.
/// </summary>
internal static partial class PrivateRepositoryScrubbing
{
	internal const string PlaceholderPrefix = "private-repo-";

	/// <summary>
	/// A scrubber that replaces each private repository with <c>private-repo-N</c>, numbered in the order of the sorted names.
	/// Names that already are placeholders are left alone, so a frozen copy that was scrubbed earlier is unchanged.
	/// <para>
	/// A name with a hyphen is distinctive enough to match as a whole token anywhere. A single word such as <c>cloud</c> is also an
	/// ordinary word in paths and prose, so it only matches where a repository can stand: before <c>://</c> or after <c>elastic/</c>.
	/// </para>
	/// </summary>
	internal static Func<string, string> For(IEnumerable<string> privateRepositories)
	{
		var names = privateRepositories
			.Where(n => !string.IsNullOrWhiteSpace(n) && !n.StartsWith(PlaceholderPrefix, StringComparison.Ordinal))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToArray();
		if (names.Length == 0)
			return text => text;

		var replacements = names.Select((n, i) => (Pattern: PatternFor(n), Placeholder: $"{PlaceholderPrefix}{i + 1}")).ToArray();
		return text =>
		{
			foreach (var (pattern, placeholder) in replacements)
				text = pattern.Replace(text, placeholder);
			return text;
		};
	}

	/// <summary>
	/// The private-repository scrubber and the volatile-value scrubbers as options for one snapshot call. For one test class to use as
	/// <c>.MatchSnapshot(options: PrivateRepositoryScrubbing.Options(names))</c>.
	/// </summary>
	internal static SnapshotOptions Options(IEnumerable<string> privateRepositories) =>
		new SnapshotOptions().Scrub(For(privateRepositories)).Scrub(VolatileValues);

	/// <summary>A 40 digit commit SHA becomes <c>{sha}</c>; an ISO date, with or without a time, becomes <c>{date}</c>.</summary>
	internal static string VolatileValues(string text) => IsoDate().Replace(Sha().Replace(text, "{sha}"), "{date}");

	private static Regex PatternFor(string name)
	{
		var escaped = Regex.Escape(name);
		var pattern = name.Contains('-')
			? $"(?<![\\w.-]){escaped}(?![\\w-])"
			: $"(?<![\\w./-]){escaped}(?=://)|(?<=elastic/){escaped}(?![\\w-])";
		return new Regex(pattern, RegexOptions.CultureInvariant, RegexTimeouts.MatchTimeout);
	}

	[GeneratedRegex("(?<![0-9a-fA-F])[0-9a-f]{40}(?![0-9a-fA-F])", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex Sha();

	[GeneratedRegex(@"\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})?)?", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex IsoDate();
}
