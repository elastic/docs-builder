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
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Tests;

public class ApiProductEntryTests
{
	[Fact]
	public void HasSpec_And_HasProduct_ReflectPresence()
	{
		var entry = new ApiProductEntry { Spec = "api.json", Product = "elasticsearch" };

		entry.HasSpec.Should().BeTrue();
		entry.HasProduct.Should().BeTrue();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void HasSpec_FalseWhenBlank(string? spec)
	{
		var entry = new ApiProductEntry { Spec = spec, Product = "elasticsearch" };

		entry.HasSpec.Should().BeFalse();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void HasProduct_FalseWhenBlank(string? product)
	{
		var entry = new ApiProductEntry { Spec = "api.json", Product = product };

		entry.HasProduct.Should().BeFalse();
	}
}

public class ApiProductSequenceTests
{
	[Fact]
	public void IsValid_TrueWithExactlyOneEntry()
	{
		var sequence = new ApiProductSequence { Entries = [new ApiProductEntry { Product = "elasticsearch" }] };

		sequence.IsValid.Should().BeTrue();
		sequence.SingleEntry.Should().NotBeNull();
	}

	[Fact]
	public void IsValid_FalseWhenEmpty()
	{
		var sequence = new ApiProductSequence();

		sequence.IsValid.Should().BeFalse();
		sequence.SingleEntry.Should().BeNull();
	}

	[Fact]
	public void IsValid_FalseWithMultipleEntries()
	{
		var sequence = new ApiProductSequence
		{
			Entries = [new ApiProductEntry { Product = "elasticsearch" }, new ApiProductEntry { Product = "kibana" }]
		};

		sequence.IsValid.Should().BeFalse();
		sequence.SingleEntry.Should().BeNull();
	}
}

public class ApiConfigurationConverterTests
{
	private readonly IDeserializer _deserializer = new DeserializerBuilder().WithTypeConverter(new ApiConfigurationConverter()).Build();

	[Fact]
	public void AcceptsStrictEntry_WithSpecProductAndChildren()
	{
		const string yaml =
			"""
			- spec: elasticsearch-openapi.json
			  product: elasticsearch
			  children:
			    - file: getting-started.md
			    - file: authentication.md
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.IsValid.Should().BeTrue();
		var entry = sequence.SingleEntry!;
		entry.Spec.Should().Be("elasticsearch-openapi.json");
		entry.Product.Should().Be("elasticsearch");
		entry.Children.Should().HaveCount(2);
		entry.Children[0].File.Should().Be("getting-started.md");
		entry.Children[1].File.Should().Be("authentication.md");
	}

	[Fact]
	public void ConverterAllowsMissingSpec_RequirednessValidatedDownstream()
	{
		// The converter only enforces shape (one entry, valid keys). 'spec:' is semantically
		// required, but that is validated by ConfigurationFile.ResolveApiEntry, not here, so it
		// can attribute a precise line/column diagnostic.
		const string yaml = """
			- product: kibana
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.IsValid.Should().BeTrue();
		var entry = sequence.SingleEntry!;
		entry.HasSpec.Should().BeFalse();
		entry.Product.Should().Be("kibana");
	}

	[Fact]
	public void AcceptsStrictEntry_WithoutChildren()
	{
		const string yaml = """
			- spec: api.json
			  product: elasticsearch
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.SingleEntry!.Children.Should().BeEmpty();
	}

	[Fact]
	public void RecordsEntryAndProductMarks()
	{
		const string yaml = """
			- spec: api.json
			  product: elasticsearch
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);
		var entry = sequence.SingleEntry!;

		entry.Line.Should().Be(1);
		entry.ProductLine.Should().Be(2);
	}

	[Fact]
	public void SkipsUnknownKeys()
	{
		const string yaml =
			"""
			- spec: api.json
			  product: elasticsearch
			  unknown_key: some value
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.SingleEntry!.Product.Should().Be("elasticsearch");
	}

	[Fact]
	public void MultipleEntries_ParseButAreStructurallyInvalid()
	{
		const string yaml =
			"""
			- spec: api1.json
			  product: elasticsearch
			- spec: api2.json
			  product: kibana
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.Entries.Should().HaveCount(2);
		sequence.IsValid.Should().BeFalse();
	}

	[Fact]
	public void AcceptsRepositoryOverride()
	{
		const string yaml =
			"""
			- spec: elasticsearch-openapi.json
			  product: elasticsearch
			  repository: elastic/elasticsearch-specification
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.SingleEntry!.Repository.Should().Be("elastic/elasticsearch-specification");
	}

	[Fact]
	public void RepositoryOverride_IsOptional()
	{
		const string yaml = """
			- spec: api.json
			  product: elasticsearch
			""";

		var sequence = _deserializer.Deserialize<ApiProductSequence>(yaml);

		sequence.SingleEntry!.Repository.Should().BeNull();
	}

	[Fact]
	public void RejectsLegacyScalarShape()
	{
		const string yaml = "elasticsearch-openapi.json";

		var act = () => _deserializer.Deserialize<ApiProductSequence>(yaml);

		act.Should().Throw<YamlException>();
	}

	[Fact]
	public void RejectsLegacyObjectShape()
	{
		const string yaml = """
			spec: elasticsearch-openapi.json
			""";

		var act = () => _deserializer.Deserialize<ApiProductSequence>(yaml);

		act.Should().Throw<YamlException>();
	}

	[Fact]
	public void RejectsLegacyIntroSpecOutroSequenceShape()
	{
		const string yaml = """
			- file: intro.md
			- spec: api.json
			- file: outro.md
			""";

		var act = () => _deserializer.Deserialize<ApiProductSequence>(yaml);

		act.Should().Throw<YamlException>().WithMessage("*legacy intro/outro shape*");
	}
}

public class ConfigurationFileApiTests
{
	[Fact]
	public void ResolvesLocalSpecProductAndChildren()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry
						{
							Spec = "elasticsearch-openapi.json",
							Product = "elasticsearch",
							Children = [new ApiEntryChild { File = "getting-started.md" }]
						}
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(0);
		config.ApiConfigurations.Should().NotBeNull();
		var resolved = config.ApiConfigurations["elasticsearch"];
		resolved.ProductKey.Should().Be("elasticsearch");
		resolved.Product.Id.Should().Be("elasticsearch");
		resolved.SpecFileName.Should().Be("elasticsearch-openapi.json");
		resolved.LocalSpecFile.Should().NotBeNull();
		resolved.LocalSpecFile.Name.Should().Be("elasticsearch-openapi.json");
		resolved.Children.Should().HaveCount(1);
		resolved.Children[0].Name.Should().Be("getting-started.md");
	}

	[Fact]
	public void ResolvesSpec_WhenLocalFileAbsent_ForRemoteResolution()
	{
		// A declared 'spec:' that does not exist on disk is expected, not an error: it means
		// the current version resolves remotely through the version index instead of a local
		// override. The basename is still captured for that remote lookup.
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries = [new ApiProductEntry { Spec = "elasticsearch-openapi.json", Product = "elasticsearch" }]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile, withLocalSpecFile: false);

		collector.Errors.Should().Be(0);
		var resolved = config.ApiConfigurations!["elasticsearch"];
		resolved.SpecFileName.Should().Be("elasticsearch-openapi.json");
		resolved.LocalSpecFile.Should().BeNull();
	}

	[Fact]
	public void ResolvesSpecFileName_FromBasenameOfNestedPath()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries = [new ApiProductEntry { Spec = "specs/elasticsearch-openapi.json", Product = "elasticsearch" }]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile, withLocalSpecFile: false);

		collector.Errors.Should().Be(0);
		var resolved = config.ApiConfigurations!["elasticsearch"];
		resolved.SpecFileName.Should().Be("elasticsearch-openapi.json");
		resolved.LocalSpecFile.Should().BeNull();
	}

	[Fact]
	public void EmitsError_WhenSpecMissing()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new() { Entries = [new ApiProductEntry { Product = "elasticsearch" }] }
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations.Should().BeNull();
	}

	[Fact]
	public void EmitsError_WhenSpecEscapesDocumentationSourceDirectory()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new() { Entries = [new ApiProductEntry { Spec = "../../outside.json", Product = "elasticsearch" }] }
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations.Should().BeNull();
	}

	[Fact]
	public void EmitsError_WhenProductMissing()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new() { Entries = [new ApiProductEntry { Spec = "elasticsearch-openapi.json" }] }
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations.Should().BeNull();
	}

	[Fact]
	public void EmitsError_WhenProductUnknown()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries = [new ApiProductEntry { Spec = "elasticsearch-openapi.json", Product = "not-a-product" }]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations.Should().BeNull();
	}

	[Fact]
	public void NormalizesUnderscoreProductId()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["dashboard"] = new()
				{
					Entries = [new ApiProductEntry { Spec = "dashboard-openapi.json", Product = "under_score_product" }]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile, extraProducts: ["under-score-product"]);

		collector.Errors.Should().Be(0);
		config.ApiConfigurations!["dashboard"].Product.Id.Should().Be("under-score-product");
	}

	[Fact]
	public void ResolvesRepositoryOverride()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry
						{
							Spec = "elasticsearch-openapi.json",
							Product = "elasticsearch",
							Repository = "elastic/elasticsearch-specification"
						}
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(0);
		config.ApiConfigurations!["elasticsearch"].Repository.Should().Be("elastic/elasticsearch-specification");
	}

	[Fact]
	public void Repository_DefaultsToNull_WhenOmitted()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries = [new ApiProductEntry { Spec = "elasticsearch-openapi.json", Product = "elasticsearch" }]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(0);
		config.ApiConfigurations!["elasticsearch"].Repository.Should().BeNull();
	}

	[Theory]
	[InlineData("no-slash")]
	[InlineData("/leading-slash")]
	[InlineData("trailing-slash/")]
	public void EmitsError_WhenRepositoryNotInOrgSlashRepoForm(string repository)
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry { Spec = "elasticsearch-openapi.json", Product = "elasticsearch", Repository = repository }
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations.Should().BeNull();
	}

	[Fact]
	public void EmitsError_WhenMultipleEntries()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries = [new ApiProductEntry { Product = "elasticsearch" }, new ApiProductEntry { Product = "elasticsearch" }]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations.Should().BeNull();
	}

	[Fact]
	public void EmitsError_WhenChildFileMissing()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry
						{
							Spec = "elasticsearch-openapi.json",
							Product = "elasticsearch",
							Children = [new ApiEntryChild { File = "missing.md" }]
						}
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations!["elasticsearch"].Children.Should().BeEmpty();
	}

	[Fact]
	public void EmitsError_WhenChildPathEscapesApiKeyDirectory()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry
						{
							Spec = "elasticsearch-openapi.json",
							Product = "elasticsearch",
							Children = [new ApiEntryChild { File = "../../outside.md" }]
						}
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations!["elasticsearch"].Children.Should().BeEmpty();
	}

	[Fact]
	public void EmitsError_WhenChildFileUsesSupplementalName()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry
						{
							Spec = "elasticsearch-openapi.json",
							Product = "elasticsearch",
							Children = [new ApiEntryChild { File = "op-search.md" }]
						}
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile, extraMarkdownFiles: ["op-search.md"]);

		collector.Errors.Should().Be(1);
		config.ApiConfigurations!["elasticsearch"].Children.Should().BeEmpty();
	}

	[Fact]
	public void AcceptsNestedChildWhoseBasenameLooksSupplemental()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry
						{
							Spec = "elasticsearch-openapi.json",
							Product = "elasticsearch",
							Children = [new ApiEntryChild { File = "guides/op-overview.md" }]
						}
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile, extraMarkdownFiles: ["guides/op-overview.md"]);

		collector.Errors.Should().Be(0);
		config.ApiConfigurations!["elasticsearch"].Children.Should().ContainSingle(f => f.Name == "op-overview.md");
	}

	[Fact]
	public void GetMarkdownPathsToExclude_IncludesChildrenAndSupplementalFiles()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries =
					[
						new ApiProductEntry
						{
							Spec = "elasticsearch-openapi.json",
							Product = "elasticsearch",
							Children = [new ApiEntryChild { File = "getting-started.md" }]
						}
					]
				}
			}
		};

		var (config, collector) = CreateConfiguration(
			docSetFile,
			extraMarkdownFiles: ["op-search.md", "tag-documents.md", "random-notes.md"]
		);

		collector.Errors.Should().Be(0);
		var docsRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs");
		var excluded = config.ApiConfigurations!["elasticsearch"].GetMarkdownPathsToExclude(docsRoot).ToArray();

		excluded.Should().Contain("api/elasticsearch/getting-started.md");
		excluded.Should().Contain("api/elasticsearch/op-search.md");
		excluded.Should().Contain("api/elasticsearch/tag-documents.md");
		excluded.Should().NotContain("api/elasticsearch/random-notes.md");
	}

	[Fact]
	public void ApiContentDirectory_IsSetToApiKeyFolder()
	{
		var docSetFile = new DocumentationSetFile
		{
			Api = new Dictionary<string, ApiProductSequence>
			{
				["elasticsearch"] = new()
				{
					Entries = [new ApiProductEntry { Spec = "elasticsearch-openapi.json", Product = "elasticsearch" }]
				}
			}
		};

		var (config, collector) = CreateConfiguration(docSetFile);

		collector.Errors.Should().Be(0);
		config.ApiConfigurations!["elasticsearch"].ApiContentDirectory.Should().NotBeNull();
		config.ApiConfigurations["elasticsearch"].ApiContentDirectory!.Name.Should().Be("elasticsearch");
	}

	private static readonly string[] DefaultProductIds = ["elasticsearch", "kibana"];

	private static (ConfigurationFile Config, DiagnosticsCollector Collector) CreateConfiguration(
		DocumentationSetFile docSet,
		string[]? extraProducts = null,
		bool withLocalSpecFile = true,
		string[]? extraMarkdownFiles = null
	)
	{
		var collector = new DiagnosticsCollector([]);
		var root = Paths.WorkingDirectoryRoot.FullName;
		var configFilePath = Path.Join(root, "docs", "_docset.yml");
		var files = new Dictionary<string, MockFileData>
		{
			{ configFilePath, new MockFileData("") },
			{ Path.Join(root, "docs", "api", "elasticsearch", "getting-started.md"), new MockFileData("# Getting started") },
			{ Path.Join(root, "outside.md"), new MockFileData("# Outside") }
		};
		if (withLocalSpecFile)
			files[Path.Join(root, "docs", "elasticsearch-openapi.json")] = new MockFileData("{}");
		foreach (var name in extraMarkdownFiles ?? [])
			files[Path.Join(root, "docs", "api", "elasticsearch", name)] = new MockFileData("# extra");
		var fileSystem = new MockFileSystem(files, root);

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(root, "docs"));

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDir);
		var versionsConfig = new VersionsConfiguration { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() };

		var productIds = DefaultProductIds.Concat(extraProducts ?? []);
		var products = productIds.ToDictionary(id => id, id => new Product { Id = id, DisplayName = id }, StringComparer.OrdinalIgnoreCase);
		var productsConfig = new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		var config = new ConfigurationFile(docSet, context, versionsConfig, productsConfig);
		return (config, collector);
	}

	private sealed class MockDocumentationSetContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IFileInfo configurationPath,
		IDirectoryInfo documentationSourceDirectory
	) : IDocumentationSetContext
	{
		public IDiagnosticsCollector Collector => collector;
		public IDocumentationFileSystem ReadFileSystem { get; } = DocumentationFileSystem.Resolve(
			documentationSourceDirectory,
			new DocumentationScopeOptions { Inner = fileSystem, ConfigurationFile = configurationPath.FullName }
		);
		public DocumentationWriteFileSystem WriteFileSystem { get; } = new(
			fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
			inner: fileSystem
		);
		public IDirectoryInfo OutputDirectory => fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts"));
		public IFileInfo ConfigurationPath => configurationPath;
		public BuildType BuildType => BuildType.Isolated;
		public IDirectoryInfo DocumentationSourceDirectory => documentationSourceDirectory;
		public GitCheckoutInformation Git => GitCheckoutInformationFactory.Create(documentationSourceDirectory, fileSystem);
		public IEnvironmentVariables Environment => SystemEnvironmentVariables.Instance;
	}
}
