// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Testing;

public static partial class DiagnosticsSnapshot
{
	/// <summary>
	/// One line per diagnostic, <c>error|warning|hint file:line message</c>, in the order they were written. Empty when
	/// there are none, so a clean run reads as <c>MatchInlineSnapshot("")</c>.
	/// <para>
	/// Paths read the same on every operating system: backslashes become forward slashes and a leading drive letter is
	/// dropped, so <c>C:\docs\toc.yml</c>, <c>C:/docs/toc.yml</c> and <c>/docs/toc.yml</c> are the same text. A message that
	/// really contains a backslash reads with a slash, on Linux too.
	/// </para>
	/// </summary>
	/// <param name="diagnostics">What a test's collector holds.</param>
	/// <param name="firstLineOnly">
	/// Keep only the first line of each message. For diagnostics that carry an exception, whose stack trace (frames and
	/// source line numbers) is no part of what a test verifies, and whose type and text can differ per platform.
	/// </param>
	public static string ToSnapshotText(this IEnumerable<Diagnostic> diagnostics, bool firstLineOnly = false)
	{
		var sb = new StringBuilder();
		foreach (var d in diagnostics)
		{
			var severity = d.Severity switch
			{
				Severity.Error => "error",
				Severity.Warning => "warning",
				_ => "hint"
			};
			var file = Normalise(d.File);
			var position = d.Line is { } line ? $"{file}:{line}" : file;
			var message = d.Message ?? string.Empty;
			if (firstLineOnly)
				message = message.Split('\n')[0].TrimEnd('\r');
			_ = sb.Append(severity).Append(' ').Append(position).Append(' ').Append(Normalise(message)).Append('\n');
		}
		return sb.ToString();
	}

	private static string Normalise(string text) => DriveLetter().Replace(text.Replace('\\', '/'), string.Empty);

	// "C:/docs/..." at the start of a path; not the "s:" of "https://", which a letter or digit precedes
	[GeneratedRegex("(?<![A-Za-z0-9])[A-Za-z]:(?=/)")]
	private static partial Regex DriveLetter();
}
