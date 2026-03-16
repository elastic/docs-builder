// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Markdown.Myst.Directives.Settings;

internal static class SettingsMarkdownNormalizer
{
	public static string Normalize(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown) || !markdown.Contains("[source,", StringComparison.Ordinal))
			return markdown;

		var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
		var output = new StringBuilder(markdown.Length);

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (!TryParseSourceLanguage(line, out var language) || i + 1 >= lines.Length)
			{
				AppendLine(output, line, i < lines.Length - 1);
				continue;
			}

			var delimiter = lines[i + 1].Trim();
			if (delimiter is not "--" and not "----")
			{
				AppendLine(output, line, i < lines.Length - 1);
				continue;
			}

			_ = output.Append("```");
			_ = output.Append(language);
			_ = output.Append('\n');

			i += 2;
			while (i < lines.Length && lines[i].Trim() != delimiter)
			{
				AppendLine(output, lines[i], true);
				i++;
			}

			_ = output.Append("```");
			if (i < lines.Length - 1)
				_ = output.Append('\n');
		}

		return output.ToString();
	}

	private static bool TryParseSourceLanguage(string line, out string language)
	{
		language = string.Empty;
		var trimmed = line.Trim();
		if (!trimmed.StartsWith("[source,", StringComparison.Ordinal) || !trimmed.EndsWith(']'))
			return false;

		var start = "[source,".Length;
		var length = trimmed.Length - start - 1;
		if (length <= 0)
			return false;

		language = trimmed.Substring(start, length).Trim();
		return !string.IsNullOrWhiteSpace(language);
	}

	private static void AppendLine(StringBuilder output, string line, bool withNewLine)
	{
		_ = output.Append(line);
		if (withNewLine)
			_ = output.Append('\n');
	}
}
