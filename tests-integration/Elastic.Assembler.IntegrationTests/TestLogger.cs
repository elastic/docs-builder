// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Assembler.IntegrationTests;

public class TestLogger(ITestOutputHelper? output) : ILogger
{
	private sealed class NullScope : IDisposable
	{
		public void Dispose() { }
	}

	public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Trace;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
		output?.WriteLine(formatter(state, exception));
}

public class TestLoggerFactory(ITestOutputHelper? output) : ILoggerFactory
{
	public void Dispose() => GC.SuppressFinalize(this);

	public void AddProvider(ILoggerProvider provider) { }

	public ILogger CreateLogger(string categoryName) => new TestLogger(output);
}
public class TestDiagnosticsOutput(ITestOutputHelper output) : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Error)
			output.WriteLine($"Error: {diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
		else
			output.WriteLine($"Warn : {diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
	}
}

public class TestDiagnosticsCollector(ITestOutputHelper output)
	: DiagnosticsCollector([new TestDiagnosticsOutput(output)])
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

	protected override void HandleItem(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
}
