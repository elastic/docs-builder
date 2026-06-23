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
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

public class ConfigurationFileReleaseNotesTests
{
	[Fact]
	public async Task ReleaseNotes_DeclaredProduct_IsExposedWithoutErrors()
	{
		var (config, diagnostics) = await CreateConfiguration(DocSetWithReleaseNotes("elasticsearch"));

		config.ReleaseNotesProducts.Should().ContainSingle().Which.Should().Be("elasticsearch");
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ReleaseNotes_UnderscoreVariant_NormalizesToCanonicalId()
	{
		var (config, diagnostics) = await CreateConfiguration(DocSetWithReleaseNotes("edot_java"));

		config.ReleaseNotesProducts.Should().ContainSingle().Which.Should().Be("edot-java");
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ReleaseNotes_DuplicateDeclarations_AreDeduplicated()
	{
		var (config, diagnostics) = await CreateConfiguration(DocSetWithReleaseNotes("elasticsearch", "elasticsearch"));

		config.ReleaseNotesProducts.Should().ContainSingle().Which.Should().Be("elasticsearch");
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ReleaseNotes_UnknownProduct_EmitsError()
	{
		var (config, diagnostics) = await CreateConfiguration(DocSetWithReleaseNotes("not-a-product"));

		config.ReleaseNotesProducts.Should().BeEmpty();
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("Unknown 'release_notes' product"));
	}

	[Fact]
	public async Task ReleaseNotes_ProductWithoutReleaseNotesFeature_EmitsError()
	{
		var (config, diagnostics) = await CreateConfiguration(DocSetWithReleaseNotes("reference-only"));

		config.ReleaseNotesProducts.Should().BeEmpty();
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("does not participate"));
	}

	[Fact]
	public async Task ReleaseNotes_InvalidProductId_EmitsError()
	{
		var (config, diagnostics) = await CreateConfiguration(DocSetWithReleaseNotes("bad/slug"));

		config.ReleaseNotesProducts.Should().BeEmpty();
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("must match"));
	}

	[Fact]
	public async Task ReleaseNotes_EmptyProductValue_EmitsError()
	{
		var (config, diagnostics) = await CreateConfiguration(DocSetWithReleaseNotes("   "));

		config.ReleaseNotesProducts.Should().BeEmpty();
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("missing a 'product' value"));
	}

	private static DocumentationSetFile DocSetWithReleaseNotes(params string[] products) =>
		new()
		{
			Project = "test",
			TableOfContents = [],
			ReleaseNotes = [.. products.Select(p => new ReleaseNotesProductReference { Product = p })]
		};

	private static async Task<(ConfigurationFile Config, IReadOnlyList<Diagnostic> Diagnostics)> CreateConfiguration(DocumentationSetFile docSet)
	{
		var recorder = new RecordingDiagnosticsOutput();
		var collector = new DiagnosticsCollector([recorder]);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var root = Paths.WorkingDirectoryRoot.FullName;
		var configFilePath = Path.Join(root, "docs", "_docset.yml");
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configFilePath, new MockFileData("") }
		}, root);

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(root, "docs"));

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDir);
		var versionsConfig = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
		};
		var productsConfig = CreateProductsConfiguration();

		var config = new ConfigurationFile(docSet, context, versionsConfig, productsConfig);
		await collector.StopAsync(TestContext.Current.CancellationToken);
		return (config, recorder.Diagnostics);
	}

	private sealed class RecordingDiagnosticsOutput : IDiagnosticsOutput
	{
		public List<Diagnostic> Diagnostics { get; } = [];
		public void Write(Diagnostic diagnostic) => Diagnostics.Add(diagnostic);
	}

	private static ProductsConfiguration CreateProductsConfiguration()
	{
		var products = new Dictionary<string, Product>
		{
			["elasticsearch"] = new() { Id = "elasticsearch", DisplayName = "Elasticsearch", Features = ProductFeatures.All },
			["edot-java"] = new() { Id = "edot-java", DisplayName = "EDOT Java", Features = ProductFeatures.All },
			["reference-only"] = new()
			{
				Id = "reference-only",
				DisplayName = "Reference Only",
				Features = new ProductFeatures { PublicReference = true, ReleaseNotes = false }
			}
		};

		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			PublicReferenceProducts = products.ToFrozenDictionary(),
			ProductDisplayNames = products.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.DisplayName)
		};
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
		public GitCheckoutInformation Git => GitCheckoutInformationFactory.Create(documentationSourceDirectory, fileSystem);
	}
}
