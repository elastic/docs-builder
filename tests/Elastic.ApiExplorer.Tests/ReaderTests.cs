// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

public class ReaderTests
{
	private static IFileInfo LocalSpecFile()
	{
		var fileSystem = new FileSystem();
		var path = fileSystem.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "elasticsearch-openapi-docs.json");
		return fileSystem.FileInfo.New(path);
	}

	[Fact]
	public async Task Reads()
	{
		var x = await OpenApiReader.Instance.ReadAsync(LocalSpecFile());

		x.Should().NotBeNull();
		x.BaseUri.Should().NotBeNull();
	}

	[Theory]
	[InlineData("json", /*lang=json,strict*/ """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0"},"paths":{}}""")]
	[InlineData("yaml", "openapi: 3.1.0\ninfo:\n  title: Test\n  version: 1.0\npaths: {}")]
	public async Task ReadsStream(string extension, string specification)
	{
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(specification));

		var document = await OpenApiReader.Instance.ReadAsync(stream, $"openapi.{extension}");

		document.Should().NotBeNull();
		document.Info.Title.Should().Be("Test");
	}

	[Fact]
	public async Task Navigation()
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(collector, FileSystemFactory.RealGitRootForPath(null), configurationContext);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);

		var openApiDocument = await OpenApiReader.Instance.ReadAsync(LocalSpecFile());
		openApiDocument.Should().NotBeNull();
		var navigation = generator.CreateNavigation("elasticsearch", openApiDocument);

		navigation.Should().NotBeNull();
	}
}
