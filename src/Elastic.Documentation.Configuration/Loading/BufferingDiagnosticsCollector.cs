// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Holds diagnostics in emission order without counting or filtering them, so a loader can return them in a
/// <see cref="Result{T}"/> and the caller's collector sees exactly what a direct write would have produced.
/// </summary>
internal sealed class BufferingDiagnosticsCollector() : DiagnosticsCollector([])
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

	public override void Write(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

	public override DiagnosticsCollector StartAsync(Cancel ctx) => this;

	public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
}
