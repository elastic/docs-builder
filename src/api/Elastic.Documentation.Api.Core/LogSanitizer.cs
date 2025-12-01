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

		// Always sanitize: remove all dangerous/control/log-forging characters
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
		c is <= '\x1F' or          // ASCII control characters (0x00-0x1F)
		'\x7F' or          // DEL character
		'\u2028' or        // Unicode line separator
		'\u2029';          // Unicode paragraph separator
}
