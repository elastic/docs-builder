// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;

namespace Elastic.Documentation.Journeys.Tests.Targets;

public static partial class BodyScrubbers
{
	/// <summary>
	/// Keeps the first line of every quoted text body (<c>&gt; </c> lines in a <c>.verified.responses</c> file) and replaces
	/// the rest with an ellipsis, so a snapshot of a documentation page shows its title and not its content.
	/// </summary>
	public static string FirstQuotedLine(string transcript)
	{
		var result = new StringBuilder();
		var inQuote = false;
		foreach (var line in transcript.ReplaceLineEndings("\n").Split('\n'))
		{
			var quoted = line.StartsWith("> ", StringComparison.Ordinal) || line == ">";
			if (quoted && inQuote)
				continue;
			if (quoted)
				_ = result.Append(line).Append('\n').Append("> …").Append('\n');
			else
				_ = result.Append(line).Append('\n');
			inQuote = quoted;
		}
		return result.ToString().TrimEnd('\n') + "\n";
	}

	/// <summary>
	/// Replaces the text of every <c>description</c> and <c>instructions</c> string in a pretty-printed JSON body (plain or quoted with <c>&gt; </c>) with an ellipsis, so
	/// a snapshot of a tool list keeps the names, schemas and hints but not wording that the tool authors edit often.
	/// </summary>
	public static string WithoutProse(string transcript) => ProseLine().Replace(transcript, "${prefix}\"…\"${suffix}");

	[GeneratedRegex("""(?<prefix>^[> ]* *"(?:description|instructions)": )".*"(?<suffix>,?)$""", RegexOptions.Multiline)]
	private static partial Regex ProseLine();
}
