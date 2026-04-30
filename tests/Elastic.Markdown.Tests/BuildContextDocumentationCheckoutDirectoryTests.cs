// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Nullean.ScopedFileSystem;
using Xunit;

namespace Elastic.Markdown.Tests;

/// <summary>
/// Regression: Codex must pass the repository clone root as <see cref="BuildContext"/> <c>source</c>
/// so <c>FindGitRoot(..., ceiling: rootFolder)</c> can see <c>.git</c> under the ceiling (#3115).
/// Prior bug used <c>DocsDirectory</c> only, capping the ceiling at the docs subtree.
/// </summary>
public class BuildContextDocumentationCheckoutDirectoryTests(ITestOutputHelper output)
{
	[Fact]
	public void SourceAsRepositoryRoot_SetsDocumentationCheckoutDirectory()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var repoPath = Path.Combine(root, "codex-checkout-dir-test");
		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = root });
		fs.AddDirectory(Path.Combine(repoPath, ".git"));
		fs.AddFile(Path.Combine(repoPath, "docs", "docset.yml"), new MockFileData("toc: []\n"));

		var readFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var writeFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(
			collector,
			readFs,
			writeFs,
			configurationContext,
			ExportOptions.Default,
			source: repoPath,
			output: Path.Combine(root, "codex-checkout-dir-test-out"));

		Assert.NotNull(context.DocumentationCheckoutDirectory);
		context.DocumentationCheckoutDirectory.FullName.Should().Be(repoPath);
	}

	[Fact]
	public void SourceAsDocsSubtreeOnly_LeavesDocumentationCheckoutDirectoryNull()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var repoPath = Path.Combine(root, "codex-docs-only-test");
		var docsPath = Path.Combine(repoPath, "docs");
		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = root });
		fs.AddDirectory(Path.Combine(repoPath, ".git"));
		fs.AddFile(Path.Combine(docsPath, "docset.yml"), new MockFileData("toc: []\n"));

		var readFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var writeFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(
			collector,
			readFs,
			writeFs,
			configurationContext,
			ExportOptions.Default,
			source: docsPath,
			output: Path.Combine(root, "codex-docs-only-test-out"));

		context.DocumentationCheckoutDirectory.Should().BeNull();
	}
}
