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
		var context = new BuildContext(collector, new FileSystem());

		context.Configuration.OpenApiSpecification.Should().NotBeNull();

		var x = await OpenApiReader.Create(context.Configuration.OpenApiSpecification);

		x.Should().NotBeNull();
		x.BaseUri.Should().NotBeNull();
	}

	[Fact]
	public async Task Navigation()
	{
		var collector = new DiagnosticsCollector([]);
		var context = new BuildContext(collector, new FileSystem());
		var generator = new OpenApiGenerator(context, NoopMarkdownStringRenderer.Instance, NullLoggerFactory.Instance);
		context.Configuration.OpenApiSpecification.Should().NotBeNull();

		var openApiDocument = await OpenApiReader.Create(context.Configuration.OpenApiSpecification);
		openApiDocument.Should().NotBeNull();
		var navigation = generator.CreateNavigation(openApiDocument);

		navigation.Should().NotBeNull();
	}
}
