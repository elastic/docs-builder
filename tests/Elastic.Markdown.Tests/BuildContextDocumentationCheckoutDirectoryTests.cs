// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.FileSystems;
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

		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var docFs = DocumentationFileSystem.Resolve(
			fs.DirectoryInfo.New(repoPath),
			new DocumentationScopeOptions { Inner = fs, Output = Path.Join(root, "codex-checkout-dir-test-out") }
		);
		var context = new BuildContext(collector, docFs, configurationContext);

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

		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var docFs = DocumentationFileSystem.Resolve(
			fs.DirectoryInfo.New(docsPath),
			new DocumentationScopeOptions { Inner = fs, Output = Path.Join(root, "codex-docs-only-test-out") }
		);
		var context = new BuildContext(collector, docFs, configurationContext);

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
		var opts = new DocumentationScopeOptions { Inner = fs, Output = Path.Combine(root, "codex-equiv-test-out") };

		var fsFromRepoRoot = DocumentationFileSystem.Resolve(fs.DirectoryInfo.New(repoPath), opts);
		var fsFromDocsFolder = DocumentationFileSystem.Resolve(fs.DirectoryInfo.New(docsPath), opts);
		var contextFromRepoRoot = new BuildContext(collector, fsFromRepoRoot, configurationContext);
		var contextFromDocsFolder = new BuildContext(collector, fsFromDocsFolder, configurationContext);

		contextFromRepoRoot.DocumentationCheckoutDirectory.Should().NotBeNull();
		contextFromDocsFolder.DocumentationCheckoutDirectory.Should().NotBeNull();
		contextFromRepoRoot
			.DocumentationCheckoutDirectory
			.FullName
			.Should()
			.Be(
				contextFromDocsFolder.DocumentationCheckoutDirectory.FullName,
				"--path repo/ and --path repo/docs/ must resolve to the same CheckoutDirectory"
			);
		contextFromRepoRoot
			.DocumentationSourceDirectory
			.FullName
			.Should()
			.Be(
				contextFromDocsFolder.DocumentationSourceDirectory.FullName,
				"--path repo/ and --path repo/docs/ must resolve to the same SourceDirectory"
			);
	}
}
