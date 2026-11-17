// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Testing.Linked;

/// <summary>
/// Scrubbers every snapshot in docs-builder's tests gets: the repository root and the temp folder are replaced by
/// <c>{repo}</c> and <c>{temp}</c>, and the rest of such a path is written with forward slashes, so a verified file
/// reads the same on every machine and operating system. Compiled into each test project.
/// </summary>
internal static partial class SnapshotScrubbing
{
#pragma warning disable CA2255 // compiled into each test project, which is application code
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Register()
	{
		// the more specific root first: the repository may sit under the temp folder
		if (FindSolutionRoot() is { } root)
			SnapshotDefaults.AddScrubber(Under(root, "{repo}"));
		// a test folder in the temp directory is usually named after a fresh Guid; that name is not part of what is verified
		SnapshotDefaults.AddScrubber(Under(Path.GetTempPath(), "{temp}", RandomName()));
		SnapshotDefaults.AddScrubber(ContentHash);
		SnapshotDefaults.AddScrubber(text => PrivateRepositoryNames.Value?.Invoke(text) ?? text);
	}

	// per test (async flow), so one test's names do not rename another test's output and the numbering does not depend on test order
	private static readonly AsyncLocal<Func<string, string>?> PrivateRepositoryNames = new();

	/// <summary>
	/// For the rest of the calling test, writes the given repository names as <c>private-repo-1</c>, <c>private-repo-2</c>, ... in
	/// every snapshot, numbered in name order. A name matches as a whole token (<c>widget</c> is not <c>widget-hosted</c>),
	/// in any case, also inside a url or a path. Pass the names a loaded configuration marks <c>private</c>, so a
	/// snapshot never records one, however the input named it. Names already written as a placeholder are left alone.
	/// </summary>
	internal static void AddPrivateRepositories(IEnumerable<string> names)
	{
		var distinct = names
			.Where(n => !string.IsNullOrWhiteSpace(n) && !PlaceholderName().IsMatch(n))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		if (distinct.Length == 0)
			return;
		var placeholders = distinct.Select((n, i) => (n, placeholder: $"private-repo-{i + 1}")).ToDictionary(
			e => e.n,
			e => e.placeholder,
			StringComparer.OrdinalIgnoreCase
		);
		var alternatives = string.Join('|', distinct.OrderByDescending(n => n.Length).Select(Regex.Escape));
		var pattern = new Regex($"(?<![\\w.\\-])(?:{alternatives})(?![\\w\\-])", RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeout);
		PrivateRepositoryNames.Value = text => pattern.Replace(text, m => placeholders[m.Value]);
	}

	[GeneratedRegex("^private-repo-[0-9]+$", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex PlaceholderName();

	/// <summary>
	/// Replaces <paramref name="root"/> with <paramref name="token"/> and writes the path that follows it with
	/// forward slashes. The path ends at whitespace, a quote, an angle bracket or a colon, so <c>file.md:3</c> keeps its line.
	/// </summary>
	internal static Func<string, string> Under(string root, string token, Regex? randomName = null)
	{
		root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		// DiagnosticsSnapshot writes a path with forward slashes and without its drive letter, so the root has to match that way too
		var forward = root.Replace('\\', '/');
		var withoutDrive = DriveLetter().Replace(forward, string.Empty);
		var roots = new[] { root, forward, withoutDrive }.Where(r => r.Length > 1).Distinct().Select(Regex.Escape);
		var pattern = new Regex(
			$"(?:{string.Join('|', roots)})(?<rest>[^\\s\"'<>:*?|]*)",
			RegexOptions.Compiled,
			RegexTimeouts.MatchTimeout
		);
		return text => pattern.Replace(text, m =>
		{
			var rest = m.Groups["rest"].Value.Replace('\\', '/');
			return token + (randomName is null ? rest : randomName.Replace(rest, "{id}"));
		});
	}

	[GeneratedRegex("^[A-Za-z]:", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex DriveLetter();

	/// <summary>A run of 32 hex digits: a <c>Guid.ToString("N")</c> used as a folder name.</summary>
	[GeneratedRegex("[0-9a-fA-F]{32}", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex RandomName();

	/// <summary>
	/// The cache-busting <c>?v=</c> hash of a static asset URL. It is a hash of the file's bytes, and a Windows checkout can change an
	/// SVG's line endings, so the footer logo read <c>?v=19bc…</c> on Linux and macOS and another value on Windows.
	/// </summary>
	internal static string ContentHash(string text) => AssetHash().Replace(text, "{hash}");

	[GeneratedRegex("(?<=\\?v=)[0-9a-f]{16}", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex AssetHash();

	private static string? FindSolutionRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docs-builder.slnx")))
			dir = dir.Parent;
		return dir?.FullName;
	}
}
