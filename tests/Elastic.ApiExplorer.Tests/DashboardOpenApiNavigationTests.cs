// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

public class DashboardOpenApiNavigationTests
{
	/// <summary>
	/// Dashboard OpenAPI (single tag for all operations) must populate the API explorer nav.
	/// Regression: grouped navigation overwrote root items with an empty top-level list.
	/// </summary>
	[Fact]
	public async Task CreateNavigation_SingleTagOpenApiSpec_HasSidebarItems()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(new DiagnosticsCollector([]), FileSystemFactory.RealRead, configurationContext);
		var fs = new FileSystem();
		var path = fs.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "dashboard-openapi.json");
		var fi = fs.FileInfo.New(path);
		var doc = await OpenApiReader.Create(fi);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("dashboard", doc);

		navigation.NavigationItems.Should().NotBeEmpty();
	}
}
