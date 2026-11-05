// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Mcp.Remote.Tests;

/// <summary>
/// The HTTP snapshot format writes a <c>text/event-stream</c> body as quoted text, so the JSON-RPC message of a
/// <c>data:</c> line would be one unreadable line. This scrubber pretty-prints it, which keeps a diff readable.
/// It runs on the rendered responses text, where every body line starts with <c>&gt; </c>.
/// </summary>
internal static partial class SseScrubbers
{
	private static readonly JsonSerializerOptions Pretty = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	[GeneratedRegex(@"^> data: (?<json>.*)$", RegexOptions.Multiline)]
	private static partial Regex DataLine();

	public static string PrettyPrintDataLines(string text) =>
		DataLine().Replace(text, match =>
		{
			JsonNode? node;
			try
			{
				node = JsonNode.Parse(match.Groups["json"].Value);
			}
			catch (JsonException)
			{
				return match.Value;
			}

			var lines = (node?.ToJsonString(Pretty) ?? "null").Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
			var result = new StringBuilder();
			for (var i = 0; i < lines.Length; i++)
			{
				if (i > 0)
					_ = result.Append('\n');
				_ = result.Append(i == 0 ? "> data: " : "> ").Append(lines[i]);
			}
			return result.ToString();
		});
}
