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
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.ApiExplorer.Tests;

public class ReaderTests
{
	private static IFileInfo LocalSpecFile()
	{
		var fileSystem = new FileSystem();
		var path = fileSystem.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "elasticsearch.json");
		return fileSystem.FileInfo.New(path);
	}

	[Test]
	public async Task Reads()
	{
		var x = await OpenApiReader.Instance.ReadAsync(LocalSpecFile());

		x.Should().NotBeNull();
		x.BaseUri.Should().NotBeNull();
	}

	[Test]
	[Arguments("json", /*lang=json,strict*/  """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0"},"paths":{}}""")]
	[Arguments("yaml", "openapi: 3.1.0\ninfo:\n  title: Test\n  version: 1.0\npaths: {}")]
	[Arguments("json", /*lang=json,strict*/  """{"swagger":"2.0","info":{"title":"Test","version":"1"},"paths":{},"host":"example.com","basePath":"/"}""")]
	[Arguments("yaml", "swagger: \"2.0\"\ninfo:\n  title: Test\n  version: \"1\"\nhost: example.com\nbasePath: /\npaths: {}")]
	public async Task ReadsStream(string extension, string specification)
	{
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(specification));

		var document = await OpenApiReader.Instance.ReadAsync(stream, $"openapi.{extension}");

		document.Should().NotBeNull();
		document.Info.Title.Should().Be("Test");
	}

	[Test]
	public async Task ReadsSwagger20Fixture()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "TestData", "swagger-2.0-sample.json");
		var fileSystem = new FileSystem();
		var fileInfo = fileSystem.FileInfo.New(path);

		var document = await OpenApiReader.Instance.ReadAsync(fileInfo);

		document.Should().NotBeNull();
		document.Info.Title.Should().Be("Sample API");
		document.Paths.Should().ContainKey("/deployments");
		document.Paths.Should().ContainKey("/deployments/{id}");
		document.Paths.Should().ContainKey("/users/login");
		document.Components?.SecuritySchemes.Should().ContainKey("apiKey");
		document.Components?.SecuritySchemes.Should().ContainKey("basicAuth");
	}

	[Test]
	public async Task ReadsSwagger20WithInvalidHost_EmitsError()
	{
		// A genuinely malformed host (whitespace, embedded scheme, etc.) must remain a hard
		// error. Only template placeholders ({{...}}) should be downgraded to warnings.
		const string spec = /*lang=json,strict*/
			"""{"swagger":"2.0","info":{"title":"T","version":"1"},"host":"not valid host","basePath":"/","paths":{}}""";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(spec));
		var collector = new DiagnosticsCollector([]);

		var document = await OpenApiReader.Instance.ReadAsync(stream, "spec.json", collector);

		document.Should().NotBeNull();
		collector.Errors.Should().BeGreaterThan(0, "a non-placeholder invalid host must remain a hard error");
		collector.Warnings.Should().Be(0);
	}

	[Test]
	public async Task ReadAsync_Yaml_QuotedNumericVersion_NotCoercedToNumber()
	{
		// Regression: a quoted "2.0" YAML scalar was coerced to the JSON number 2 by WriteScalar,
		// causing Microsoft.OpenApi's version detector to see "2" instead of "2.0" and fail to parse
		// the document as Swagger 2.0.
		const string yaml = "swagger: \"2.0\"\ninfo:\n  title: QuotedVersion\n  version: \"1\"\nhost: h\nbasePath: /\npaths: {}";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(yaml));

		var doc = await OpenApiReader.Instance.ReadAsync(stream, "spec.yaml");

		doc.Should().NotBeNull("a quoted '2.0' YAML scalar must not be coerced to the JSON number 2");
		doc!.Info.Title.Should().Be("QuotedVersion");
	}

	[Test]
	public async Task ReadsSwagger20WithTemplateHost_EmitsWarningNotError()
	{
		// Swagger 2.0 specs such as the ECE API use {{hostname}} as a placeholder value.
		// Microsoft.OpenApi treats that as an invalid host, but the document still parses
		// correctly. The reader must downgrade this to a warning so generation is not blocked.
		var path = Path.Combine(AppContext.BaseDirectory, "TestData", "ece-template-host.json");
		var fileSystem = new FileSystem();
		var fileInfo = fileSystem.FileInfo.New(path);
		var collector = new DiagnosticsCollector([]);

		var document = await OpenApiReader.Instance.ReadAsync(fileInfo, collector);

		document.Should().NotBeNull();
		document!.Info.Title.Should().Be("Elastic Cloud Enterprise API");
		document.Paths.Should().ContainKey("/account");
		collector.Errors.Should().Be(0, "an invalid-host placeholder must not be treated as a hard error");
		collector.Warnings.Should().BeGreaterThan(0, "the invalid-host diagnostic must be emitted as a warning");
	}

	[Test]
	public async Task Navigation()
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);

		var openApiDocument = await OpenApiReader.Instance.ReadAsync(LocalSpecFile());
		openApiDocument.Should().NotBeNull();
		var navigation = generator.CreateNavigation("elasticsearch", openApiDocument);

		navigation.Should().NotBeNull();
	}
}
