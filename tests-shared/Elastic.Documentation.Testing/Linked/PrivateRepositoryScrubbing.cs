// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Nullean.Snapshots;

namespace Elastic.Documentation.Testing.Linked;

/// <summary>
/// Keeps the names of private repositories out of a snapshot. The names are not listed anywhere: a test derives them from the
/// <c>private: true</c> repositories of the assembler config it loaded. A name is scrubbed to <c>private-repo_N</c> in any case with
/// <see cref="Scrubbers.Values"/>, and forbidden with <see cref="SnapshotOptions.Forbid"/> as exact values, so a snapshot or verified
/// file that names one fails without the name being printed. <see cref="LeakingLines"/> is the pattern check for files a test scans. A name that already is a <c>private-repo-N</c> placeholder is left alone.
/// </summary>
internal static partial class PrivateRepositoryScrubbing
{
	internal const string Placeholder = "private-repo";

	/// <summary>The names that are not placeholders already, in name order.</summary>
	internal static string[] Real(IEnumerable<string> privateRepositories) =>
		[
			.. privateRepositories
				.Where(n => !string.IsNullOrWhiteSpace(n) && !PlaceholderName().IsMatch(n))
				.Distinct(StringComparer.Ordinal)
				.Order(StringComparer.Ordinal)
		];

	/// <summary>
	/// The literal values <see cref="SnapshotOptions.Forbid"/> takes, in the spellings of the config, lower case and upper case. A name
	/// with a hyphen is forbidden on its own. A single word is an ordinary word in prose, so only <c>name://</c>, <c>elastic/name/</c>,
	/// <c>elastic/name.git</c> and <c>elastic/name"</c> are. A literal cannot say "followed by anything but a word character or a hyphen",
	/// and it cannot match another case: <see cref="LeakingLines"/> does both, for the files a test scans.
	/// </summary>
	internal static string[] Forbidden(IEnumerable<string> privateRepositories) =>
		[
			.. Real(privateRepositories)
				.SelectMany(n => new[] { n, n.ToLowerInvariant(), n.ToUpperInvariant() })
				.Distinct(StringComparer.Ordinal)
				.SelectMany(n => n.Contains('-') ? [n] : new[] { $"{n}://", $"elastic/{n}/", $"elastic/{n}.git", $"elastic/{n}\"" })
		];

	/// <summary>
	/// The 1-based numbers of the lines of <paramref name="text"/> that name a private repository, in any case. A name with a hyphen
	/// matches as a whole token. A single word matches before <c>://</c> or after <c>elastic/</c>, and then up to anything that is not a
	/// word character or a hyphen, the end of the line included. Gives line numbers, never a name.
	/// </summary>
	internal static List<int> LeakingLines(string text, IEnumerable<string> privateRepositories)
	{
		var patterns = Real(privateRepositories).Select(Pattern).ToArray();
		return [
			.. text
				.ReplaceLineEndings("\n")
				.Split('\n')
				.Select((line, i) => (line, number: i + 1))
				.Where(l => patterns.Any(p => p.IsMatch(l.line)))
				.Select(l => l.number)
		];
	}

	private static Regex Pattern(string name)
	{
		var escaped = Regex.Escape(name);
		var pattern = name.Contains('-')
			? $"(?<![\\w.-]){escaped}(?![\\w-])"
			: $"(?<![\\w./-]){escaped}(?=://)|(?<=elastic/){escaped}(?![\\w-])";
		return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeout);
	}

	/// <summary>
	/// A scrubber that writes each of the names a loaded configuration marks private as <c>private-repo_N</c>, whatever the case of
	/// the input. <see cref="Scrubbers.Values"/> matches case-sensitively, so a name in another case is first written as the config
	/// spells it, with the word boundaries <see cref="Scrubbers.Values"/> uses.
	/// </summary>
	internal static Func<string, string> Scrub(IEnumerable<string> privateRepositories)
	{
		var names = Real(privateRepositories);
		if (names.Length == 0)
			return text => text;
		var canonical = names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).ToDictionary(
			g => g.Key,
			g => g.First(),
			StringComparer.OrdinalIgnoreCase
		);
		var alternatives = string.Join('|', canonical.Keys.OrderByDescending(n => n.Length).Select(Regex.Escape));
		var anyCase = new Regex(
			$"(?<!\\w)(?:{alternatives})(?!\\w)",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
			RegexTimeouts.MatchTimeout
		);
		var values = Scrubbers.Values(canonical.Values, Placeholder);
		return text => values(anyCase.Replace(text, m => canonical[m.Value]));
	}

	/// <summary>
	/// The scrubber, the guard and the volatile-value scrubbers as options for one snapshot call, for a test class to use as
	/// <c>.MatchSnapshot(options: PrivateRepositoryScrubbing.Options(names))</c>.
	/// </summary>
	internal static SnapshotOptions Options(IEnumerable<string> privateRepositories) =>
		new SnapshotOptions().Scrub(Scrub(privateRepositories)).Scrub(VolatileValues).Forbid(Forbidden(privateRepositories));

	/// <summary>A 40 digit commit SHA becomes <c>{sha}</c>; an ISO date, with or without a time, becomes <c>{date}</c>.</summary>
	internal static string VolatileValues(string text) => IsoDate().Replace(Sha().Replace(text, "{sha}"), "{date}");

	[GeneratedRegex("^private-repo-[0-9]+$", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex PlaceholderName();

	[GeneratedRegex("(?<![0-9a-fA-F])[0-9a-f]{40}(?![0-9a-fA-F])", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex Sha();

	[GeneratedRegex(@"\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})?)?", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex IsoDate();
}
