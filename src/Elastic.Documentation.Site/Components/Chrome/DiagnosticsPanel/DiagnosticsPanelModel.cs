// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Chrome.DiagnosticsPanel;

/// <param name="Severity">error, warning or hint.</param>
public sealed record DiagnosticSample(string Severity, string Location, string Message);

/// <summary>The developer console of docs-builder serve. With samples it is drawn open and still, to show what it looks like with messages.</summary>
public sealed record DiagnosticsPanelModel
{
	public IReadOnlyList<DiagnosticSample> Samples { get; init; } = [];

	public bool Static => Samples.Count > 0;

	public int Count(string severity) => Samples.Count(s => s.Severity == severity);

	public string Worst => Count("error") > 0 ? "error" : Count("warning") > 0 ? "warning" : "hint";
}
