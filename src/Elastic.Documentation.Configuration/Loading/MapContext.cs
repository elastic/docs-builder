// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// What a loader's map step reports to: the file being loaded and the findings it makes itself (warnings and hints a loader raises while shaping the record).
/// The loader creates one per load, so a finding cannot be lost, reordered or forgotten, and is kept when a later step throws.
/// </summary>
internal sealed class MapContext(string file)
{
	private readonly List<Diagnostic> _findings = [];

	public string File { get; } = file;

	public IReadOnlyList<Diagnostic> Findings => _findings;

	public void Warn(string message, int? line = null, int? column = null) => Add(Severity.Warning, message, line, column);

	public void Hint(string message, int? line = null, int? column = null) => Add(Severity.Hint, message, line, column);

	private void Add(Severity severity, string message, int? line, int? column) =>
		_findings.Add(new Diagnostic { Severity = severity, File = File, Message = message, Line = line, Column = column });
}
