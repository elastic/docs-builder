// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Elastic.SiteSearch.Cli.Logging;

public class CondensedConsoleFormatter() : ConsoleFormatter("condensed")
{
	private const string Reset = "\x1b[0m";
	private const string Bold = "\x1b[1m";
	private const string Dim = "\x1b[2m";
	private const string Red = "\x1b[31m";
	private const string Yellow = "\x1b[33m";
	private const string Blue = "\x1b[34m";

	public override void Write<TState>(
		in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter
	)
	{
		var message = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception);
		var logLevel = GetLogLevel(logEntry.LogLevel);
		var category = ShortCategoryName(logEntry.Category);

		var timestamp = Environment.UserInteractive
			? ""
			: DateTime.UtcNow.ToString("[yyyy-MM-ddTHH:mm:ss.fffZ] ", System.Globalization.CultureInfo.InvariantCulture);

		textWriter.WriteLine($"{timestamp}{logLevel}::{category}:: {message}");

		if (logEntry.Exception is { } exception)
			textWriter.WriteLine(exception.ToString());
	}

	private static string GetLogLevel(LogLevel logLevel) => logLevel switch
	{
		LogLevel.Trace => "trace",
		LogLevel.Debug => "debug",
		LogLevel.Information => $"{Blue}{Bold}info {Reset}",
		LogLevel.Warning => $"{Yellow}{Bold}warn {Reset}",
		LogLevel.Error => $"{Red}{Bold}error{Reset}",
		LogLevel.Critical => $"{Red}{Bold}fail {Reset}",
		LogLevel.None => "     ",
		_ => "???"
	};

	private static string ShortCategoryName(string category)
	{
		var tokens = category.Split('.', StringSplitOptions.RemoveEmptyEntries);
		var prefix = string.Join(".", tokens.Take(tokens.Length - 1).Select(t => char.ToLowerInvariant(t[0])));
		if (prefix.Length > 0)
			prefix += ".";

		var maxLength = 22 - prefix.Length;
		var last = tokens.Last();
		var start = Math.Max(0, last.Length - maxLength);
		var shortened = prefix + last[start..];
		return $"{Dim}{shortened,-22}{Reset}";
	}
}
