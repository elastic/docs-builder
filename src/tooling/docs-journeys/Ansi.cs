// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Journeys.Cli;

/// <summary>Colour and icons for a terminal. <c>NO_COLOR</c> drops the colour and keeps the layout.</summary>
internal static class Ansi
{
	private static readonly bool Colour = Environment.GetEnvironmentVariable("NO_COLOR") is not { Length: > 0 };

	/// <summary>True when the terminal is likely to draw <c>✓</c>, <c>✗</c> and box drawing: a UTF-8 locale, or a terminal that sets its own marker.</summary>
	public static readonly bool Unicode = DetectUnicode();

	public static string Ok => Unicode ? "✓" : "+";
	public static string Fail => Unicode ? "✗" : "x";
	public static string Skip => Unicode ? "⊘" : "-";
	public static string Pointer => Unicode ? "▶" : ">";
	public static string Bar => Unicode ? "│" : "|";
	public static string Dot => Unicode ? "·" : "-";

	public static string Bold(string text) => Wrap("1", text);
	public static string Dim(string text) => Wrap("2", text);
	public static string Red(string text) => Wrap("31", text);
	public static string BoldRed(string text) => Wrap("1;31", text);
	public static string Green(string text) => Wrap("32", text);
	public static string Yellow(string text) => Wrap("33", text);
	public static string Blue(string text) => Wrap("94", text);
	public static string Cyan(string text) => Wrap("36", text);

	private static string Wrap(string code, string text) => Colour ? $"\e[{code}m{text}\e[0m" : text;

	private static bool DetectUnicode()
	{
		if (Environment.GetEnvironmentVariable("TERM") is "dumb" or "linux")
			return false;
		if (
			Environment.GetEnvironmentVariable("WT_SESSION") is { Length: > 0 }
			|| Environment.GetEnvironmentVariable("TERM_PROGRAM") is { Length: > 0 }
		)
			return true;
		foreach (var name in new[] { "LC_ALL", "LC_CTYPE", "LANG" })
			if (Environment.GetEnvironmentVariable(name) is { Length: > 0 } value)
				return value.Contains("UTF-8", StringComparison.OrdinalIgnoreCase)
					|| value.Contains("UTF8", StringComparison.OrdinalIgnoreCase);
		return false;
	}
}
