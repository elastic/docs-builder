// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.ApiExplorer.Tests;

public class DashboardOpenApiNavigationTests
{
	/// <summary>
	/// Dashboard OpenAPI (single tag for all operations) must populate the API explorer nav.
	/// Regression: grouped navigation overwrote root items with an empty top-level list.
	/// </summary>
	[Test]
	public async Task CreateNavigation_SingleTagOpenApiSpec_HasSidebarItems()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var fs = new FileSystem();
		var path = fs.Path.Combine(AppContext.BaseDirectory, "TestData", "dashboard-openapi.json");
		var fi = fs.FileInfo.New(path);
		var doc = await OpenApiReader.Instance.ReadAsync(fi);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("dashboard", doc);

		navigation.NavigationItems.Should().NotBeEmpty();
	}

	[Test]
	public async Task CreateNavigation_BlankSummary_TitlesOperationByHumanizedOperationId()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var fs = new FileSystem();
		var fi = fs.FileInfo.New(fs.Path.Combine(AppContext.BaseDirectory, "TestData", "blank-summary-openapi.json"));
		var doc = await OpenApiReader.Instance.ReadAsync(fi);

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("blank", doc);

		var titles = Flatten(navigation.NavigationItems).OfType<OperationNavigationItem>().Select(o => o.NavigationTitle).ToArray();
		titles.Should().ContainSingle().Which.Should().Be("Get Script");
	}

	[Test]
	public async Task CreateNavigation_OperationIdsWithTheSameTitle_StaySeparatePages()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var fs = new FileSystem();
		var fi = fs.FileInfo.New(fs.Path.Combine(AppContext.BaseDirectory, "TestData", "colliding-operation-ids-openapi.json"));
		var doc = await OpenApiReader.Instance.ReadAsync(fi);

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("colliding", doc);

		var operations = Flatten(navigation.NavigationItems).OfType<OperationNavigationItem>().ToArray();
		operations.Should().HaveCount(2);
		operations.Select(o => o.Url).Should().OnlyHaveUniqueItems();
		operations.Should().OnlyContain(o => o.Siblings.Count == 0);
		operations.Select(o => o.NavigationTitle).Should().AllBe("Get Script");
	}

	private static IEnumerable<INavigationItem> Flatten(IEnumerable<INavigationItem> items)
	{
		foreach (var item in items)
		{
			yield return item;
			if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
				foreach (var child in Flatten(node.NavigationItems))
					yield return child;
		}
	}
}
