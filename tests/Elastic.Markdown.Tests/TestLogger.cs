// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Tests;

public class TestLogger(ITestOutputHelper output) : ILogger
{
	private class NullScope : IDisposable
	{
		public void Dispose() { }
	}

	public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Trace;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
		output.WriteLine(formatter(state, exception));
}

public class TestLoggerProvider(ITestOutputHelper output) : ILoggerProvider
{
	public void Dispose()
	{
	}

	public ILogger CreateLogger(string categoryName) => new TestLogger(output);
}

public class TestLoggerFactory(ITestOutputHelper output) : ILoggerFactory
{
	public void Dispose()
	{
	}

	public void AddProvider(ILoggerProvider provider) { }

	public ILogger CreateLogger(string categoryName) => new TestLogger(output);
}
