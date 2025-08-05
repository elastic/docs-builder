// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using static Crayon.Output;

namespace Elastic.Documentation.ServiceDefaults.Logging;

public class CondensedConsoleFormatter() : ConsoleFormatter("condensed")
{
	public override void Write<TState>(
		in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter
	)
	{
		var now = DateTime.UtcNow;
		var message = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception);

		var logLevel = GetLogLevel(logEntry.LogLevel);
		var categoryName = logEntry.Category;

		var nowString =
			Environment.UserInteractive
			? ""
			: now.ToString("[yyyy-MM-ddTHH:mm:ss.fffZ] ", System.Globalization.CultureInfo.InvariantCulture);

		textWriter.WriteLine($"{nowString}{logLevel}::{ShortCategoryName(categoryName)}:: {message}");
	}

	private static string GetLogLevel(LogLevel logLevel) => logLevel switch
	{
		LogLevel.Trace => "trace",
		LogLevel.Debug => "debug",
		LogLevel.Information => Blue().Bold().Text("info "),
		LogLevel.Warning => Yellow().Bold().Text("warn "),
		LogLevel.Error => Red().Bold().Text("error"),
		LogLevel.Critical => Red().Bold().Text("fail "),
		LogLevel.None => "    ",
		_ => "???"
	};

	private static string ShortCategoryName(string category)
	{
		var tokens = category.Split('.', StringSplitOptions.RemoveEmptyEntries);
		var s = string.Join(".", tokens.Take(tokens.Length - 1).Select(t => t.ToLowerInvariant().First()).ToArray());
		if (s.Length > 0)
			s += ".";

		var maxLength = 22 - s.Length;
		var last = tokens.Last();
		var start = Math.Max(0, last.Length - maxLength);
		s += last[start..];
		return Dim().Text(s.PadRight(22));
	}
}
