// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Elastic.Markdown.Myst.Directives.Settings;

internal static class SettingsMarkdownNormalizer
{
	public static string Normalize(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return markdown;

		var result = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
		if (result.Contains("[source,", StringComparison.Ordinal))
			result = NormalizeAsciiDocSourceBlocks(result);
		if (result.Contains('{'))
			result = NormalizeKibanaMarkupArtifacts(result);
		if (result.Contains("](/", StringComparison.Ordinal)
			|| result.Contains("](docs-content://", StringComparison.Ordinal)
			|| result.Contains("(elasticsearch://", StringComparison.Ordinal)
			|| result.Contains("(ecs://", StringComparison.Ordinal))
			result = RewriteReferenceLinksForDocset(result);

		return result;
	}

	private static string NormalizeKibanaMarkupArtifacts(string markdown)
	{
		var s = markdown.Replace("{applies_to}", "Applies to", StringComparison.Ordinal);
		return NormalizeAppliesToBacktickArtifacts(s);
	}

	/// <summary>Kibana-exported blurbs sometimes emit `` `key: value` `` pairs that break markdown code spans.</summary>
	private static string NormalizeAppliesToBacktickArtifacts(string markdown)
	{
		const string prefix = "Applies to`";
		var idx = 0;
		var output = new StringBuilder(markdown.Length);
		while (idx < markdown.Length)
		{
			var found = markdown.IndexOf(prefix, idx, StringComparison.OrdinalIgnoreCase);
			if (found < 0)
			{
				_ = output.Append(markdown.AsSpan(idx));
				break;
			}

			_ = output.Append(markdown.AsSpan(idx, found - idx));
			var valueStart = found + prefix.Length;
			var close = markdown.IndexOf('`', valueStart);
			if (close < 0)
			{
				_ = output.Append(markdown.AsSpan(found));
				break;
			}

			_ = output.Append("Applies to (");
			_ = output.Append(markdown.AsSpan(valueStart, close - valueStart));
			_ = output.Append(')');
			idx = close + 1;
		}

		return output.ToString();
	}

	/// <summary>
	/// Kibana-generated YAML often uses repo-root paths and cross-repo schemes. For docsets that only ship settings fixtures
	/// (for example docs-builder), rewrite to resolvable targets.
	/// </summary>
	private static string RewriteReferenceLinksForDocset(string markdown)
	{
		var s = RewriteDocsContentLinks(markdown);
		s = RewriteParenLinksWithPrefix(
			s,
			"](/reference/",
			"https://www.elastic.co/docs/reference/");

		s = RewriteSchemeLinks(
			s,
			"(elasticsearch://reference/",
			"(https://www.elastic.co/docs/reference/");

		return RewriteSchemeLinks(
			s,
			"(ecs://reference/",
			"(https://www.elastic.co/docs/reference/");
	}

	/// <summary>
	/// Turns docs-content scheme links into public docs URLs so settings YAML can be built inside repositories
	/// that only bundle fixtures (no full cross-link index coverage).
	/// </summary>
	[SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "Parsed from markdown URLs only.")]
	private static string RewriteDocsContentLinks(string markdown)
	{
		const string prefix = "](docs-content://";
		var idx = 0;
		while (true)
		{
			var found = markdown.IndexOf(prefix, idx, StringComparison.Ordinal);
			if (found < 0)
				return markdown;

			var innerStart = found + prefix.Length;
			var endParen = markdown.IndexOf(')', innerStart);
			if (endParen < 0)
				return markdown;

			var inner = markdown[innerStart..endParen];
			if (!Uri.TryCreate("docs-content://" + inner, UriKind.Absolute, out var uri))
			{
				idx = innerStart;
				continue;
			}

			var path = (uri.Host + uri.AbsolutePath.TrimStart('/')).TrimEnd('/');
			if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
				path = path[..^3];

			var fragment = uri.Fragment.Length > 0 ? uri.Fragment : string.Empty;
			var replacement = $"](https://www.elastic.co/docs/{path}{fragment}";
			markdown = string.Concat(markdown.AsSpan(0, found), replacement, markdown.AsSpan(endParen));
			idx = found + replacement.Length;
		}
	}

	private static string RewriteParenLinksWithPrefix(string markdown, string literalPrefix, string absoluteBase)
	{
		var idx = 0;
		while (true)
		{
			var found = markdown.IndexOf(literalPrefix, idx, StringComparison.Ordinal);
			if (found < 0)
				return markdown;

			var pathStart = found + literalPrefix.Length;
			var endParen = markdown.IndexOf(')', pathStart);
			if (endParen < 0)
				return markdown;

			var inner = markdown[pathStart..endParen];
			var hashIdx = inner.IndexOf('#');
			var path = hashIdx >= 0 ? inner[..hashIdx] : inner;
			var fragment = hashIdx >= 0 ? inner[hashIdx..] : string.Empty;

			if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
				path = path[..^3];

			var replacement = $"]({absoluteBase}{path}{fragment}";
			markdown = string.Concat(markdown.AsSpan(0, found), replacement, markdown.AsSpan(endParen));
			idx = found + replacement.Length;
		}
	}

	private static string RewriteSchemeLinks(string markdown, string schemePrefix, string httpsPrefix)
	{
		var idx = 0;
		while (idx < markdown.Length)
		{
			var open = markdown.IndexOf(schemePrefix, idx, StringComparison.Ordinal);
			if (open < 0)
				return markdown;

			var innerStart = open + schemePrefix.Length;
			var closeParen = markdown.IndexOf(')', innerStart);
			if (closeParen < 0)
				return markdown;

			var inner = markdown[innerStart..closeParen];
			var hashIdx = inner.IndexOf('#');
			var path = hashIdx >= 0 ? inner[..hashIdx] : inner;
			var fragment = hashIdx >= 0 ? inner[hashIdx..] : string.Empty;

			if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
				path = path[..^3];

			var replacement = $"{httpsPrefix}{path}{fragment}";
			markdown = string.Concat(markdown.AsSpan(0, open), replacement, markdown.AsSpan(closeParen));
			idx = open + replacement.Length;
		}

		return markdown;
	}

	private static string NormalizeAsciiDocSourceBlocks(string markdown)
	{
		var lines = markdown.Split('\n');
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
