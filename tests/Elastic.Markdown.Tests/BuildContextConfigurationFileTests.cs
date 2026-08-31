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
/// Regression for https://github.com/elastic/docs-builder/issues/3767: Codex clone discovery may
/// prefer a non-default docset (e.g. `docs-dev/` with `registry: internal`) over a sibling public
/// `docs/docset.yml`. Build must honor that explicit choice instead of rediscovering `docs/` from
/// the repository root via <see cref="Paths.FindDocsFolderFromRoot"/>.
/// </summary>
public class BuildContextConfigurationFileTests(ITestOutputHelper output)
{
	[Fact]
	public void ExplicitConfigurationFile_OverridesDefaultDiscovery()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var repoPath = Path.Combine(root, "codex-configuration-file-test");
		var publicDocsetPath = Path.Combine(repoPath, "docs", "docset.yml");
		var internalDocsetPath = Path.Combine(repoPath, "docs-dev", "docset.yml");

		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = root });
		fs.AddDirectory(Path.Combine(repoPath, ".git"));
		fs.AddFile(publicDocsetPath, new MockFileData("toc: []\n"));
		fs.AddFile(internalDocsetPath, new MockFileData("registry: internal\ntoc: []\n"));

		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var docFs = DocumentationFileSystem.Resolve(
			fs.DirectoryInfo.New(repoPath),
			new DocumentationScopeOptions
			{
				Inner = fs,
				ConfigurationFile = internalDocsetPath,
				Output = Path.Join(root, "codex-configuration-file-test-out")
			}
		);

		var context = new BuildContext(collector, docFs, configurationContext);

		context.ConfigurationPath.FullName.Should().Be(internalDocsetPath);
		context.DocumentationSourceDirectory.FullName.Should().Be(Path.Combine(repoPath, "docs-dev"));
		// source stays the repository root so #3115's git-ceiling behavior is unaffected.
		context.DocumentationCheckoutDirectory.Should().NotBeNull();
		context.DocumentationCheckoutDirectory.FullName.Should().Be(repoPath);
	}

	[Fact]
	public void NoExplicitConfigurationFile_FallsBackToDefaultDiscovery()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var repoPath = Path.Combine(root, "codex-configuration-file-fallback-test");
		var publicDocsetPath = Path.Combine(repoPath, "docs", "docset.yml");

		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = root });
		fs.AddDirectory(Path.Combine(repoPath, ".git"));
		fs.AddFile(publicDocsetPath, new MockFileData("toc: []\n"));

		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var docFs = DocumentationFileSystem.Resolve(
			fs.DirectoryInfo.New(repoPath),
			new DocumentationScopeOptions { Inner = fs, Output = Path.Join(root, "codex-configuration-file-fallback-test-out") }
		);

		var context = new BuildContext(collector, docFs, configurationContext);

		context.ConfigurationPath.FullName.Should().Be(publicDocsetPath);
	}
}
