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

public class ConfigurationFileCtaTests
{
	[Fact]
	public void ResolveCta_FrontmatterId_TakesPrecedenceOverPathScope()
	{
		var config = CreateConfiguration(DocSetWith(
			("observability", "solutions/observability"),
			("monitor-kubernetes", null)));

		var cta = config.ResolveCta("monitor-kubernetes", "solutions/observability/get-started/quickstart.md", out var warning);

		cta.Name.Should().Be("monitor-kubernetes");
		warning.Should().BeNull();
	}

	[Fact]
	public void ResolveCta_NoFrontmatter_UsesPathScope()
	{
		var config = CreateConfiguration(DocSetWith(("observability", "solutions/observability")));

		var cta = config.ResolveCta(null, "solutions/observability/apps/apm.md", out var warning);

		cta.Name.Should().Be("observability");
		warning.Should().BeNull();
	}

	[Fact]
	public void ResolveCta_NoFrontmatterAndNoScopeMatch_FallsBackToDefault()
	{
		var config = CreateConfiguration(DocSetWith(("observability", "solutions/observability")));

		var cta = config.ResolveCta(null, "reference/query-languages/esql.md", out var warning);

		cta.Name.Should().Be(Cta.DefaultName);
		warning.Should().BeNull();
	}

	[Fact]
	public void ResolveCta_PathScope_MatchesWholeSegmentsOnly()
	{
		var config = CreateConfiguration(DocSetWith(("observability", "solutions/observability")));

		var cta = config.ResolveCta(null, "solutions/observability-labs/index.md", out _);

		cta.Name.Should().Be(Cta.DefaultName);
	}

	[Fact]
	public void ResolveCta_OverlappingScopes_MostSpecificPrefixWins()
	{
		var config = CreateConfiguration(DocSetWith(
			("observability", "solutions/observability"),
			("monitor-kubernetes", "solutions/observability/get-started")));

		config.ResolveCta(null, "solutions/observability/get-started/quickstart.md", out _)
			.Name.Should().Be("monitor-kubernetes");
		config.ResolveCta(null, "solutions/observability/apps/apm.md", out _)
			.Name.Should().Be("observability");
	}

	[Fact]
	public void ResolveCta_UnknownFrontmatterId_WarnsAndFallsBackToPathScope()
	{
		var config = CreateConfiguration(DocSetWith(("observability", "solutions/observability")));

		var cta = config.ResolveCta("does-not-exist", "solutions/observability/apps/apm.md", out var warning);

		cta.Name.Should().Be("observability");
		warning.Should().Contain("does-not-exist").And.Contain("ignored");
	}

	[Fact]
	public void ResolveCta_PathScope_NormalizesSeparatorsAndSlashes()
	{
		var config = CreateConfiguration(DocSetWith(("observability", "/solutions/observability/")));

		var cta = config.ResolveCta(null, @"solutions\observability\apps\apm.md", out _);

		cta.Name.Should().Be("observability");
	}

	[Fact]
	public async Task Constructor_PathClaimedByTwoTemplates_EmitsError()
	{
		var docSet = DocSetWith(
			("observability", "solutions/observability"),
			("security", "solutions/observability"));

		var (_, diagnostics) = await CreateConfigurationWithDiagnostics(docSet);

		diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error)
			.Which.Message.Should().Contain("already claimed by 'cta.observability'");
	}

	[Fact]
	public async Task Constructor_EmptyPath_EmitsError()
	{
		var docSet = DocSetWith(("observability", "  "));

		var (_, diagnostics) = await CreateConfigurationWithDiagnostics(docSet);

		diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error)
			.Which.Message.Should().Contain("empty path");
	}

	private static DocumentationSetFile DocSetWith(params (string Name, string? Path)[] templates)
	{
		var cta = new Dictionary<string, CtaDefinition>();
		foreach (var (name, path) in templates)
		{
			cta[name] = new CtaDefinition
			{
				Button = new CtaButton { Label = "Get started free", Url = $"https://cloud.elastic.co/serverless-registration?onboarding_token={name}" },
				Paths = path is null ? [] : [path]
			};
		}
		return new DocumentationSetFile
		{
			Project = "test",
			TableOfContents = [],
			Cta = cta
		};
	}

	private static ConfigurationFile CreateConfiguration(DocumentationSetFile docSet)
	{
		var collector = new DiagnosticsCollector([]);
		return CreateConfiguration(docSet, collector);
	}

	private static async Task<(ConfigurationFile Config, IReadOnlyList<Diagnostic> Diagnostics)> CreateConfigurationWithDiagnostics(DocumentationSetFile docSet)
	{
		var recorder = new RecordingDiagnosticsOutput();
		var collector = new DiagnosticsCollector([recorder]);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var config = CreateConfiguration(docSet, collector);
		await collector.StopAsync(TestContext.Current.CancellationToken);
		return (config, recorder.Diagnostics);
	}

	private static ConfigurationFile CreateConfiguration(DocumentationSetFile docSet, DiagnosticsCollector collector)
	{
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
		var productsConfig = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>().ToFrozenDictionary(),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		return new ConfigurationFile(docSet, context, versionsConfig, productsConfig);
	}

	private sealed class RecordingDiagnosticsOutput : IDiagnosticsOutput
	{
		public List<Diagnostic> Diagnostics { get; } = [];
		public void Write(Diagnostic diagnostic) => Diagnostics.Add(diagnostic);
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
		public IEnvironmentVariables Environment => SystemEnvironmentVariables.Instance;
	}
}
