// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Codex.Navigation;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public abstract class CodexNavigationTestBase(ITestOutputHelper output)
{
	protected TestDiagnosticsCollector Collector { get; } = new(output);

	protected ICodexDocumentationContext CreateContext() => new TestCodexDocumentationContext(Collector);

	protected static CodexConfiguration CreateCodexConfiguration(
		string sitePrefix,
		List<CodexDocumentationSetReference> docSets) =>
		new()
		{
			Title = "Test Codex",
			SitePrefix = sitePrefix,
			DocumentationSets = docSets
		};

	protected IReadOnlyDictionary<string, IDocumentationSetNavigation> CreateMockDocSetNavigations(
		IEnumerable<string> repoNames)
	{
		var result = new Dictionary<string, IDocumentationSetNavigation>();
		var fileSystem = new MockFileSystem();

		foreach (var repoName in repoNames)
		{
			var docSet = CreateMockDocumentationSet(fileSystem, repoName);
			var context = new TestDocumentationSetContext(
				fileSystem,
				fileSystem.DirectoryInfo.New($"/{repoName}/docs"),
				fileSystem.DirectoryInfo.New($"/{repoName}/output"),
				fileSystem.FileInfo.New($"/{repoName}/docs/docset.yml"),
				output,
				repoName);

			var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
				docSet, context, TestDocumentationFileFactory.Instance);

			result[repoName] = navigation;
		}

		return result;
	}

	private static DocumentationSetFile CreateMockDocumentationSet(MockFileSystem fileSystem, string repoName)
	{
		var docsPath = $"/{repoName}/docs";
		fileSystem.AddDirectory(docsPath);
		fileSystem.AddFile($"{docsPath}/index.md", new MockFileData($"# {repoName}"));

		// language=yaml
		var yaml = $"""
		            project: '{repoName}'
		            toc:
		              - file: index.md
		            """;

		return DocumentationSetFile.LoadAndResolve(
			new DiagnosticsCollector([]),
			yaml,
			fileSystem.DirectoryInfo.New(docsPath));
	}
}

internal sealed class TestCodexDocumentationContext(IDiagnosticsCollector collector) : ICodexDocumentationContext
{
	private readonly MockFileSystem _fileSystem = new();

	public IFileInfo ConfigurationPath => _fileSystem.FileInfo.New("/codex.yml");
	public IDiagnosticsCollector Collector => collector;
	public IFileSystem ReadFileSystem => _fileSystem;
	public IFileSystem WriteFileSystem => _fileSystem;
	public IDirectoryInfo OutputDirectory => _fileSystem.DirectoryInfo.New("/output");
	public bool AssemblerBuild => false;

	public void EmitError(string message) => collector.EmitError(ConfigurationPath, message);
}
