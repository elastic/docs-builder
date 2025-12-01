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
	/// Sanitizes a string for safe logging by removing newline and carriage return characters.
	/// This prevents log forging attacks where malicious input could inject fake log entries.
	/// </summary>
	/// <param name="input">The input string to sanitize.</param>
	/// <returns>The sanitized string with newlines removed, or empty string if input is null.</returns>
	public static string Sanitize(string? input) =>
		input?.Replace("\r", "").Replace("\n", "") ?? string.Empty;
}
