// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>Records what is written, unfiltered and in order.</summary>
internal sealed class BufferedCollector() : DiagnosticsCollector([])
{
	public List<Diagnostic> Written { get; } = [];

	public override void Write(Diagnostic diagnostic) => Written.Add(diagnostic);
}
