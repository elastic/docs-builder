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
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

public class ConfigurationFileCtaTests
{
	[Fact]
	public void ResolveCta_FrontmatterId_TakesPrecedenceOverTocDefault()
	{
		var docSet = LoadDocSet(
			"""
			project: test
			cta:
			  observability:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
			  monitor-kubernetes:
			    button:
			      label: Monitor Kubernetes
			      url: https://example.com/kubernetes
			toc:
			  - toc: solutions/observability
			""",
			("""
			default_cta: observability
			toc:
			  - file: get-started/quickstart.md
			""", "solutions/observability/toc.yml"),
			("# Quickstart", "solutions/observability/get-started/quickstart.md")
		);

		var config = CreateConfiguration(docSet);
		var cta = config.ResolveCta("monitor-kubernetes", "solutions/observability/get-started/quickstart.md", out var warning);

		cta.Name.Should().Be("monitor-kubernetes");
		warning.Should().BeNull();
	}

	[Fact]
	public void ResolveCta_NoFrontmatter_UsesTocDefault()
	{
		var docSet = LoadDocSet(
			"""
			project: test
			cta:
			  observability:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
			toc:
			  - toc: solutions/observability
			""",
			("""
			default_cta: observability
			toc:
			  - file: apps/apm.md
			""", "solutions/observability/toc.yml"),
			("# APM", "solutions/observability/apps/apm.md")
		);

		var config = CreateConfiguration(docSet);
		var cta = config.ResolveCta(null, "solutions/observability/apps/apm.md", out var warning);

		cta.Name.Should().Be("observability");
		warning.Should().BeNull();
	}

	[Fact]
	public void ResolveCta_NoFrontmatterAndNoTocDefault_FallsBackToDefault()
	{
		var docSet = LoadDocSet(
			"""
			project: test
			cta:
			  observability:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
			toc:
			  - file: reference/query-languages/esql.md
			""",
			("# ES|QL", "reference/query-languages/esql.md")
		);

		var config = CreateConfiguration(docSet);
		var cta = config.ResolveCta(null, "reference/query-languages/esql.md", out var warning);

		cta.Name.Should().Be(Cta.DefaultName);
		warning.Should().BeNull();
	}

	[Fact]
	public void ResolveCta_NestedTocDefault_OverridesParentDefault()
	{
		var docSet = LoadDocSet(
			"""
			project: test
			cta:
			  observability:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
			  monitor-kubernetes:
			    button:
			      label: Monitor Kubernetes
			      url: https://example.com/kubernetes
			toc:
			  - toc: solutions/observability
			""",
			("""
			default_cta: observability
			toc:
			  - file: apps/apm.md
			  - toc: get-started
			""", "solutions/observability/toc.yml"),
			("""
			default_cta: monitor-kubernetes
			toc:
			  - file: quickstart.md
			""", "solutions/observability/get-started/toc.yml"),
			("# APM", "solutions/observability/apps/apm.md"),
			("# Quickstart", "solutions/observability/get-started/quickstart.md")
		);

		var config = CreateConfiguration(docSet);

		config.ResolveCta(null, "solutions/observability/get-started/quickstart.md", out _).Name.Should().Be("monitor-kubernetes");
		config.ResolveCta(null, "solutions/observability/apps/apm.md", out _).Name.Should().Be("observability");
	}

	[Fact]
	public void ResolveCta_UnknownFrontmatterId_WarnsAndFallsBackToTocDefault()
	{
		var docSet = LoadDocSet(
			"""
			project: test
			cta:
			  observability:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
			toc:
			  - toc: solutions/observability
			""",
			("""
			default_cta: observability
			toc:
			  - file: apps/apm.md
			""", "solutions/observability/toc.yml"),
			("# APM", "solutions/observability/apps/apm.md")
		);

		var config = CreateConfiguration(docSet);
		var cta = config.ResolveCta("does-not-exist", "solutions/observability/apps/apm.md", out var warning);

		cta.Name.Should().Be("observability");
		warning.Should().Contain("does-not-exist").And.Contain("ignored");
	}

	[Fact]
	public void ResolveCta_DocsetDefaultCta_AppliesToRootLevelPages()
	{
		var docSet = LoadDocSet(
			"""
			project: test
			default_cta: observability
			cta:
			  observability:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
			toc:
			  - file: index.md
			""",
			("# Home", "index.md")
		);

		var config = CreateConfiguration(docSet);
		var cta = config.ResolveCta(null, "index.md", out _);

		cta.Name.Should().Be("observability");
	}

	[Fact]
	public async Task LoadAndResolve_PageClaimedByTwoDefaults_EmitsError()
	{
		var recorder = new RecordingDiagnosticsOutput();
		var collector = new DiagnosticsCollector([recorder]);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		_ = LoadDocSet(
			collector,
			"""
			project: test
			cta:
			  observability:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
			  security:
			    button:
			      label: Get started free
			      url: https://cloud.elastic.co/serverless-registration?onboarding_token=security
			toc:
			  - toc: section-a
			  - toc: section-b
			""",
			("""
			default_cta: observability
			toc:
			  - file: ../shared/page.md
			""", "section-a/toc.yml"),
			("""
			default_cta: security
			toc:
			  - file: ../shared/page.md
			""", "section-b/toc.yml"),
			("# Shared", "shared/page.md")
		);

		await collector.StopAsync(TestContext.Current.CancellationToken);

		recorder
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("observability") && d.Message.Contains("security"));
	}

	[Fact]
	public async Task Constructor_UnknownTocDefaultCta_EmitsError()
	{
		var docSet = LoadDocSet(
			"""
			project: test
			toc:
			  - toc: solutions/observability
			""",
			("""
			default_cta: does-not-exist
			toc:
			  - file: apps/apm.md
			""", "solutions/observability/toc.yml"),
			("# APM", "solutions/observability/apps/apm.md")
		);

		var (_, diagnostics) = await CreateConfigurationWithDiagnostics(docSet);

		diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error).Which.Message.Should().Contain("does-not-exist");
	}

	private static DocumentationSetFile LoadDocSet(string docsetYaml, params (string Content, string Path)[] files)
	{
		var collector = new DiagnosticsCollector([]);
		return LoadDocSet(collector, docsetYaml, files);
	}

	private static DocumentationSetFile LoadDocSet(
		DiagnosticsCollector collector,
		string docsetYaml,
		params (string Content, string Path)[] files
	)
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), "/docs");
		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));

		foreach (var (content, path) in files)
		{
			var fullPath = $"/docs/{path}";
			var directory = fileSystem.Path.GetDirectoryName(fullPath);
			if (directory is not null)
				fileSystem.AddDirectory(directory);
			fileSystem.AddFile(fullPath, new MockFileData(content));
		}

		return DocumentationSetFile.LoadAndResolve(
			collector,
			docsetYaml,
			fileSystem.DirectoryInfo.New("/docs"),
			new ScopedFileSystem(fileSystem, "/docs")
		);
	}

	private static ConfigurationFile CreateConfiguration(DocumentationSetFile docSet)
	{
		var collector = new DiagnosticsCollector([]);
		return CreateConfiguration(docSet, collector);
	}

	private static async Task<(ConfigurationFile Config, IReadOnlyList<Diagnostic> Diagnostics)> CreateConfigurationWithDiagnostics(
		DocumentationSetFile docSet
	)
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
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> { { "/docs/docset.yml", new MockFileData("") } }, "/docs");

		var configPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var docsDir = fileSystem.DirectoryInfo.New("/docs");

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDir);
		var versionsConfig = new VersionsConfiguration { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() };
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
