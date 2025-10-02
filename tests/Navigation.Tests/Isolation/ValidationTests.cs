// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
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
		fileSystem.AddDirectory("/docs/api/nested-toc");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation(docSet, context);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("TableOfContents navigation does not allow nested children"));
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
		fileSystem.AddDirectory("/docs/api");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation(docSet, context);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Check using Errors count instead of Diagnostics collection
		context.Collector.Errors.Should().BeGreaterThan(0);
		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("TableOfContents navigation may only contain other TOC references as children"));
	}

	[Fact]
	public async Task ValidationEmitsErrorForNestedTocWithFileChildren()
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
		fileSystem.AddDirectory("/docs/setup/advanced/performance");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation(docSet, context);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Nested TOC under a root-level TOC should not allow file children
		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("TableOfContents navigation does not allow nested children"));
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
		fileSystem.AddDirectory("/docs/docs/guides/api/endpoints");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation(docSet, context);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Nested TOC structure under folders should still validate correctly
		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("TableOfContents navigation does not allow nested children"));
	}
}
