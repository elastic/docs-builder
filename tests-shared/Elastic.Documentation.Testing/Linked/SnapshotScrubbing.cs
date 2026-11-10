// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Testing.Linked;

/// <summary>
/// Scrubbers every snapshot in docs-builder's tests gets: the repository root and the temp folder are replaced by
/// <c>{repo}</c> and <c>{temp}</c>, and the rest of such a path is written with forward slashes, so a verified file
/// reads the same on every machine and operating system. Compiled into each test project.
/// </summary>
internal static class SnapshotScrubbing
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
		SnapshotDefaults.AddScrubber(Under(Path.GetTempPath(), "{temp}", RandomName));
		SnapshotDefaults.AddScrubber(ContentHash);
	}

	/// <summary>
	/// Replaces <paramref name="root"/> with <paramref name="token"/> and writes the path that follows it with
	/// forward slashes. The path ends at whitespace, a quote, an angle bracket or a colon, so <c>file.md:3</c> keeps its line.
	/// </summary>
	internal static Func<string, string> Under(string root, string token, Regex? randomName = null)
	{
		root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		// DiagnosticsSnapshot writes a path with forward slashes and without its drive letter, so the root has to match that way too
		var forward = root.Replace('\\', '/');
		var withoutDrive = DriveLetter.Replace(forward, string.Empty);
		var roots = new[] { root, forward, withoutDrive }.Where(r => r.Length > 1).Distinct().Select(Regex.Escape);
		var pattern = new Regex($"(?:{string.Join('|', roots)})(?<rest>[^\\s\"'<>:*?|]*)", RegexOptions.Compiled);
		return text => pattern.Replace(text, m =>
		{
			var rest = m.Groups["rest"].Value.Replace('\\', '/');
			return token + (randomName is null ? rest : randomName.Replace(rest, "{id}"));
		});
	}

	private static readonly Regex DriveLetter = new("^[A-Za-z]:", RegexOptions.Compiled);

	/// <summary>A run of 32 hex digits: a <c>Guid.ToString("N")</c> used as a folder name.</summary>
	private static readonly Regex RandomName = new("[0-9a-fA-F]{32}", RegexOptions.Compiled);

	/// <summary>
	/// The cache-busting <c>?v=</c> hash of a static asset URL. It is a hash of the file's bytes, and a Windows checkout can change an
	/// SVG's line endings, so the footer logo read <c>?v=19bc…</c> on Linux and macOS and another value on Windows.
	/// </summary>
	internal static string ContentHash(string text) => AssetHash.Replace(text, "{hash}");

	private static readonly Regex AssetHash = new("(?<=\\?v=)[0-9a-f]{16}", RegexOptions.Compiled);

	private static string? FindSolutionRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docs-builder.slnx")))
			dir = dir.Parent;
		return dir?.FullName;
	}
}
