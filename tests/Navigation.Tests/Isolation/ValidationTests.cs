// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Node;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class ValidationTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public async Task ValidationEmitsErrorWhenTableOfContentsHasNonTocChildrenAndNestedTocNotAllowed()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - toc: api
		               children:
		                 - toc: nested-toc
		                   children:
		                     - file: should-error.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/api");
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Message.Contains("may not contain children, define children in") &&
			d.Message.Contains("toc.yml"));
	}

	[Fact]
	public async Task ValidationEmitsErrorWhenTableOfContentsHasNonTocChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - toc: api
		               children:
		                 - file: should-error.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Check using Errors count instead of Diagnostics collection
		context.Collector.Errors.Should().BeGreaterThan(0);
		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Message.Contains("may not contain children, define children in") &&
			d.Message.Contains("toc.yml"));
	}

	[Fact]
	public void ValidationEmitsErrorForNestedTocWithFileChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: setup
		               children:
		                 - toc: advanced
		                   children:
		                     - toc: performance
		                       children:
		                         - file: index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/setup/advanced");
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Nested TOC under a root-level TOC should not allow file children
		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Message.Contains("may not contain children, define children in") &&
			d.Message.Contains("toc.yml"));
	}

	[Fact]
	public async Task ValidationEmitsErrorForDeeplyNestedFolderWithInvalidTocStructure()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		               children:
		                 - folder: guides
		                   children:
		                     - toc: api
		                       children:
		                         - toc: endpoints
		                           children:
		                             - file: users.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/docs/guides/api");
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Nested TOC structure under folders should still validate correctly
		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Message.Contains("may not contain children, define children in") &&
			d.Message.Contains("toc.yml"));
	}

	[Fact]
	public async Task ValidationEmitsErrorWhenTocYmlFileNotFound()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - toc: api
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/api");
		// Note: not adding /docs/api/toc.yml file
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("Table of contents file not found") &&
			d.Message.Contains("api/toc.yml"));
	}

	[Fact]
	public async Task ValidationEmitsHintForDeepLinkingVirtualFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: a/b/c/getting-started.md
		               children:
		                 - file: a/b/c/setup.md
		                 - file: a/b/c/install.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		context.Collector.Hints.Should().BeGreaterThan(0, "should have emitted a hint for deep-linking virtual file");
		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Hint &&
			d.Message.Contains("a/b/c/getting-started.md") &&
			d.Message.Contains("deep-linking") &&
			d.Message.Contains("folder"));
	}

	[Fact]
	public async Task ValidationEmitsHintForNestedPathVirtualFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: guides/api/overview.md
		               children:
		                 - file: guides/api/authentication.md
		                 - file: guides/api/endpoints.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		context.Collector.Hints.Should().BeGreaterThan(0);
		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Hint &&
			d.Message.Contains("guides/api/overview.md") &&
			d.Message.Contains("Virtual files are primarily intended to group sibling files together"));
	}

	[Fact]
	public async Task ValidationDoesNotEmitHintForSimpleVirtualFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: chapter1.md
		                 - file: chapter2.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		context.Collector.Hints.Should().Be(0, "simple virtual files without deep-linking should not trigger hints");
		context.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ValidationDoesNotEmitHintForFilesWithoutChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: a/b/c/getting-started.md
		             - file: guides/setup.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		context.Collector.Hints.Should().Be(0, "files without children should not trigger hints, even with deep paths");
		context.Diagnostics.Should().BeEmpty();
	}
}
