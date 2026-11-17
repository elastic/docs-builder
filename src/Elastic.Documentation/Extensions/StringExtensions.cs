// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Elastic.Documentation.Extensions;

public static class StringExtensions
{
	/// <summary>
	/// The URL slug of <paramref name="text"/>: trimmed, lower-cased, diacritics removed, whitespace runs turned into one <c>-</c>,
	/// everything but <c>a-z 0-9 - _ .</c> dropped and repeated dashes collapsed. Null gives an empty string.
	/// These rules produce the anchors, heading ids and changelog links, so changing them breaks existing links.
	/// </summary>
	public static string Slugify(this string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);
		var pendingSeparator = false;
		foreach (var original in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(original) == UnicodeCategory.NonSpacingMark)
				continue;
			if (char.IsWhiteSpace(original))
			{
				pendingSeparator = true;
				continue;
			}
			var c = char.ToLowerInvariant(original);
			if (!IsSlugChar(c))
				continue;
			if (pendingSeparator)
			{
				AppendSlugChar(sb, '-');
				pendingSeparator = false;
			}
			AppendSlugChar(sb, c);
		}
		if (pendingSeparator)
			AppendSlugChar(sb, '-');
		return sb.ToString();
	}

	private static bool IsSlugChar(char c) => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_' or '.';

	private static void AppendSlugChar(StringBuilder sb, char c)
	{
		if (c == '-' && sb.Length > 0 && sb[^1] == '-')
			return;
		_ = sb.Append(c);
	}

	/// <summary>Null for an empty string, so a Razor attribute bound to it is left out instead of written as <c>name=""</c>.</summary>
	public static string? NullIfEmpty(this string? value) => string.IsNullOrEmpty(value) ? null : value;

	/// <summary>The values joined with <paramref name="separator"/>, or null when the joined text is empty (see <see cref="NullIfEmpty"/>).</summary>
	public static string? JoinOrNull(this IEnumerable<string> values, char separator) => string.Join(separator, values).NullIfEmpty();

	public static string OptionalWindowsReplace(this string relativePath)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			relativePath = relativePath.Replace('\\', '/');
		return relativePath;
	}

	/// <summary>Converts a kebab-case/snake_case identifier to a title-cased string ("get-script" becomes "Get Script").</summary>
	public static string TitleCaseFromSlug(this string slug) =>
		string.Join(
			' ',
			slug
				.Replace('-', ' ')
				.Replace('_', ' ')
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant())
		);

	/// <summary>
	/// Uppercases the first letter of a title. Titles that start with a code-like token (containing digits,
	/// underscores, dots, backticks, or inner capitals such as <c>kibana.alert</c> or <c>inputsForVersions</c>)
	/// are left unchanged so identifiers keep their exact casing.
	/// Hyphens and apostrophes are allowed in prose words (for example <c>don't</c>).
	/// </summary>
	public static string CapitalizeFirstLetter(this string title)
	{
		if (string.IsNullOrEmpty(title) || !char.IsLower(title[0]))
			return title;

		var firstWordEnd = title.IndexOfAny([' ', '\t']);
		var firstWord = firstWordEnd < 0 ? title : title[..firstWordEnd];
		if (firstWord.Any(c => !char.IsLetter(c) && c is not ('-' or '\'' or '\u2019')) || firstWord.Skip(1).Any(char.IsUpper))
			return title;

		return char.ToUpperInvariant(title[0]) + title[1..];
	}
}
