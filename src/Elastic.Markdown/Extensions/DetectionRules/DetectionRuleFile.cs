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
		var document = MarkdownParser.MinimalParseStringAsync(markdown, RuleSourceMarkdownPath, null);
		return Task.FromResult(document);
	}

	protected override Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx)
	{
		var markdown = GetMarkdown();
		var document = MarkdownParser.ParseStringAsync(markdown, RuleSourceMarkdownPath, null);
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

			 **Rule type**: {Rule.Type}

			 **Rule indices**: {RenderArray(Rule.Indices)}

			 **Rule Severity**: {Rule.Severity}

			 **Risk Score**: {Rule.RiskScore}

			 **Runs every**: {Rule.RunsEvery}

			 **Searches indices from**: `{Rule.IndicesFromDateMath}`

			 **Maximum alerts per execution**: {Rule.MaximumAlertsPerExecution}

			 **References**: {RenderArray((Rule.References ?? []).Select(r => $"[{r}]({r})").ToArray())}

			 **Tags**: {RenderArray(Rule.Tags)}

			 **Version**: {Rule.Version}

			 **Rule authors**: {RenderArray(Rule.Authors)}

			 **Rule license**: {Rule.License}
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
