// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Configuration.Tests;

public class PhysicalDocsetTests
{
	[Test]
	public void CliReferenceRefReadsTitleOverrides()
	{
		const string yaml =
			"""
			project: test
			toc:
			  - cli: cli/schema.json
			    folder: cli
			    title: Elastic CLI reference
			    navigation_title: CLI reference
			""";

		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);
		var cliRef = docSet.TocEntries.OfType<CliReferenceEntry>().Single();

		cliRef.Title.Should().Be("Elastic CLI reference");
		cliRef.NavigationTitle.Should().Be("CLI reference");
	}

	[Test]
	public void PhysicalDocsetFileCanBeDeserialized()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		File.Exists(docsetPath).Should().BeTrue($"Expected docset file to exist at {docsetPath}");

		var yaml = File.ReadAllText(docsetPath);
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		docSet.Project.Should().Be("doc-builder");
		docSet.MaxTocDepth.Should().Be(2);
		docSet.DevDocs.Should().BeTrue();
		docSet.Features.PrimaryNav.Should().BeFalse();

		docSet.CrossLinks.Should().Equal("docs-content", "elasticsearch");
		docSet.Exclude.Should().ContainSingle().Which.Should().Be("_*.md");
		docSet.Subs.Should().NotBeEmpty();
		docSet.Subs.Should().ContainKey("dbuild").WhoseValue.Should().Be("docs-builder");

		docSet.Api.Should().ContainKey("docs-builder-elasticsearch");
		var apiEntry = docSet.Api["docs-builder-elasticsearch"].SingleEntry;
		apiEntry.Should().NotBeNull();
		apiEntry.Spec.Should().Be("elasticsearch.json");
		apiEntry.Product.Should().Be("elasticsearch");
		apiEntry.Repository.Should().Be("elastic/elasticsearch-specification");

		docSet.TocEntries.Should().NotBeEmpty();

		var firstItem = docSet.TocEntries.ElementAt(0).Should().BeOfType<IndexFileEntry>().Subject;
		firstItem.Path.Should().Be("index.md");
		firstItem.Hidden.Should().BeFalse();

		var hiddenFiles = docSet.TocEntries.OfType<FileEntry>().Where(f => f.Hidden).ToList();
		hiddenFiles.Should().Contain(f => f.Path == "404.md");
		hiddenFiles.Should().Contain(f => f.Path == "developer-notes.md");

		docSet.TocEntries.OfType<FolderEntry>().Should().NotBeEmpty();

		var cliRef = docSet.TocEntries.OfType<CliReferenceEntry>().FirstOrDefault();
		cliRef.Should().NotBeNull();
	}

	[Test]
	public void PhysicalDocsetContainsExpectedFolders()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var folderNames = docSet.TocEntries.OfType<FolderEntry>().Select(f => f.Path).ToList();

		folderNames.Should().Contain("getting-started");
		folderNames.Should().Contain("syntax");
		folderNames.Should().Contain("documentation");
		folderNames.Should().Contain("data");
		folderNames.Should().Contain("integrations");

		// development is a toc: reference, not a folder
		var tocRefs = docSet.TocEntries.OfType<NestedTocEntry>().Select(t => t.Path).ToList();
		tocRefs.Should().Contain("development");

		var cliRef = docSet.TocEntries.OfType<CliReferenceEntry>().FirstOrDefault();
		cliRef.Should().NotBeNull();
	}

	[Test]
	public void PhysicalDocsetHasValidNestedStructure()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var documentationFolder = docSet.TocEntries.OfType<FolderEntry>().First(f => f.Path == "documentation");
		documentationFolder.Children.Should().NotBeEmpty();

		var nestedFolders = documentationFolder.Children.OfType<FolderEntry>().Select(f => f.Path).ToList();
		nestedFolders.Should().Contain("isolated");
		nestedFolders.Should().Contain("assembler");
		nestedFolders.Should().Contain("codex");
		nestedFolders.Should().Contain("catalog");

		var cliRef = docSet.TocEntries.OfType<CliReferenceEntry>().First();
		cliRef.Children.Should().BeEmpty();
	}

	[Test]
	public void PhysicalTestDocsetContainsFileReferencesWithChildren()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs-tests", "docset.yml");
		File.Exists(docsetPath).Should().BeTrue($"Expected test docset file to exist at {docsetPath}");

		var yaml = File.ReadAllText(docsetPath);
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var fileWithChildren = docSet
			.TocEntries
			.OfType<FileEntry>()
			.FirstOrDefault(f => f.Path == "cross-links.md" && f.Children.Count > 0);

		fileWithChildren.Should().NotBeNull();
		fileWithChildren.Children.Should().NotBeEmpty();
		fileWithChildren.Children.Should().Contain(c => c is CrossLinkTocEntry);
	}
}
