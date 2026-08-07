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
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

public class ReaderTests
{

	[Fact]
	public async Task Reads()
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(collector, FileSystemFactory.RealGitRootForPath(null), configurationContext);

		context.Configuration.ApiConfigurations.Should().NotBeNull().And.NotBeEmpty();

		var firstApiConfig = context.Configuration.ApiConfigurations.First().Value;
		firstApiConfig.LocalSpecFile.Should().NotBeNull();

		var x = await OpenApiReader.Create(firstApiConfig.LocalSpecFile);

		x.Should().NotBeNull();
		x.BaseUri.Should().NotBeNull();
	}

	[Fact]
	public async Task Navigation()
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(collector, FileSystemFactory.RealGitRootForPath(null), configurationContext);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		context.Configuration.ApiConfigurations.Should().NotBeNull().And.NotBeEmpty();

		var (urlPathPrefix, apiConfig) = context.Configuration.ApiConfigurations.First();
		apiConfig.LocalSpecFile.Should().NotBeNull();
		var openApiDocument = await OpenApiReader.Create(apiConfig.LocalSpecFile);
		openApiDocument.Should().NotBeNull();
		var navigation = generator.CreateNavigation(urlPathPrefix, openApiDocument, apiConfig);

		navigation.Should().NotBeNull();
	}
}
