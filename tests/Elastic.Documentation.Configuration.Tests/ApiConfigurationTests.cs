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
	public void ApiConfiguration_InvalidWhenNoSpecs()
	{
		var config = new ApiConfiguration();

		config.IsValid.Should().BeFalse();
	}



	[Fact]
	public void ApiConfiguration_InvalidWhenSpecIsEmpty()
	{
		var config = new ApiConfiguration { Spec = "" };

		config.IsValid.Should().BeFalse();
	}

	[Fact]
	public void ApiConfiguration_InvalidWhenSpecIsWhitespace()
	{
		var config = new ApiConfiguration { Spec = "   " };

		config.IsValid.Should().BeFalse();
	}

}

public class ApiProductSequenceTests
{
	[Fact]
	public void ApiProductSequence_ValidatesRequiresAtLeastOneSpec()
	{
		var sequence = new ApiProductSequence
		{
			Entries = [new ApiProductEntry { File = "intro.md" }]
		};

		sequence.IsValid.Should().BeFalse();
	}

	[Fact]
	public void ApiProductSequence_ValidWithSpecOnly()
	{
		var sequence = new ApiProductSequence
		{
			Entries = [new ApiProductEntry { Spec = "api.json" }]
		};

		sequence.IsValid.Should().BeTrue();
		sequence.GetSpecPaths().Should().BeEquivalentTo(["api.json"]);
		sequence.GetIntroMarkdownFiles().Should().BeEmpty();
		sequence.GetOutroMarkdownFiles().Should().BeEmpty();
	}

	[Fact]
	public void ApiProductSequence_ValidWithIntroSpecOutro()
	{
		var sequence = new ApiProductSequence
		{
			Entries = [
				new ApiProductEntry { File = "intro.md" },
				new ApiProductEntry { Spec = "api.json" },
				new ApiProductEntry { File = "outro.md" }
			]
		};

		sequence.IsValid.Should().BeTrue();
		sequence.GetSpecPaths().Should().BeEquivalentTo(["api.json"]);
		sequence.GetIntroMarkdownFiles().Should().BeEquivalentTo(["intro.md"]);
		sequence.GetOutroMarkdownFiles().Should().BeEquivalentTo(["outro.md"]);
	}

	[Fact]
	public void ApiProductSequence_SeparatesIntroAndOutroFiles()
	{
		var sequence = new ApiProductSequence
		{
			Entries = [
				new ApiProductEntry { File = "intro1.md" },
				new ApiProductEntry { File = "intro2.md" },
				new ApiProductEntry { Spec = "api1.json" },
				new ApiProductEntry { Spec = "api2.json" },
				new ApiProductEntry { File = "outro1.md" },
				new ApiProductEntry { File = "outro2.md" }
			]
		};

		sequence.IsValid.Should().BeTrue();
		sequence.GetIntroMarkdownFiles().Should().BeEquivalentTo(["intro1.md", "intro2.md"]);
		sequence.GetSpecPaths().Should().BeEquivalentTo(["api1.json", "api2.json"]);
		sequence.GetOutroMarkdownFiles().Should().BeEquivalentTo(["outro1.md", "outro2.md"]);
	}

	[Fact]
	public void ApiProductEntry_ValidatesExactlyOneProperty()
	{
		var validFile = new ApiProductEntry { File = "test.md" };
		var validSpec = new ApiProductEntry { Spec = "test.json" };
		var invalidBoth = new ApiProductEntry { File = "test.md", Spec = "test.json" };
		var invalidNone = new ApiProductEntry();

		validFile.IsValid.Should().BeTrue();
		validFile.IsMarkdownFile.Should().BeTrue();
		validFile.IsOpenApiSpec.Should().BeFalse();

		validSpec.IsValid.Should().BeTrue();
		validSpec.IsMarkdownFile.Should().BeFalse();
		validSpec.IsOpenApiSpec.Should().BeTrue();

		invalidBoth.IsValid.Should().BeFalse();
		invalidNone.IsValid.Should().BeFalse();
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
		config.Specs.Should().BeNull();
	}

	[Fact]
	public void Converter_HandlesObjectFormat_IgnoresTemplate()
	{
		const string yaml = """
			spec: elasticsearch-openapi.json
			template: elasticsearch-api-overview.md
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().Be("elasticsearch-openapi.json");
		config.Specs.Should().BeNull();
	}


	[Fact]
	public void Converter_SkipsUnknownPropertiesWithNestedContent()
	{
		const string yaml = """
			spec: elasticsearch-openapi.json
			unknown_nested:
			  foo: bar
			  nested:
			    deep: value
			template: elasticsearch-api-overview.md
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().Be("elasticsearch-openapi.json");
		config.Specs.Should().BeNull();
	}

	[Fact]
	public void Converter_HandlesWrongTokenTypeForSpec()
	{
		const string yaml = """
			spec:
			  nested: value
			template: elasticsearch-api-overview.md
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().BeNull(); // Should be null when wrong token type
		config.Specs.Should().BeNull();
	}

	[Fact]
	public void Converter_IgnoresTemplateWithWrongTokenType()
	{
		const string yaml = """
			spec: elasticsearch-openapi.json
			template:
			  - item1
			  - item2
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().Be("elasticsearch-openapi.json");
		config.Specs.Should().BeNull();
	}

