// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core;

/// <summary>
/// Utility for sanitizing user input before logging to prevent log forging attacks.
/// </summary>
public static class LogSanitizer
{
	/// <summary>
	/// Sanitizes a string for safe logging by removing dangerous control and separator characters.
	/// This prevents log forging attacks where malicious input could inject fake log entries
	/// via newlines, tabs, escape sequences, or other control characters.
	/// Uses span-based operations for optimal performance.
	/// </summary>
	/// <remarks>
	/// Removes:
	/// - ASCII control characters (0x00-0x1F)
	/// - DEL character (0x7F)
	/// - Unicode line separator (U+2028)
	/// - Unicode paragraph separator (U+2029)
	/// </remarks>
	/// <param name="input">The input string to sanitize.</param>
	/// <returns>The sanitized string with control characters removed, or empty string if input is null.</returns>
	public static string Sanitize(string? input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		var span = input.AsSpan();

		// Fast path: check if any dangerous characters exist (common case has none) - zero allocations
		var hasDangerousChars = false;
		foreach (var c in span)
		{
			if (IsDangerousChar(c))
			{
				hasDangerousChars = true;
				break;
			}
		}

		if (!hasDangerousChars)
			return input;

		// Slow path: count chars to keep, then create string with exact size
		var keepCount = 0;
		foreach (var c in span)
		{
			if (!IsDangerousChar(c))
				keepCount++;
		}

		return string.Create(keepCount, input, static (dest, src) =>
		{
			var destIndex = 0;
			foreach (var c in src)
			{
				if (!IsDangerousChar(c))
					dest[destIndex++] = c;
			}
		});
	}

	/// <summary>
	/// Checks if a character is dangerous for logging (could enable log forging).
	/// </summary>
	private static bool IsDangerousChar(char c) =>
		c <= '\x1F' ||          // ASCII control characters (0x00-0x1F)
		c == '\x7F' ||          // DEL character
		c == '\u2028' ||        // Unicode line separator
		c == '\u2029';          // Unicode paragraph separator
}
