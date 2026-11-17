// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Nullean.Snapshots;

namespace Elastic.Documentation.Testing.Linked;

/// <summary>
/// Keeps the names of private repositories out of a snapshot. The names are not listed anywhere: a test derives them from the
/// <c>private: true</c> repositories of the assembler config it loaded. A name is scrubbed to <c>private-repo_N</c> with
/// <see cref="Scrubbers.Values"/> and forbidden with <see cref="SnapshotOptions.Forbid"/>, so a verified file that names one fails
/// without the name being printed. A name that already is a <c>private-repo-N</c> placeholder is left alone.
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
	/// What a name must not look like in a snapshot. A name with a hyphen is distinctive enough to forbid on its own. A single word is an
	/// ordinary word in prose and paths, so only the forms a repository can take are forbidden: <c>name://</c> and <c>elastic/name</c>
	/// followed by a quote, a slash, a dot, a colon or whitespace.
	/// </summary>
	internal static string[] Forbidden(IEnumerable<string> privateRepositories) =>
		[
			.. Real(privateRepositories).SelectMany(
				n => n.Contains('-') ? [n] : new[] { $"{n}://", $"elastic/{n}/", $"elastic/{n}.git", $"elastic/{n}\"" }
			)
		];

	/// <summary>A scrubber that writes each of the names a loaded configuration marks private as <c>private-repo_N</c>.</summary>
	internal static Func<string, string> Scrub(IEnumerable<string> privateRepositories) =>
		Scrubbers.Values(Real(privateRepositories), Placeholder);

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
