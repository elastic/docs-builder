// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class CrossLinkRegistryTests
{
	[Fact]
	public void Registry_NullOrEmpty_ParsesAsPublic()
	{
		foreach (var config in new[] { null, "", "   ", "public" }.Select(v => CreateConfiguration(CreateDocSet(v, ["elasticsearch"]))))
			config.Registry.Should().Be(DocSetRegistry.Public);
	}

	[Fact]
	public void Registry_Internal_ParsesAsInternal()
	{
		var docSet = CreateDocSet("internal", ["docs-eng-team"]);
		var config = CreateConfiguration(docSet);

		config.Registry.Should().Be(DocSetRegistry.Internal);
	}

	[Fact]
	public void CrossLinkEntry_BareRepo_InheritsDocsetRegistry()
	{
		var docSet = CreateDocSet("internal", ["other-internal-repo"]);
		var config = CreateConfiguration(docSet);

		config.CrossLinkEntries.Should().ContainSingle()
			.Which.Should().Be(new CrossLinkEntry("other-internal-repo", DocSetRegistry.Internal));
	}

	[Fact]
	public void CrossLinkEntry_PublicPrefix_UsesPublicRegistry()
	{
		var docSet = CreateDocSet("internal", ["other-internal-repo", "public://elasticsearch"]);
		var config = CreateConfiguration(docSet);

		config.CrossLinkEntries.Should().HaveCount(2);
		config.CrossLinkEntries[0].Should().Be(new CrossLinkEntry("other-internal-repo", DocSetRegistry.Internal));
		config.CrossLinkEntries[1].Should().Be(new CrossLinkEntry("elasticsearch", DocSetRegistry.Public));
	}

	[Fact]
	public void CrossLinkEntry_PublicDocset_BareReposUsePublic()
	{
		var docSet = CreateDocSet(null, ["elasticsearch", "kibana"]);
		var config = CreateConfiguration(docSet);

		config.CrossLinkEntries.Should().HaveCount(2);
		config.CrossLinkEntries[0].Should().Be(new CrossLinkEntry("elasticsearch", DocSetRegistry.Public));
		config.CrossLinkEntries[1].Should().Be(new CrossLinkEntry("kibana", DocSetRegistry.Public));
	}

	[Fact]
	public void CrossLinkEntry_PublicDocset_InternalPrefix_ExcludesInvalidEntry()
	{
		var docSet = CreateDocSet(null, ["elasticsearch", "internal://docs-eng-team"]);
		var config = CreateConfiguration(docSet);

		// Public docsets cannot link to internal; the invalid entry is excluded
		config.CrossLinkEntries.Should().ContainSingle()
			.Which.Should().Be(new CrossLinkEntry("elasticsearch", DocSetRegistry.Public));
	}

	[Fact]
	public void CrossLinkRepositories_MatchesCrossLinkEntries()
	{
		var docSet = CreateDocSet("internal", ["repo-a", "public://repo-b"]);
		var config = CreateConfiguration(docSet);

		config.CrossLinkRepositories.Should().BeEquivalentTo(["repo-a", "repo-b"]);
	}

	private static DocumentationSetFile CreateDocSet(string? registry, IReadOnlyList<string> crossLinks)
	{
		var docSet = new DocumentationSetFile
		{
			Project = "test",
			CrossLinks = crossLinks.ToList(),
			Registry = registry,
			TableOfContents = []
		};
		return docSet;
	}

	private static ConfigurationFile CreateConfiguration(DocumentationSetFile docSet)
	{
		var collector = new DiagnosticsCollector([]);
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
		public IFileSystem ReadFileSystem => fileSystem;
		public IFileSystem WriteFileSystem => fileSystem;
		public IDirectoryInfo OutputDirectory => fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts"));
		public IFileInfo ConfigurationPath => configurationPath;
		public BuildType BuildType => BuildType.Isolated;
		public IDirectoryInfo DocumentationSourceDirectory => documentationSourceDirectory;
		public GitCheckoutInformation Git => GitCheckoutInformation.Create(documentationSourceDirectory, fileSystem);
	}
}