	[Fact]
	public void Converter_IgnoresSpecsPropertyForNow()
	{
		const string yaml = """
			spec: elasticsearch-openapi.json
			specs:
			  - elasticsearch-core.json
			  - elasticsearch-xpack.json
			template: elasticsearch-api-overview.md
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().Be("elasticsearch-openapi.json");
		// Specs should remain null since multi-spec support is deferred
		config.Specs.Should().BeNull();
	}

	[Fact]
	public void Converter_HandlesComplexUnknownStructures()
	{
		const string yaml = """
			spec: elasticsearch-openapi.json
			complex_unknown:
			  level1:
			    level2:
			      - item1
			      - item2
			      - nested:
			          deep: value
			another_unknown:
			  - array_item
			template: elasticsearch-api-overview.md
			""";

		var config = _deserializer.Deserialize<ApiConfiguration>(yaml);

		config.Should().NotBeNull();
		config.Spec.Should().Be("elasticsearch-openapi.json");
		config.Specs.Should().BeNull();
	}
}

public class ApiProductSequenceConverterTests
{
	private readonly IDeserializer _deserializer;

	public ApiProductSequenceConverterTests() => _deserializer = new DeserializerBuilder()
			.WithTypeConverter(new ApiConfigurationConverter())
			.Build();

	[Fact]
	public void Converter_HandlesSequenceFormat()
	{
		const string yaml = """
			- file: intro.md
			- spec: api.json
			- file: outro.md
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.Should().NotBeNull();
		sequence.IsValid.Should().BeTrue();
		sequence.Entries.Should().HaveCount(3);
		sequence.GetIntroMarkdownFiles().Should().BeEquivalentTo(["intro.md"]);
		sequence.GetSpecPaths().Should().BeEquivalentTo(["api.json"]);
		sequence.GetOutroMarkdownFiles().Should().BeEquivalentTo(["outro.md"]);
	}

	[Fact]
	public void Converter_ConvertLegacyStringToSequence()
	{
		const string yaml = "api.json";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.Should().NotBeNull();
		sequence.IsValid.Should().BeTrue();
		sequence.Entries.Should().HaveCount(1);
		sequence.GetSpecPaths().Should().BeEquivalentTo(["api.json"]);
		sequence.GetIntroMarkdownFiles().Should().BeEmpty();
		sequence.GetOutroMarkdownFiles().Should().BeEmpty();
	}

	[Fact]
	public void Converter_ConvertLegacyObjectToSequence_IgnoresTemplate()
	{
		const string yaml = """
			spec: api.json
			template: template.md
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.Should().NotBeNull();
		sequence.IsValid.Should().BeTrue();
		sequence.Entries.Should().HaveCount(1);
		sequence.GetIntroMarkdownFiles().Should().BeEmpty();
		sequence.GetSpecPaths().Should().BeEquivalentTo(["api.json"]);
		sequence.GetOutroMarkdownFiles().Should().BeEmpty();
	}

	[Fact]
	public void Converter_ConvertLegacyObjectWithoutTemplateToSequence()
	{
		const string yaml = """
			spec: api.json
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.Should().NotBeNull();
		sequence.IsValid.Should().BeTrue();
		sequence.Entries.Should().HaveCount(1);
		sequence.GetSpecPaths().Should().BeEquivalentTo(["api.json"]);
		sequence.GetIntroMarkdownFiles().Should().BeEmpty();
		sequence.GetOutroMarkdownFiles().Should().BeEmpty();
	}
}

public class ConfigurationFileApiTests
{
	[Fact]
	public void ConfigurationFile_ProcessesNewApiSequenceConfiguration()
	{
		// Arrange  
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries = [
						new ApiProductEntry { File = "intro.md" },
						new ApiProductEntry { Spec = "elasticsearch-openapi.json" },
						new ApiProductEntry { File = "outro.md" }
					]
				}
			}
		};

		var config = CreateConfiguration(docSetFile);

		// Assert
		config.ApiConfigurations.Should().NotBeNull();
		config.ApiConfigurations.Should().ContainKey("elasticsearch");

		var elasticConfig = config.ApiConfigurations["elasticsearch"];
		elasticConfig.ProductKey.Should().Be("elasticsearch");
		elasticConfig.IntroMarkdownFiles.Should().HaveCount(1);
		elasticConfig.IntroMarkdownFiles[0].Name.Should().Be("intro.md");
		elasticConfig.SpecFiles.Should().HaveCount(1);
		elasticConfig.PrimarySpecFile.Name.Should().Be("elasticsearch-openapi.json");
		elasticConfig.OutroMarkdownFiles.Should().HaveCount(1);
		elasticConfig.OutroMarkdownFiles[0].Name.Should().Be("outro.md");

		// Backward compatibility
		config.OpenApiSpecifications.Should().NotBeNull();
		config.OpenApiSpecifications.Should().ContainKey("elasticsearch");
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
			{ Path.Join(root, "docs", "elasticsearch-api-overview.md"), new MockFileData("# Elasticsearch APIs") },
			{ Path.Join(root, "docs", "intro.md"), new MockFileData("# Introduction") },
			{ Path.Join(root, "docs", "outro.md"), new MockFileData("# Conclusion") }
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
