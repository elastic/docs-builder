// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Tracks CommonMark fenced code blocks for line-oriented passes over API description Markdown.</summary>
internal static partial class CodeFences
{
	/// <summary>True for a fence line and for every line inside an open fence; updates <paramref name="openFence"/> as it goes.</summary>
	public static bool InCode(string line, ref Fence? openFence)
	{
		// A fence inside a blockquote cannot outlive it: the first unquoted line ends both.
		if (openFence is { Quoted: true } && !QuoteLine().IsMatch(line))
			openFence = null;

		var match = FenceLine().Match(line);
		if (!match.Success)
			return openFence is not null;

		var fence = new Fence(match.Groups["fence"].Value, match.Groups["quote"].Success);
		if (openFence is null)
			openFence = fence;
		else if (fence.Closes(openFence.Value, match.Groups["rest"].Value))
			openFence = null;
		return true;
	}

	public static bool IsFenceLine(string line) => FenceLine().IsMatch(line);

	// A fence may sit inside a blockquote, so a `> info` line within that quoted code is still literal.
	[GeneratedRegex(@"^(?<quote>\s{0,3}>\s?)?\s{0,3}(?<fence>`{3,}|~{3,})(?<rest>.*)$")]
	private static partial Regex FenceLine();

	[GeneratedRegex(@"^\s{0,3}>")]
	private static partial Regex QuoteLine();

	public readonly record struct Fence(string Delimiter, bool Quoted)
	{
		// CommonMark: the closing fence uses the same character, is at least as long and has nothing after it.
		// It must also sit in the same quote context, so a quoted fence never closes an unquoted block.
		public bool Closes(Fence open, string rest) =>
			Quoted == open.Quoted
				&& Delimiter[0] == open.Delimiter[0]
				&& Delimiter.Length >= open.Delimiter.Length
				&& string.IsNullOrWhiteSpace(rest);
	}
}
