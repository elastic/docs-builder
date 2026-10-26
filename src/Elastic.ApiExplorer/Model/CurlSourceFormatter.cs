// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// Rewrites single-line <c>curl</c> samples into a readable multi-line form
/// (method + URL on the first line, one flag per subsequent line).
/// </summary>
public static class CurlSourceFormatter
{
	private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

	public static string Format(string source)
	{
		var trimmed = source.Trim();
		if (trimmed.Length == 0)
			return source;

		// Already wrapped / multi-line — leave author formatting alone.
		if (trimmed.Contains('\n', StringComparison.Ordinal))
			return source;

		if (!trimmed.StartsWith("curl", StringComparison.OrdinalIgnoreCase))
			return source;

		var tokens = Tokenize(trimmed);
		if (tokens.Count == 0 || !tokens[0].Equals("curl", StringComparison.OrdinalIgnoreCase))
			return source;

		string? method = null;
		string? url = null;
		var flags = new List<(string Flag, string? Value)>();

		for (var i = 1; i < tokens.Count; i++)
		{
			var token = tokens[i];
			if (IsFlag(token))
			{
				var flag = token;
				string? value = null;
				if (i + 1 < tokens.Count && !IsFlag(tokens[i + 1]))
				{
					value = tokens[i + 1];
					i++;
				}

				if (IsMethodFlag(flag) && value is not null)
					method = StripQuotes(value);
				else if (IsUrlFlag(flag) && value is not null)
					url = value;
				else if (IsDataFlag(flag) && value is not null)
					flags.Add((flag, PrettyPrintDataArgument(value)));
				else
					flags.Add((flag, value));
			}
			else if (url is null && LooksLikeUrl(token))
				url = token;
			else
				flags.Add((token, null));
		}

		var sb = new StringBuilder();
		_ = sb.Append("curl");
		if (method is not null)
			_ = sb.Append(" -X ").Append(method);
		if (url is not null)
			_ = sb.Append(' ').Append(EnsureQuoted(url));

		for (var i = 0; i < flags.Count; i++)
		{
			var (flag, value) = flags[i];
			_ = sb.Append(" \\\n  ").Append(flag);
			if (value is not null)
			{
				if (value.Contains('\n', StringComparison.Ordinal))
				{
					// Multi-line -d JSON: put the payload on following indented lines
					_ = sb.Append(' ').Append(value);
				}
				else
					_ = sb.Append(' ').Append(value);
			}
		}

		return sb.ToString();
	}

	private static bool IsFlag(string token) => token.StartsWith('-') && token.Length > 1;

	private static bool IsMethodFlag(string flag) => flag is "-X" or "--request";

	private static bool IsUrlFlag(string flag) => flag is "--url";

	private static bool IsDataFlag(string flag) => flag is "-d" or "--data" or "--data-raw" or "--data-binary" or "--data-urlencode";

	private static bool LooksLikeUrl(string token)
	{
		var bare = StripQuotes(token);
		return bare.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| bare.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			|| bare.StartsWith('$') // "$ELASTICSEARCH_URL/..."

			|| bare.Contains("/_", StringComparison.Ordinal)
			|| bare.Contains("/.", StringComparison.Ordinal);
	}

	private static string StripQuotes(string token)
	{
		if (token.Length >= 2 && ((token[0] == '"' && token[^1] == '"') || (token[0] == '\'' && token[^1] == '\'')))
			return token[1..^1];
		return token;
	}

	private static string EnsureQuoted(string token)
	{
		if (token.Length >= 2 && ((token[0] == '"' && token[^1] == '"') || (token[0] == '\'' && token[^1] == '\'')))
			return token;
		return $"\"{token}\"";
	}

	private static string PrettyPrintDataArgument(string token)
	{
		var quote = token.Length >= 2 && (token[0] == '\'' || token[0] == '"') ? token[0] : '"';
		var inner = StripQuotes(token);
		try
		{
			var node = JsonNode.Parse(inner);
			if (node is null)
				return token;
			var pretty = node.ToJsonString(PrettyJson);
			// Keep the original quote style; indent continuation of the payload.
			return quote + pretty.Replace("\n", "\n  ", StringComparison.Ordinal) + quote;
		}
		catch (JsonException)
		{
			return token;
		}
	}

	private static List<string> Tokenize(string source)
	{
		var tokens = new List<string>();
		var i = 0;
		while (i < source.Length)
		{
			while (i < source.Length && char.IsWhiteSpace(source[i]))
				i++;
			if (i >= source.Length)
				break;

			if (source[i] is '"' or '\'')
			{
				var quote = source[i];
				var start = i;
				i++;
				while (i < source.Length)
				{
					if (source[i] == '\\' && i + 1 < source.Length)
					{
						i += 2;
						continue;
					}
					if (source[i] == quote)
					{
						i++;
						break;
					}
					i++;
				}
				tokens.Add(source[start..i]);
			}
			else
			{
				var start = i;
				while (i < source.Length && !char.IsWhiteSpace(source[i]))
					i++;
				tokens.Add(source[start..i]);
			}
		}

		return tokens;
	}
}
