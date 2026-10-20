// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>Records every written diagnostic so tests can assert on message, severity and position.</summary>
public class CapturingDiagnosticsCollector() : DiagnosticsCollector([])
{
	private readonly ConcurrentQueue<Diagnostic> _captured = new();

	public IReadOnlyCollection<Diagnostic> Captured => _captured;

	public override void Write(Diagnostic diagnostic)
	{
		_captured.Enqueue(diagnostic);
		base.Write(diagnostic);
	}
}
