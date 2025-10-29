// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class PhysicalDocsetTests
{
	[Fact]
	public void PhysicalDocsetFileCanBeDeserialized()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		File.Exists(docsetPath).Should().BeTrue($"Expected docset file to exist at {docsetPath}");

		var yaml = File.ReadAllText(docsetPath);
		// Tests use direct deserialization to test YAML parsing without TOC loading/resolution
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		// Assert basic properties
		docSet.Project.Should().Be("doc-builder");
		docSet.MaxTocDepth.Should().Be(2);
		docSet.DevDocs.Should().BeTrue();
		docSet.Features.PrimaryNav.Should().BeFalse();

		// Assert cross links
		docSet.CrossLinks.Should().ContainSingle().Which.Should().Be("docs-content");

		// Assert exclude patterns
		docSet.Exclude.Should().ContainSingle().Which.Should().Be("_*.md");

		// Assert substitutions
		docSet.Subs.Should().NotBeEmpty();
		docSet.Subs.Should().ContainKey("stack").WhoseValue.Should().Be("Elastic Stack");
		docSet.Subs.Should().ContainKey("dbuild").WhoseValue.Should().Be("docs-builder");

		// Assert API configuration
		docSet.Api.Should().HaveCount(2);
		docSet.Api.Should().ContainKey("elasticsearch").WhoseValue.Should().Be("elasticsearch-openapi.json");
		docSet.Api.Should().ContainKey("kibana").WhoseValue.Should().Be("kibana-openapi.json");

		// Assert TOC structure
		docSet.TableOfContents.Should().NotBeEmpty();

		// First item should be index.md
		var firstItem = docSet.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		firstItem.PathRelativeToDocumentationSet.Should().Be("index.md");
		firstItem.Hidden.Should().BeFalse();

		// Should have hidden files (404.md, developer-notes.md)
		var hiddenFiles = docSet.TableOfContents.OfType<FileRef>().Where(f => f.Hidden).ToList();
		hiddenFiles.Should().Contain(f => f.PathRelativeToDocumentationSet == "404.md");
		hiddenFiles.Should().Contain(f => f.PathRelativeToDocumentationSet == "developer-notes.md");

		// Should have folders
		docSet.TableOfContents.OfType<FolderRef>().Should().NotBeEmpty();
		var contributeFolder = docSet.TableOfContents.OfType<FolderRef>().FirstOrDefault(f => f.PathRelativeToDocumentationSet == "contribute");
		contributeFolder.Should().NotBeNull();
		contributeFolder.Children.Should().NotBeEmpty();

		// Should have TOC references
		var tocRefs = docSet.TableOfContents.OfType<IsolatedTableOfContentsRef>().ToList();
		tocRefs.Should().NotBeEmpty();
		tocRefs.Should().Contain(toc => toc.PathRelativeToDocumentationSet == "development");

		// Should have deeply nested structures
		var testingFolder = docSet.TableOfContents.OfType<FolderRef>().FirstOrDefault(f => f.PathRelativeToDocumentationSet == "testing");
		testingFolder.Should().NotBeNull();
		testingFolder.Children.Should().NotBeEmpty();
	}

	[Fact]
	public void PhysicalDocsetContainsExpectedFolders()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		// Tests use direct deserialization to test YAML parsing without TOC loading/resolution
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var folderNames = docSet.TableOfContents.OfType<FolderRef>().Select(f => f.PathRelativeToDocumentationSet).ToList();

		// Assert expected folders exist
		folderNames.Should().Contain("contribute");
		folderNames.Should().Contain("building-blocks");
		folderNames.Should().Contain("configure");
		folderNames.Should().Contain("syntax");
		folderNames.Should().Contain("cli");
		folderNames.Should().Contain("migration");
		folderNames.Should().Contain("testing");
	}

	[Fact]
	public void PhysicalDocsetHasValidNestedStructure()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		// Tests use direct deserialization to test YAML parsing without TOC loading/resolution
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		// Test the configure folder has nested folders
		var configureFolder = docSet.TableOfContents.OfType<FolderRef>().First(f => f.PathRelativeToDocumentationSet == "configure");
		configureFolder.Children.Should().NotBeEmpty();

		// Should have site and content-set folders
		var nestedFolders = configureFolder.Children.OfType<FolderRef>().Select(f => f.PathRelativeToDocumentationSet).ToList();
		nestedFolders.Should().Contain("site");
		nestedFolders.Should().Contain("content-set");

		// Test the cli folder has nested folders
		var cliFolder = docSet.TableOfContents.OfType<FolderRef>().First(f => f.PathRelativeToDocumentationSet == "cli");
		var cliNestedFolders = cliFolder.Children.OfType<FolderRef>().Select(f => f.PathRelativeToDocumentationSet).ToList();
		cliNestedFolders.Should().Contain("docset");
		cliNestedFolders.Should().Contain("assembler");
		cliNestedFolders.Should().Contain("links");
	}

	[Fact]
	public void PhysicalDocsetContainsFileReferencesWithChildren()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		// Tests use direct deserialization to test YAML parsing without TOC loading/resolution
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		// Find testing folder
		var testingFolder = docSet.TableOfContents.OfType<FolderRef>().First(f => f.PathRelativeToDocumentationSet == "testing");

		// Look for file with children (cross-links.md with crosslink children)
		var fileWithChildren = testingFolder.Children.OfType<FileRef>()
			.FirstOrDefault(f => f.PathRelativeToDocumentationSet == "cross-links.md" && f.Children.Count > 0);

		fileWithChildren.Should().NotBeNull();
		fileWithChildren.Children.Should().NotBeEmpty();
		fileWithChildren.Children.Should().Contain(c => c is CrossLinkRef);
	}
}
