// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.DiagnosticsPanel;

public static class DiagnosticsPanelExamples
{
	public static ComponentSection Panels { get; } = new(
		"Diagnostics panel",
		"The developer console on `docs-builder serve`. It polls the build state and lists diagnostics. Without a running server it stays at Connecting, so it is hidden in the style guide until a server answers.",
		"diagnostics",
		[ComponentExample.Of("Panel", _DiagnosticsPanel.Create, new DiagnosticsPanelModel())]
	);
}
