// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.CodeBlocks;

namespace Elastic.Markdown.Tests;

public class CodeBlockLanguageTests
{
	[Test]
	[Arguments("clojure")]
	[Arguments("clj")]
	[Arguments("gherkin")]
	[Arguments("Gherkin")]
	[Arguments("feature")]
	[Arguments("diff")]
	[Arguments("patch")]
	[Arguments("makefile")]
	[Arguments("Makefile")]
	[Arguments("graphql")]
	[Arguments("gql")]
	[Arguments("batch")]
	[Arguments("fish")]
	public void Languages_KnownLanguage_IsSupported(string language) => CodeBlock.Languages.Should().Contain(language);

	[Test]
	[Arguments("hcl")]
	[Arguments("terraform")]
	[Arguments("cue")]
	public void Languages_ThirdPartyGrammar_IsNotSupported(string language) => CodeBlock.Languages.Should().NotContain(language);

	[Test]
	[Arguments(BuildType.Isolated, Severity.Warning)]
	[Arguments(BuildType.Assembler, Severity.Warning)]
	[Arguments(BuildType.Codex, Severity.Hint)]
	public async Task Build_UnknownLanguage_SeverityDependsOnBuildType(BuildType buildType, Severity expected)
	{
		var diagnostics = await Build(buildType, "not-a-valid-language");

		var unknown = diagnostics.Where(d => d.Message.Contains("Unknown language: not-a-valid-language")).ToList();
		unknown.Should().ContainSingle().Which.Severity.Should().Be(expected);
	}

	[Test]
	[Arguments(BuildType.Isolated)]
	[Arguments(BuildType.Codex)]
	public async Task Build_KnownLanguage_EmitsNoUnknownLanguageDiagnostic(BuildType buildType)
	{
		var diagnostics = await Build(buildType, "fish");

		diagnostics.Should().NotContain(d => d.Message.Contains("Unknown language"));
	}

	private static async Task<IReadOnlyList<Diagnostic>> Build(BuildType buildType, string language)
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				["docs/docset.yml"] = new(
					"""
					project: test
					toc:
					- file: index.md
					"""
				),
				["docs/index.md"] = new($"# Title\n\n```{language}\necho hi\n```\n")
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var collector = new TestDiagnosticsCollector();
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			BuildType = buildType
		};
		var set = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver());
		var generator = new DocumentationGenerator(set, new TestLoggerFactory());

		await generator.GenerateAll(TestContext.Current!.Execution.CancellationToken);
		await collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		return collector.Diagnostics.ToList();
	}
}
