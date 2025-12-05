// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Helpers;

internal static class StringExtensions
{
	/// <summary>
	/// Ensures the string is trimmed of leading and trailing whitespace.
	/// Only allocates a new string if trimming actually changes the content.
	/// </summary>
	/// <param name="value">The string to trim</param>
	/// <returns>The trimmed string, reusing the original if no changes needed</returns>
	public static string EnsureTrimmed(this string value)
	{
		var span = value.AsSpan();
		var trimmed = span.Trim();
		return trimmed.Length != value.Length ? trimmed.ToString() : value;
	}
}
