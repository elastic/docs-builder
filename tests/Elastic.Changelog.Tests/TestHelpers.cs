// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Tests;

public class TestDiagnosticsOutput : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Error)
			TestContext.Current?.Output.WriteLine($"Error: {diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
		else
			TestContext.Current?.Output.WriteLine($"Warn : {diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
	}
}

public class TestDiagnosticsCollector() : DiagnosticsCollector([new TestDiagnosticsOutput()])
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

	/// <inheritdoc />
	public override void Write(Diagnostic diagnostic)
	{
		IncrementSeverityCount(diagnostic);
		_diagnostics.Add(diagnostic);
	}

	/// <inheritdoc />
	public override DiagnosticsCollector StartAsync(Cancel ctx) => this;

	/// <inheritdoc />
	public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
}

public class TestLogger : ILogger
{
	private sealed class NullScope : IDisposable
	{
		public void Dispose() { }
	}

	public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Trace;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter
	) => TestContext.Current?.Output.WriteLine(formatter(state, exception));
}

public class TestLoggerProvider : ILoggerProvider
{
	public void Dispose() => GC.SuppressFinalize(this);

	public ILogger CreateLogger(string categoryName) => new TestLogger();
}

public class TestLoggerFactory : ILoggerFactory
{
	public void Dispose() => GC.SuppressFinalize(this);

	public void AddProvider(ILoggerProvider provider) { }

	public ILogger CreateLogger(string categoryName) => new TestLogger();
}
