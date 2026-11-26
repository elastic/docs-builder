// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Tests.DetectionRules;

public class DetectionRuleExportTests
{
	// DetectionRule reads the rule from disk, so the docset is written to a temporary folder. The output
	// folder is outside the checkout, so a write next to it is refused.
	[Test]
	public async Task GenerateAll_RuleOutsideDocsFolder_WritesMarkdownNextToHtml()
	{
		var ctx = TestContext.Current!.Execution.CancellationToken;
		var root = Directory.CreateTempSubdirectory("docs-builder-rules-");
		try
		{
			var repo = root.CreateSubdirectory("repo");
			WriteFile(
				repo,
				"docs/docset.yml",
				//language=yaml
				"""
				project: test
				extensions:
				  - detection-rules
				toc:
				  - file: index.md
				  - file: rules.md
				    detection_rules:
				      - ../rules
				"""
			);
			WriteFile(repo, "docs/index.md", "# Home");
			WriteFile(repo, "docs/rules.md", "# Rules");
			WriteFile(
				repo,
				"rules/linux/kernel_ring_buffer.toml",
				//language=toml
				"""
				[metadata]
				creation_date = "2024/08/01"
				maturity = "production"

				[rule]
				author = ["Elastic"]
				description = "Test rule"
				name = "Kernel Ring Buffer"
				rule_id = "abc-123"
				risk_score = 47
				severity = "medium"
				type = "query"
				license = "Elastic License v2"
				language = "kuery"
				query = "process.name : evil.exe"
				"""
			);
			_ = repo.CreateSubdirectory(".git");

			var fs = new FileSystem();
			var logger = new TestLoggerFactory();
			await using var collector = new DiagnosticsCollector([]).StartAsync(ctx);
			var documentationFileSystem = DocumentationFileSystem.Resolve(
				fs.DirectoryInfo.New(Path.Join(repo.FullName, "docs")),
				new DocumentationScopeOptions { Inner = fs, Output = Path.Join(root.FullName, "out", "html") }
			);
			var context = new BuildContext(collector, documentationFileSystem, TestHelpers.CreateConfigurationContext(fs))
			{
				SkipStaticAssetExtraction = true
			};
			var set = new DocumentationSet(context, logger, new TestCrossLinkResolver());
			var generator = new DocumentationGenerator(set, logger, markdownExporters: [new LlmMarkdownExporter()]);

			await generator.GenerateAll(ctx);
			await collector.StopAsync(ctx);

			var rulePages = Directory
				.EnumerateFiles(root.FullName, "*", SearchOption.AllDirectories)
				.Where(f => f.Contains("kernel_ring_buffer", StringComparison.Ordinal) && !f.EndsWith(".toml", StringComparison.Ordinal))
				.Select(f => Path.GetRelativePath(set.OutputDirectory.FullName, f).Replace('\\', '/'));
			rulePages.Should().BeEquivalentTo("rules/linux/kernel_ring_buffer/index.html", "rules/linux/kernel_ring_buffer.md");
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	private static void WriteFile(DirectoryInfo root, string relativePath, string content)
	{
		var path = Path.Join(root.FullName, relativePath);
		_ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}
}
