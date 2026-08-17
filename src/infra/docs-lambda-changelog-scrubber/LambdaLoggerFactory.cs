// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Elastic.Documentation.Lambda.ChangelogScrubber;

/// <summary>
/// Routes <see cref="ILogger"/> calls from the shared Elastic.Changelog processing code to the
/// Lambda's own logger, so the extracted processor stays free of Lambda dependencies.
/// </summary>
internal sealed class LambdaLoggerFactory(ILambdaLogger lambdaLogger) : ILoggerFactory
{
	public ILogger CreateLogger(string categoryName) => new LambdaLoggerAdapter(categoryName, lambdaLogger);

	public void AddProvider(ILoggerProvider provider)
	{
		// Providers are meaningless here; everything goes to the Lambda logger.
	}

	public void Dispose()
	{
	}

	private sealed class LambdaLoggerAdapter(string categoryName, ILambdaLogger lambdaLogger) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
				return;

			var message = $"{categoryName}: {formatter(state, exception)}";
			if (exception is not null)
				message = $"{message} | {exception}";

			lambdaLogger.Log(MapLevel(logLevel), message);
		}

		private static string MapLevel(LogLevel level) => level switch
		{
			LogLevel.Trace => "TRACE",
			LogLevel.Debug => "DEBUG",
			LogLevel.Information => "INFO",
			LogLevel.Warning => "WARN",
			LogLevel.Error => "ERROR",
			LogLevel.Critical => "CRITICAL",
			_ => "INFO"
		};
	}
}
