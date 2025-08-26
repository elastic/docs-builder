// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.ApiExplorer.Tests;

public class ReaderTests
{

	[Fact]
	public async Task Reads()
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(collector, new FileSystem(), configurationContext);

		context.Configuration.OpenApiSpecifications.Should().NotBeNull().And.NotBeEmpty();

		var x = await OpenApiReader.Create(context.Configuration.OpenApiSpecifications.First().Value);

		x.Should().NotBeNull();
		x.BaseUri.Should().NotBeNull();
	}

	[Fact]
	public async Task Navigation()
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(collector, new FileSystem(), configurationContext);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		context.Configuration.OpenApiSpecifications.Should().NotBeNull().And.NotBeEmpty();

		var (urlPathPrefix, fi) = context.Configuration.OpenApiSpecifications.First();
		var openApiDocument = await OpenApiReader.Create(fi);
		openApiDocument.Should().NotBeNull();
		var navigation = generator.CreateNavigation(urlPathPrefix, openApiDocument);

		navigation.Should().NotBeNull();
	}
}
