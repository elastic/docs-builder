// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Authoring.Tests.Applicability;

// Where `applies_to` shows up in a page: the inline role, the front matter, `{applies-switch}` and the `:applies_to:` option of
// admonitions and dropdowns. Each class records the diagnostics and the rendered markup so a change to how the value is
// read or shown has to be deliberate. The values are the ones the converter table in Elastic.Markdown.Tests covers.

public class InlineAppliesToRoleMatrix : MarkdownTest
{
	protected override string Markdown =>
		"""
		Plain {applies_to}`stack: ga`

		Versioned {applies_to}`stack: ga 9.0+`

		Two lifecycles {applies_to}`stack: ga 9.2+, beta 9.0-9.1`

		Deployment {applies_to}`{ ece: ga 3.0+, ess: beta }`

		Scalar deployment {applies_to}`deployment: ga`

		Serverless project {applies_to}`serverless: preview`

		Serverless mapping {applies_to}`serverless: { elasticsearch: ga, security: beta }`

		Product {applies_to}`product: deprecated 2.0`

		Specific product {applies_to}`ecctl: ga 1.0+`

		Removed {applies_to}`stack: removed 9.5`

		Unavailable {applies_to}`serverless: unavailable`

		Unknown lifecycle {applies_to}`stack: nonsense`

		Unknown key {applies_to}`stak: ga`

		Version on a versionless key {applies_to}`serverless: ga 9.0`

		All {applies_to}`all`
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"""
		error {repo}/docs/index.md:1 Unknown product lifecycle: 'nonsense'. Valid lifecycles are: ga, preview, tech-preview, experimental, beta, deprecated, removed.
		hint {repo}/docs/index.md:1 Unknown key 'stak' in applies_to. Did you mean 'stack'?
		error {repo}/docs/index.md:1 Can't specify a version for 'serverless' because this product is not versioned. Remove the version, or use 'stack:' for version-specific requirements.
		"""
		);

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class AppliesSwitchMatrix : MarkdownTest
{
	protected override string Markdown =>
		"""
		::::{applies-switch}

		:::{applies-item} stack: ga 9.0+
		Stack content
		:::

		:::{applies-item} stack: experimental 9.1+
		Experimental content
		:::

		:::{applies-item} serverless: ga
		Serverless content
		:::

		:::{applies-item} { ece: ga 4.0+, ess: ga }
		Several definitions
		:::

		:::{applies-item} stack: nonsense
		Invalid lifecycle
		:::

		:::{applies-item} unknown: ga
		Unknown key
		:::
		::::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() => await Docs.HasDiagnostics("");

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class AdmonitionAndDropdownAppliesToOption : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{note}
		:applies_to: stack: ga 9.0+
		A note.
		:::

		:::{warning}
		:applies_to: serverless: ga
		A warning.
		:::

		:::{tip}
		:applies_to: { stack: preview 9.1, serverless: ga }
		A tip with two definitions.
		:::

		:::{important}
		:applies_to: stack: ga, serverless: ga
		An important notice with a comma separated value.
		:::

		:::{note}
		:applies_to: stack: nonsense
		An invalid lifecycle.
		:::

		:::{dropdown} Versioned dropdown
		:applies_to: stack: ga 9.0+
		Dropdown content.
		:::

		:::{dropdown} Deployment dropdown
		:applies_to: { ece: ga, eck: beta }
		Dropdown content.
		:::

		:::{dropdown} Unknown key dropdown
		:applies_to: unknown: ga
		Dropdown content.
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() => await Docs.HasDiagnostics("");

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class FrontMatterAppliesToTable : GeneratorTest
{
	private static readonly string[] Cases =
	[
		"applies_to:",
		"applies_to: all",
		"applies_to: ''",
		"applies_to: nonsense",
		"applies_to:\n  stack: ga",
		"applies_to:\n  stack: ga 9.0+",
		"applies_to:\n  stack: ga 9.2+, beta 9.0-9.1",
		"applies_to:\n  stack: preview 9.0, ga 9.1, deprecated 9.3",
		"applies_to:\n  stack: nonsense",
		"applies_to:\n  stack: ga 9",
		"applies_to:\n  deployment: ga",
		"applies_to:\n  deployment:\n    ece: ga 3.0+\n    eck: beta\n    ess: preview\n    self: ga 9.0+",
		"applies_to:\n  deployment:\n    ech: ga",
		"applies_to:\n  deployment:\n    ess: ga 9.0",
		"applies_to:\n  serverless: ga",
		"applies_to:\n  serverless:\n    elasticsearch: ga\n    security: beta",
		"applies_to:\n  serverless: ga 9.0",
		"applies_to:\n  product: ga 1.0+",
		"applies_to:\n  product:\n    ecctl: ga 1.0+",
		"applies_to:\n  stack: ga 9.0+\n  serverless: ga\n  deployment:\n    self: ga",
		"applies_to:\n  unknown: ga",
		"applies_to:\n  :stack: ga",
		"applies_to:\n  - stack: ga 9.0+\n  - serverless: ga",
		"applies_to:\n  - 'stack: ga 9.0+'\n  - 'serverless: ga'",
		"applies_to:\n  - nonsense",
		"applies_to: [ga]",
		"applies_to:\n  stack: [ga]",
		"applies_to:\n  Stack: ga",
		"applies_to:\n  stack: GA 9.0+",
		"applies_to:\n  stack: dev"
	];

	protected override IReadOnlyCollection<TestFile> Files =>
		[
			TestFile.Index("# Index\n"),
			.. Cases.Select((yaml, i) => TestFile.Page($"cases/case-{i:00}.md", $"---\n{yaml}\n---\n# Case {i}\n"))
		];

	// the pages are built in parallel, so the diagnostics are sorted to read the same on every run
	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics()
	{
		var result = await Docs.Converts("cases/case-00.md").Inner;

		var lines = result.Context.Collector.Diagnostics.ToSnapshotText().Split('\n', StringSplitOptions.RemoveEmptyEntries);

		string.Join("\n", lines.Order(StringComparer.Ordinal)).Should().MatchSnapshot();
	}

	[Test, DisplayName("records what each front matter reads to")]
	public async Task RecordsWhatEachFrontMatterReadsTo()
	{
		var sb = new StringBuilder();
		for (var i = 0; i < Cases.Length; i++)
		{
			var result = await Docs.Converts($"cases/case-{i:00}.md").Inner;
			var appliesTo = result.File.YamlFrontMatter?.AppliesTo;
			_ = sb.Append(Cases[i].Replace("\n", " / ")).AppendLine();
			_ = sb.Append("    ").AppendLine(
				appliesTo is null
					? "null"
					: JsonNode.Parse(JsonSerializer.Serialize(appliesTo, SourceGenerationContext.Default.ApplicableTo))!.ToJsonString()
			);
		}

		sb.ToString().Should().MatchSnapshot();
	}
}
