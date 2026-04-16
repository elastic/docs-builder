// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Tests;

public class ApiConfigurationTests
{
	[Fact]
	public void ApiConfiguration_ValidatesSingleSpec()
	{
		var config = new ApiConfiguration { Spec = "elasticsearch-openapi.json" };

		config.IsValid.Should().BeTrue();
		config.GetSpecPaths().Should().BeEquivalentTo(["elasticsearch-openapi.json"]);
	}

	[Fact]
	public void ApiConfiguration_ValidatesMultipleSpecs()
	{
		var config = new ApiConfiguration
		{
			Specs = ["elasticsearch-core.json", "elasticsearch-xpack.json"]
		};

		config.IsValid.Should().BeTrue();
		config.GetSpecPaths().Should().BeEquivalentTo(["elasticsearch-core.json", "elasticsearch-xpack.json"]);
	}

	[Fact]
	public void ApiConfiguration_InvalidWhenNoSpecs()
	{
		var config = new ApiConfiguration();

		config.IsValid.Should().BeFalse();
	}

	[Fact]
	public void ApiConfiguration_InvalidWhenBothSpecAndSpecs()
	{
		var config = new ApiConfiguration
		{
			Spec = "elasticsearch-openapi.json",
			Specs = ["elasticsearch-core.json"]
		};

		config.IsValid.Should().BeFalse();
	}

	[Fact]
	public void ApiConfiguration_WithTemplate()
	{
		var config = new ApiConfiguration
		{
			Spec = "elasticsearch-openapi.json",
			Template = "elasticsearch-api-overview.md"
		};

		config.IsValid.Should().BeTrue();
		config.Template.Should().Be("elasticsearch-api-overview.md");
	}
}

public class ApiConfigurationConverterTests
{
	private readonly IDeserializer _deserializer;

	public ApiConfigurationConverterTests() => _deserializer = new DeserializerBuilder()
			.WithTypeConverter(new ApiConfigurationConverter())
			.Build();

	[Fact]
	public void Converter_HandlesStringFormat()
	{
		const string yaml = "elasticsearch-openapi.json";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().Be("elasticsearch-openapi.json");
		config.Template.Should().BeNull();
		config.Specs.Should().BeNull();
	}

	[Fact]
	public void Converter_HandlesObjectFormat()
	{
		const string yaml = """
			spec: elasticsearch-openapi.json
			template: elasticsearch-api-overview.md
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().Be("elasticsearch-openapi.json");
		config.Template.Should().Be("elasticsearch-api-overview.md");
		config.Specs.Should().BeNull();
	}

	[Fact]
	public void Converter_HandlesMultiSpecFormat()
	{
		const string yaml = """
			specs:
			  - elasticsearch-core.json
			  - elasticsearch-xpack.json
			template: elasticsearch-api-overview.md
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().BeNull();
		config.Specs.Should().BeEquivalentTo(["elasticsearch-core.json", "elasticsearch-xpack.json"]);
		config.Template.Should().Be("elasticsearch-api-overview.md");
	}
}

public class ConfigurationFileApiTests
{
	[Fact]
	public void ConfigurationFile_ProcessesNewApiConfiguration()
	{
		// Arrange  
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiConfiguration>
			{
				["elasticsearch"] = new()
				{
					Spec = "elasticsearch-openapi.json",
					Template = "elasticsearch-api-overview.md"
				}
			}
		};

		var config = CreateConfiguration(docSetFile);

		// Assert
		config.ApiConfigurations.Should().NotBeNull();
		config.ApiConfigurations!.Should().ContainKey("elasticsearch");

		var elasticConfig = config.ApiConfigurations["elasticsearch"];
		elasticConfig.ProductKey.Should().Be("elasticsearch");
		elasticConfig.HasCustomTemplate.Should().BeTrue();
		elasticConfig.TemplateFile!.Name.Should().Be("elasticsearch-api-overview.md");
		elasticConfig.SpecFiles.Should().HaveCount(1);
		elasticConfig.PrimarySpecFile.Name.Should().Be("elasticsearch-openapi.json");

		// Backward compatibility
		config.OpenApiSpecifications.Should().NotBeNull();
		config.OpenApiSpecifications!.Should().ContainKey("elasticsearch");
		config.OpenApiSpecifications["elasticsearch"].Name.Should().Be("elasticsearch-openapi.json");
	}

	private static ConfigurationFile CreateConfiguration(DocumentationSetFile docSet)
	{
		var collector = new DiagnosticsCollector([]);
		var root = Paths.WorkingDirectoryRoot.FullName;
		var configFilePath = Path.Join(root, "docs", "_docset.yml");
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configFilePath, new MockFileData("") },
			{ Path.Join(root, "docs", "elasticsearch-openapi.json"), new MockFileData("{}") },
			{ Path.Join(root, "docs", "elasticsearch-api-overview.md"), new MockFileData("# Elasticsearch APIs") }
		}, root);

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(root, "docs"));

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDir);
		var versionsConfig = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
		};
		var productsConfig = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>().ToFrozenDictionary(),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		return new ConfigurationFile(docSet, context, versionsConfig, productsConfig);
	}

	private sealed class MockDocumentationSetContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IFileInfo configurationPath,
		IDirectoryInfo documentationSourceDirectory)
		: IDocumentationSetContext
	{
		public IDiagnosticsCollector Collector => collector;
		public ScopedFileSystem ReadFileSystem => WriteFileSystem;
		public ScopedFileSystem WriteFileSystem { get; } = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(fileSystem);
		public IDirectoryInfo OutputDirectory => fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts"));
		public IFileInfo ConfigurationPath => configurationPath;
		public BuildType BuildType => BuildType.Isolated;
		public IDirectoryInfo DocumentationSourceDirectory => documentationSourceDirectory;
		public GitCheckoutInformation Git => GitCheckoutInformation.Create(documentationSourceDirectory, fileSystem);
	}
}
