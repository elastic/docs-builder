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

public class ConfigurationFileStorybookRegistryTests
{
	private const string Default = "https://ci-artifacts.kibana.dev/storybooks/main/storybook-docs/docs_registry.json";
	private const string Expression = $"${{KIBANA_STORYBOOK_REGISTRY:-{Default}}}";

	[Fact]
	public void UnsetVariable_ResolvesToCommittedDefault_WithNoFallback()
	{
		var config = CreateConfiguration(Expression, new MockEnvironment());

		config.StorybookRegistry.Should().Be(Default);
		config.StorybookRegistryFallback.Should().BeNull();
	}

	[Fact]
	public void SetVariable_ResolvesToEnvironmentValue_AndExposesDefaultAsFallback()
	{
		const string prRegistry = "https://ci-artifacts.kibana.dev/storybooks/pr-42/storybook-docs/docs_registry.json";
		var config = CreateConfiguration(Expression, new MockEnvironment { ["KIBANA_STORYBOOK_REGISTRY"] = prRegistry });

		config.StorybookRegistry.Should().Be(prRegistry);
		config.StorybookRegistryFallback.Should().Be(Default);
	}

	[Fact]
	public void DisallowedVariable_IsLeftLiteral_AndWarns()
	{
		var collector = new DiagnosticsCollector([]);
		var config = CreateConfiguration("${AWS_SECRET_ACCESS_KEY:-fallback}", new MockEnvironment { ["AWS_SECRET_ACCESS_KEY"] = "super-secret" }, collector);

		config.StorybookRegistry.Should().Be("${AWS_SECRET_ACCESS_KEY:-fallback}");
		config.StorybookRegistry.Should().NotContain("super-secret");
	}

	private static ConfigurationFile CreateConfiguration(string registry, IEnvironmentVariables environment, DiagnosticsCollector? collector = null)
	{
		collector ??= new DiagnosticsCollector([]);
		var root = Paths.WorkingDirectoryRoot.FullName;
		var configFilePath = Path.Join(root, "docs", "_docset.yml");
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configFilePath, new MockFileData("") }
		}, root);

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(root, "docs"));

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDir, environment);
		var versionsConfig = new VersionsConfiguration { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() };
		var productsConfig = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>().ToFrozenDictionary(),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		var docSet = new DocumentationSetFile
		{
			Project = "test",
			TableOfContents = [],
			Storybook = new DocumentationSetStorybook { Registry = registry }
		};

		return new ConfigurationFile(docSet, context, versionsConfig, productsConfig);
	}

	private sealed class MockEnvironment : IEnvironmentVariables
	{
		private readonly Dictionary<string, string?> _variables = [with(StringComparer.Ordinal)];

		public string? this[string name]
		{
			set => _variables[name] = value;
		}

		public string? GetEnvironmentVariable(string name) => _variables.GetValueOrDefault(name);

		public bool IsRunningOnCI => false;
	}

	private sealed class MockDocumentationSetContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IFileInfo configurationPath,
		IDirectoryInfo documentationSourceDirectory,
		IEnvironmentVariables environment)
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
		public IEnvironmentVariables Environment => environment;
	}
}
