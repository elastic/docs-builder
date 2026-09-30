// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Configuration.Tests;

public class ConfigurationFileExcludeTests
{
	[Test]
	public void IsExcluded_DocsetGlob_MatchesNestedKibanaDocsPath()
	{
		var docSet = new DocumentationSetDto
		{
			Project = "test",
			TableOfContents = [],
			Exclude = ["reference/query-languages/esql/kibana/docs/**"]
		};
		var config = CreateConfiguration(docSet);

		config.IsExcluded("reference/query-languages/esql/kibana/docs/functions/mv_slice.md").Should().BeTrue();
	}

	[Test]
	public void IsExcluded_DocsetGlob_DoesNotMatchOutsideTree()
	{
		var docSet = new DocumentationSetDto
		{
			Project = "test",
			TableOfContents = [],
			Exclude = ["reference/query-languages/esql/kibana/docs/**"]
		};
		var config = CreateConfiguration(docSet);

		config.IsExcluded("reference/query-languages/esql/guide.md").Should().BeFalse();
	}

	private static ResolvedDocumentationSet CreateConfiguration(DocumentationSetDto docSet)
	{
		var collector = new DiagnosticsCollector([]);
		var root = Paths.WorkingDirectoryRoot.FullName;
		var configFilePath = Path.Join(root, "docs", "_docset.yml");
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> { { configFilePath, new MockFileData("") } }, root);

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(root, "docs"));

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDir);
		var versionsConfig = new VersioningSystemCatalog { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() };
		var productsConfig = new ProductCatalog
		{
			Products = new Dictionary<string, Product>().ToFrozenDictionary(),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		return DocumentationSetResolver.Resolve(docSet, context, versionsConfig, productsConfig).EmitTo(context.Collector)!;
	}

	private sealed class MockDocumentationSetContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IFileInfo configurationPath,
		IDirectoryInfo documentationSourceDirectory
	) : IDocumentationSetContext
	{
		public IDiagnosticsCollector Collector => collector;
		public IDocumentationFileSystem ReadFileSystem { get; } = DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName);
		public DocumentationWriteFileSystem WriteFileSystem { get; } = new DocumentationWriteFileSystem(
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
