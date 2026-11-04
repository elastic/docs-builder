// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Changelog.Utilities;

/// <summary>
/// Defense-in-depth sanitizer for values written to GitHub Actions step
/// outputs (<c>GITHUB_OUTPUT</c>) when those values originate from
/// attacker-controlled PR metadata (title, body, labels, etc.).
/// </summary>
/// <remarks>
/// <para>
/// <c>Actions.Core</c> already protects the output framing layer with
/// random delimiters, so this sanitizer is *belt-and-braces*: it strips
/// control characters that could appear in error messages or be parsed
/// downstream, and caps length per field so a malicious PR cannot blow
/// past runner env-var budgets or downstream string buffers.
/// </para>
/// <para>
/// See <see href="https://github.com/elastic/docs-eng-team/issues/491">elastic/docs-eng-team#491</see>
/// for the security review that motivates these caps.
/// </para>
/// </remarks>
public static class OutputSanitizer
{
	/// <summary>Cap for PR title outputs (matches the action-layer sanitizer).</summary>
	public const int TitleMaxLength = 200;

	/// <summary>Cap for extracted release-note descriptions.</summary>
	public const int DescriptionMaxLength = 4 * 1024;

	/// <summary>Cap for the changelog type identifier.</summary>
	public const int TypeMaxLength = 128;

	/// <summary>Cap for comma-separated product or label lists.</summary>
	public const int LabelsMaxLength = 4 * 1024;

	/// <summary>Cap for rendered Markdown label tables.</summary>
	public const int LabelTableMaxLength = 8 * 1024;

	/// <summary>Cap for filesystem paths derived from repo layout.</summary>
	public const int PathMaxLength = 1024;

	/// <summary>
	/// Strips null bytes and C0/DEL control characters (preserving
	/// <c>\n</c> and <c>\t</c>) and truncates the result to
	/// <paramref name="maxLength"/> characters. Returns
	/// <see cref="string.Empty"/> for <see langword="null"/> or empty input.
	/// </summary>
	public static string SanitizeForOutput(string? value, int maxLength)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		if (maxLength <= 0)
			return string.Empty;

		var capacity = Math.Min(value.Length, maxLength);
		var builder = new StringBuilder(capacity);

		foreach (var c in value)
		{
			if (builder.Length >= maxLength)
				break;

			// Strip C0 control characters (U+0000..U+001F) except \n and \t,
			// plus DEL (U+007F). These are the characters most likely to
			// confuse downstream shell, YAML, Markdown, and log parsers.
			if (c is (< (char)0x20 and not '\n' and not '\t') or (char)0x7F)
				continue;

			_ = builder.Append(c);
		}

		return builder.ToString();
	}
}
