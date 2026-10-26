// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Chrome.DiagnosticsPanel;

public static class DiagnosticsPanelExamples
{
	/// <summary>Made-up messages of each severity, for showing the console with something in it.</summary>
	public static IReadOnlyList<DiagnosticSample> Samples { get; } =
	[
		new("error", "docs/lorem/index.md:12", "Link target not found: ./ipsum/dolor.md"),
		new("error", "docs/lorem/sit.md:4", "Unknown directive {amet}."),
		new("warning", "docs/lorem/api.md:7", "Heading level skipped: expected h3 after h2."),
		new("warning", "docs/lorem/index.md:30", "The image consectetur.png has no alt text."),
		new("hint", "docs/lorem/index.md", "Rebuilt 3 files in 212ms."),
		new("hint", "docs/lorem/adipiscing.md:1", "This page is not part of the table of contents.")
	];

	public static ComponentSection Panels { get; } = new(
		"Diagnostics panel",
		"The developer console on `docs-builder serve`. It polls the build state and lists diagnostics. Without a running server it stays at Connecting, so it is hidden in the style guide until a server answers.",
		"diagnostics",
		[
			ComponentExample.Of("Panel", _DiagnosticsPanel.Create, new DiagnosticsPanelModel()),
			ComponentExample.Of("With messages", _DiagnosticsPanel.Create, new DiagnosticsPanelModel { Samples = Samples })
		]
	);
}
