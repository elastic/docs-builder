// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Markdown.Tests;

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
		// Accept drops an exact duplicate of a rule's diagnostic, as the production collector does, so a finding two layers report is seen once.
		if (Accept(diagnostic))
			_diagnostics.Add(diagnostic);
	}

	/// <inheritdoc />
	public override DiagnosticsCollector StartAsync(Cancel ctx) => this;

	/// <inheritdoc />
	public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
}
