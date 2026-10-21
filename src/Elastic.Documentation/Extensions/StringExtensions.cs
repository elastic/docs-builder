// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.InteropServices;

namespace Elastic.Documentation.Extensions;

public static class StringExtensions
{
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
