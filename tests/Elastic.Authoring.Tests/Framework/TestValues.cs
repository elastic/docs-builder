// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Markdown;
using Elastic.Markdown.IO;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Writes diagnostics to xUnit's test output. Resolves the output helper per write
/// so it remains safe even when the collector outlives the test that triggered the generator build
/// (the collector is shared across all tests in a class via the per-type cache).
/// </summary>
public sealed class TestDiagnosticsOutput : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		var line = diagnostic.Line ?? 0;
		var prefix = diagnostic.Severity switch
		{
			Severity.Error => "Error",
			Severity.Warning => "Warn ",
			_ => "Hint "
		};
		TestContext.Current?.Output.WriteLine($"{prefix}: {diagnostic.Message} ({diagnostic.File}:{line})");
	}
}

/// <summary>
/// Diagnostics collector for the authoring test harness. Unlike the Elastic.Markdown.Tests version,
/// this overrides <c>HandleItem</c> (not <c>Write</c>) and does NOT override StartAsync/StopAsync,
/// so the real channel-based lifecycle runs — which is required when using
/// <see cref="DocumentationGenerator.GenerateAll"/>.
/// </summary>
public sealed class TestDiagnosticsCollector() : DiagnosticsCollector([new TestDiagnosticsOutput()])
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

	protected override void HandleItem(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
}

public sealed class TestLogger : ILogger
{
	public bool IsEnabled(LogLevel logLevel) => true;
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter
	) => TestContext.Current?.Output.WriteLine(formatter(state, exception));
}

public sealed class TestLoggerFactory : ILoggerFactory
{
	public void AddProvider(ILoggerProvider provider) { }
	public ILogger CreateLogger(string categoryName) => new TestLogger();
	public void Dispose() { }
}

public sealed record ConversionResult
{
	public required MarkdownFile File { get; init; }
	public required MarkdownDocument Document { get; init; }
	public required string Html { get; init; }
}

public sealed class TestConversionCollector : IConversionCollector
{
	private readonly ConcurrentDictionary<string, ConversionResult> _results = new();

	public IReadOnlyDictionary<string, ConversionResult> Results => _results;

	public void Collect(MarkdownFile file, MarkdownDocument document, string html) =>
		_results.TryAdd(file.RelativePath, new ConversionResult { File = file, Document = document, Html = html });
}

public sealed record MarkdownResult
{
	public required MarkdownFile File { get; init; }
	public required MarkdownDocument MinimalParse { get; init; }
	public required MarkdownDocument Document { get; init; }
	public required string Html { get; init; }
	public required GeneratorContext Context { get; init; }
}

/// <summary>Snapshot of everything produced by a single <see cref="DocumentationGenerator.GenerateAll"/> run.</summary>
public sealed record GeneratorResults
{
	public required GeneratorContext Context { get; init; }
	public required IReadOnlyList<MarkdownResult> MarkdownResults { get; init; }
}

/// <summary>
/// The live infrastructure objects produced by <see cref="AuthoringGenerator.GenerateAsync"/>.
/// Held by the per-class <see cref="Scenario"/> and shared across all test methods in the class.
/// </summary>
public sealed class GeneratorContext
{
	public required TestDiagnosticsCollector Collector { get; init; }
	public required TestConversionCollector ConversionCollector { get; init; }
	public required DocumentationSet Set { get; init; }
	public required DocumentationGenerator Generator { get; init; }
	public required IDocumentationFileSystem ReadFileSystem { get; init; }
	public required IFileSystem WriteFileSystem { get; init; }
}
