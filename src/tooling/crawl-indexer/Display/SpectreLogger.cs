// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace CrawlIndexer.Display;

/// <summary>
/// Global state for live display mode to control logging behavior.
/// </summary>
public static class LiveDisplayState
{
	/// <summary>
	/// When true, suppresses Info-level logs to prevent interference with live progress display.
	/// Only set to true in interactive mode (not CI, not redirected console).
	/// </summary>
	public static bool SuppressInfoLogs { get; set; }
}

/// <summary>
/// Logger that writes to Spectre.Console's AnsiConsole for proper terminal handling.
/// </summary>
public sealed class SpectreLogger(string categoryName) : ILogger
{
	// Categories that should only log at Warning level (too noisy at Info)
	private static readonly HashSet<string> NoisyCategories =
	[
		"Polly",
		"System.Net.Http.HttpClient",
		"Microsoft.Extensions.Http"
	];

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel)
	{
		// During live display in interactive mode, only show warnings and above
		if (LiveDisplayState.SuppressInfoLogs && logLevel < LogLevel.Warning)
			return false;

		// Noisy HTTP/Polly categories only show warnings and above
		if (IsNoisyCategory())
			return logLevel >= LogLevel.Warning;

		return logLevel >= LogLevel.Information;
	}

	private bool IsNoisyCategory() =>
		NoisyCategories.Any(c => categoryName.Contains(c, StringComparison.OrdinalIgnoreCase));

	[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
		Justification = "Exception formatting is best-effort for error display")]
	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter
	)
	{
		if (!IsEnabled(logLevel))
			return;

		var message = formatter(state, exception);
		var shortCategory = ShortCategoryName(categoryName);

		var (levelColor, levelText) = logLevel switch
		{
			LogLevel.Trace => ("grey", "trace"),
			LogLevel.Debug => ("grey", "debug"),
			LogLevel.Information => ("blue bold", "info "),
			LogLevel.Warning => ("yellow bold", "warn "),
			LogLevel.Error => ("red bold", "error"),
			LogLevel.Critical => ("red bold", "fail "),
			_ => ("white", "??? ")
		};

		AnsiConsole.MarkupLine($"[{levelColor}]{levelText}[/]::[dim]{Markup.Escape(shortCategory)}[/]:: {Markup.Escape(message)}");

		if (exception is not null)
			AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
	}

	private static string ShortCategoryName(string category)
	{
		var tokens = category.Split('.', StringSplitOptions.RemoveEmptyEntries);
		var s = string.Join(".", tokens.Take(tokens.Length - 1).Select(t => char.ToLowerInvariant(t[0])).ToArray());
		if (s.Length > 0)
			s += ".";

		var maxLength = 22 - s.Length;
		var last = tokens[^1];
		var start = Math.Max(0, last.Length - maxLength);
		s += last[start..];
		return s.PadRight(22);
	}

}

/// <summary>
/// Logger provider that creates SpectreLoggers.
/// </summary>
public sealed class SpectreLoggerProvider : ILoggerProvider
{
	public ILogger CreateLogger(string categoryName) => new SpectreLogger(categoryName);

	public void Dispose()
	{
		// Nothing to dispose
	}
}

/// <summary>
/// Extensions for configuring Spectre logging.
/// </summary>
public static class SpectreLoggingExtensions
{
	private static readonly string[] NoisyPrefixes =
	[
		"Polly",
		"System.Net.Http",
		"Microsoft.Extensions.Http"
	];

	/// <summary>
	/// Adds filters to suppress noisy HTTP/Polly logs.
	/// Call this AFTER all service defaults are configured.
	/// </summary>
	public static ILoggingBuilder AddNoisyLogFilters(this ILoggingBuilder builder)
	{
		// Add filter for noisy categories - these will apply to all providers
		foreach (var prefix in NoisyPrefixes)
			_ = builder.AddFilter(prefix, LogLevel.Warning);

		return builder;
	}
}
