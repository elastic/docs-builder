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
	/// Sanitizes a string for safe logging by removing all ASCII control characters (0x00-0x1F).
	/// This prevents log forging attacks where malicious input could inject fake log entries
	/// via newlines, tabs, escape sequences, or other control characters.
	/// Uses span-based operations for optimal performance.
	/// </summary>
	/// <param name="input">The input string to sanitize.</param>
	/// <returns>The sanitized string with control characters removed, or empty string if input is null.</returns>
	public static string Sanitize(string? input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		var span = input.AsSpan();

		// Fast path: check if any control characters exist (common case has none) - zero allocations
		var hasControlChars = false;
		foreach (var c in span)
		{
			if (IsControlChar(c))
			{
				hasControlChars = true;
				break;
			}
		}

		if (!hasControlChars)
			return input;

		// Slow path: count chars to keep, then create string with exact size
		var keepCount = 0;
		foreach (var c in span)
		{
			if (!IsControlChar(c))
				keepCount++;
		}

		return string.Create(keepCount, input, static (dest, src) =>
		{
			var destIndex = 0;
			foreach (var c in src)
			{
				if (!IsControlChar(c))
					dest[destIndex++] = c;
			}
		});
	}

	/// <summary>
	/// Checks if a character is an ASCII control character (0x00-0x1F).
	/// </summary>
	private static bool IsControlChar(char c) => c <= '\x1F';
}
