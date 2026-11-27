// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// What a loader's map step reports to: the file being loaded and the findings it makes itself (warnings, hints and errors a loader raises while shaping the record).
/// The loader creates one per load, so a finding cannot be lost, reordered or forgotten, and is kept when a later step throws.
/// </summary>
// The collector is never started: it holds an unused channel and a cancellation source that owns no timer or handle, so there is nothing to dispose.
#pragma warning disable CA1001
internal sealed class MapContext(string file)
{
	private readonly List<Diagnostic> _findings = [];

	public string File { get; } = file;

	public IReadOnlyList<Diagnostic> Findings => _findings;

	/// <summary>
	/// An <see cref="IDiagnosticsCollector"/> over this context's findings, so a parser written against a collector can be reused unchanged.
	/// Every write is appended to <see cref="Findings"/> in order, with the diagnostic exactly as written (file, line, message, rule id are not rewritten),
	/// interleaved with <see cref="Warn"/>, <see cref="Hint"/> and <see cref="Error"/>. The collector counts severities from this context only, so a parser
	/// that asks it whether anything errored sees the errors of its own load and no others (it is never shared with a process-wide collector).
	/// Writing an error here is not fatal by itself: whether it discards the value depends on the loader's <see cref="Fatal"/> mode.
	/// </summary>
	public IDiagnosticsCollector Collector => field ??= new BufferingDiagnosticsCollector(_findings);

	public void Warn(string message, int? line = null, int? column = null) => Add(Severity.Warning, message, line, column);

	public void Hint(string message, int? line = null, int? column = null) => Add(Severity.Hint, message, line, column);

	/// <summary>
	/// An explicit error finding made while mapping. Under <see cref="Fatal.All"/> (the default) it makes the load <see cref="Unreadable{T}"/>;
	/// under <see cref="Fatal.CheckErrors(string)"/> and <see cref="Fatal.None(string)"/> the value is kept and the error is only reported.
	/// </summary>
	public void Error(string message, int? line = null, int? column = null) => Add(Severity.Error, message, line, column);

	private void Add(Severity severity, string message, int? line, int? column) =>
		Collector.Write(new Diagnostic { Severity = severity, File = File, Message = message, Line = line, Column = column });
}
