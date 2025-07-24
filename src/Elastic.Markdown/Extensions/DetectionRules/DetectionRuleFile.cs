// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Plugins.DetectionRules;
using Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Extensions.DetectionRules;

public record DetectionRuleOverviewFile : MarkdownFile
{
	public DetectionRuleOverviewFile(IFileInfo sourceFile, IDirectoryInfo rootPath, MarkdownParser parser, BuildContext build, DocumentationSet set)
		: base(sourceFile, rootPath, parser, build, set)
	{
	}

	public RuleReference[] Rules { get; set; } = [];

	private Dictionary<string, DetectionRuleFile> Files { get; } = [];

	public void AddDetectionRuleFile(DetectionRuleFile df, RuleReference ruleReference) => Files[ruleReference.RelativePath] = df;

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
		Title = "Prebuilt detection rules reference";
		var markdown = GetMarkdown();
		var document = MarkdownParser.MinimalParseString(markdown, SourceFile, null);
		return Task.FromResult(document);
	}

	protected override Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx)
	{
		var markdown = GetMarkdown();
		var document = MarkdownParser.ParseString(markdown, SourceFile, null);
		return Task.FromResult(document);
	}

	private string GetMarkdown()
	{
		var groupedRules =
			Rules
				.GroupBy(r => r.Rule.Domain ?? "Unspecified")
				.OrderBy(g => g.Key)
				.ToArray();
		// language=markdown
		var markdown =
"""
# Prebuilt detection rules reference

:::{important}
All prebuilt machine learning rules are tagged with `ML`, and their rule type is `machine_learning`. Before using prebuilt machine learning rules, ensure you have all of the appropriate requirements.
:::

""";

		foreach (var group in groupedRules)
		{
			markdown +=
$"""

## {group.Key}

""";
			foreach (var r in group.OrderBy(r => r.Rule.Name))
			{
				var url = Files[r.RelativePath].Url;
				markdown +=
$"""
[{r.Rule.Name}](!{url}) <br>
""";

			}

		}


		return markdown;
	}

}

public record DetectionRuleFile : MarkdownFile
{
	public DetectionRule? Rule { get; set; }

	public override string LinkReferenceRelativePath { get; }

	public IFileInfo RuleSourceMarkdownPath { get; }

	public DetectionRuleFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		DocumentationSet set
	) : base(sourceFile, rootPath, parser, build, set)
	{
		RuleSourceMarkdownPath = SourcePath(sourceFile, build);
		LinkReferenceRelativePath = Path.GetRelativePath(build.DocumentationSourceDirectory.FullName, RuleSourceMarkdownPath.FullName);
	}

	private static IFileInfo SourcePath(IFileInfo rulePath, BuildContext build)
	{
		var relative = Path.GetRelativePath(build.DocumentationCheckoutDirectory!.FullName, rulePath.FullName);
		var newPath = Path.Combine(build.DocumentationSourceDirectory.FullName, relative);
		var md = Path.ChangeExtension(newPath, ".md");
		return rulePath.FileSystem.FileInfo.New(md);
	}

	public static IFileInfo OutputPath(IFileInfo rulePath, BuildContext build)
	{
		var relative = Path.GetRelativePath(build.DocumentationOutputDirectory.FullName, rulePath.FullName);
		if (relative.StartsWith("../"))
			relative = relative[3..];
		var newPath = Path.Combine(build.DocumentationOutputDirectory.FullName, relative);
		return rulePath.FileSystem.FileInfo.New(newPath);
	}

	protected override string RelativePathUrl => RelativePath.AsSpan().TrimStart("../").ToString();

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
		Title = Rule?.Name;
		var markdown = GetMarkdown();
		var document = MarkdownParser.MinimalParseString(markdown, RuleSourceMarkdownPath, null);
		return Task.FromResult(document);
	}

	protected override Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx)
	{
		var markdown = GetMarkdown();
		var document = MarkdownParser.ParseString(markdown, RuleSourceMarkdownPath, null);
		return Task.FromResult(document);
	}

	private string GetMarkdown()
	{
		if (Rule is null)
			return $"# {Title}";
		// language=markdown
		var markdown =
$"""
# {Rule.Name}

{Rule.Description}

**Rule type**: {Rule.Type}<br>
**Rule indices**: {RenderArray(Rule.Indices)}

**Rule Severity**: {Rule.Severity}<br>
**Risk Score**: {Rule.RiskScore}<br>
**Runs every**: {Rule.RunsEvery}<br>
**Searches indices from**: `{Rule.IndicesFromDateMath}`<br>
**Maximum alerts per execution**: {Rule.MaximumAlertsPerExecution}<br>
**References**: {RenderArray((Rule.References ?? []).Select(r => $"[{r}]({r})").ToArray())}

**Tags**: {RenderArray(Rule.Tags)}

**Version**: {Rule.Version}<br>
**Rule authors**: {RenderArray(Rule.Authors)}

**Rule license**: {Rule.License}<br>
""";
		// language=markdown
		if (!string.IsNullOrWhiteSpace(Rule.Setup))
		{
			markdown +=
$"""

 {Rule.Setup}
""";
		}

		// language=markdown
		if (!string.IsNullOrWhiteSpace(Rule.Note))
		{
			markdown +=
$"""

 ## Investigation guide

 {Rule.Note}
""";
		}
		// language=markdown
		if (!string.IsNullOrWhiteSpace(Rule.Query))
		{
			markdown +=
$"""

 ## Rule Query

 ```{Rule.Language ?? Rule.Type}
 {Rule.Query}
 ```
 """;
		}

		foreach (var threat in Rule.Threats)
		{
			// language=markdown
			markdown +=
$"""

**Framework:** {threat.Framework}

* Tactic:
  * Name: {threat.Tactic.Name}
  * Id: {threat.Tactic.Id}
  * Reference URL: [{threat.Tactic.Reference}]({threat.Tactic.Reference})

""";
			foreach (var technique in threat.Techniques)
			{
				// language=markdown
				markdown += TechniqueMarkdown(technique, "Technique");
				foreach (var subTechnique in technique.SubTechniques)
					markdown += TechniqueMarkdown(subTechnique, "Sub Technique");
			}
		}
		return markdown;
	}

	private static string TechniqueMarkdown(DetectionRuleSubTechnique technique, string header) =>
$"""

* {header}:
  * Name: {technique.Name}
  * Id: {technique.Id}
  * Reference URL: [{technique.Reference}]({technique.Reference})

""";

	private static string RenderArray(string[]? values)
	{
		if (values == null || values.Length == 0)
			return string.Empty;
		return "\n - " + string.Join("\n - ", values) + "\n";
	}
}
