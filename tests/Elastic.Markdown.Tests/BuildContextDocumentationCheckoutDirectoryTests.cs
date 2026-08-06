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
/// Tests that <see cref="BuildContext.DocumentationCheckoutDirectory"/> resolves correctly
/// for various <c>--path</c> / <c>source</c> combinations.
/// <para>
/// Key invariant: <c>--path repo/</c> and <c>--path repo/docs/</c> must resolve to the
/// <em>same</em> <c>CheckoutDirectory</c> because the docset scan converges first (both land on
/// <c>repo/docs/</c> as the anchor), and <c>FindGitRoot</c> then walks at most one parent.
/// </para>
/// <para>
/// Regression guard (#3115): Codex passes the repository clone root as <c>source</c> so that
/// <c>FindGitRoot</c> can see <c>.git</c> within the default <c>maxParents</c> range.
/// </para>
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
	public void SourceAsDocsSubtree_ResolvesCheckoutFromParent()
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

		// --path repo/docs/ now resolves the same checkout as --path repo/:
		// the docset scan anchors at repo/docs/, FindGitRoot walks one parent to repo/.git
		Assert.NotNull(context.DocumentationCheckoutDirectory);
		context.DocumentationCheckoutDirectory.FullName.Should().Be(repoPath);
	}

	[Fact]
	public void PathAndDocsSubfolder_ResolveIdenticalCheckout()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var repoPath = Path.Combine(root, "codex-equiv-test");
		var docsPath = Path.Combine(repoPath, "docs");
		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = root });
		fs.AddDirectory(Path.Combine(repoPath, ".git"));
		fs.AddFile(Path.Combine(docsPath, "docset.yml"), new MockFileData("toc: []\n"));

		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);

		var contextFromRepoRoot = new BuildContext(
			collector,
			FileSystemFactory.ScopeCurrentWorkingDirectory(fs),
			FileSystemFactory.ScopeCurrentWorkingDirectory(fs),
			configurationContext,
			ExportOptions.Default,
			source: repoPath,
			output: Path.Combine(root, "codex-equiv-test-out"));

		var contextFromDocsFolder = new BuildContext(
			collector,
			FileSystemFactory.ScopeCurrentWorkingDirectory(fs),
			FileSystemFactory.ScopeCurrentWorkingDirectory(fs),
			configurationContext,
			ExportOptions.Default,
			source: docsPath,
			output: Path.Combine(root, "codex-equiv-test-out"));

		contextFromRepoRoot.DocumentationCheckoutDirectory.Should().NotBeNull();
		contextFromDocsFolder.DocumentationCheckoutDirectory.Should().NotBeNull();
		contextFromRepoRoot.DocumentationCheckoutDirectory.FullName
			.Should().Be(contextFromDocsFolder.DocumentationCheckoutDirectory.FullName,
				"--path repo/ and --path repo/docs/ must resolve to the same CheckoutDirectory");
		contextFromRepoRoot.DocumentationSourceDirectory.FullName
			.Should().Be(contextFromDocsFolder.DocumentationSourceDirectory.FullName,
				"--path repo/ and --path repo/docs/ must resolve to the same SourceDirectory");
	}
}
