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
}
