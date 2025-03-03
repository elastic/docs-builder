// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Extensions.DetectionRules;

public record DetectionRuleFile : MarkdownFile
{
	public DetectionRule? Rule { get; set; }

	public DetectionRuleFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		DocumentationSet set
	) : base(sourceFile, rootPath, parser, build, set)
	{
	}

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
		var document = MarkdownParser.MinimalParseStringAsync(Rule?.Note ?? string.Empty, SourceFile, null);
		Title = Rule?.Name;
		return Task.FromResult(document);
	}

	protected override Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx)
	{
		if (Rule == null)
			return Task.FromResult(MarkdownParser.ParseStringAsync($"# {Title}", SourceFile, null));

		// language=markdown
		var markdown = $"""
# {Rule.Name}

**Rule type**: {Rule.Type}
**Rule indices**: {RenderArray(Rule.Indices)}
**Rule Severity**: {Rule.Severity}
**Risk Score**: {Rule.RiskScore}
**Runs every**: {Rule.RunsEvery}
**Searches indices from**: `{Rule.IndicesFromDateMath}`
**Maximum alerts per execution**: {Rule.MaximumAlertsPerExecution}
**References**: {RenderArray(Rule.References)}
**Tags**: {RenderArray(Rule.Tags)}
**Version**: {Rule.Version}
**Rule authors**: {RenderArray(Rule.Authors)}
**Rule license**: {Rule.License}

## Investigation guide

{Rule.Note}

## Rule Query

```{Rule.Language ?? Rule.Type}
{Rule.Query}
```
""";
		var document = MarkdownParser.ParseStringAsync(markdown, SourceFile, null);
		return Task.FromResult(document);
	}

	private static string RenderArray(string[]? values)
	{
		if (values == null || values.Length == 0)
			return string.Empty;
		return "\n - " + string.Join("\n - ", values) + "\n";
	}
}
