// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.CliReference;

namespace Elastic.Documentation.Configuration.Tests;

public class PhysicalDocsetTests
{
	[Fact]
	public void CliReferenceRefReadsTitleOverrides()
	{
		const string yaml = """
			project: test
			toc:
			  - cli: cli/schema.json
			    folder: cli
			    title: Elastic CLI reference
			    navigation_title: CLI reference
			""";

		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);
		var cliRef = docSet.TableOfContents.OfType<CliReferenceRef>().Single();

		cliRef.Title.Should().Be("Elastic CLI reference");
		cliRef.NavigationTitle.Should().Be("CLI reference");
	}

	[Fact]
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

		docSet.CrossLinks.Should().ContainSingle().Which.Should().Be("docs-content");
		docSet.Exclude.Should().ContainSingle().Which.Should().Be("_*.md");
		docSet.Subs.Should().NotBeEmpty();
		docSet.Subs.Should().ContainKey("dbuild").WhoseValue.Should().Be("docs-builder");

		docSet.Api.Should().BeNullOrEmpty("API declarations live in docs-content for assembler builds");

		docSet.TableOfContents.Should().NotBeEmpty();

		var firstItem = docSet.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		firstItem.PathRelativeToDocumentationSet.Should().Be("index.md");
		firstItem.Hidden.Should().BeFalse();

		var hiddenFiles = docSet.TableOfContents.OfType<FileRef>().Where(f => f.Hidden).ToList();
		hiddenFiles.Should().Contain(f => f.PathRelativeToDocumentationSet == "404.md");
		hiddenFiles.Should().Contain(f => f.PathRelativeToDocumentationSet == "developer-notes.md");

		docSet.TableOfContents.OfType<FolderRef>().Should().NotBeEmpty();

		var cliRef = docSet.TableOfContents.OfType<CliReferenceRef>().FirstOrDefault();
		cliRef.Should().NotBeNull();
	}

	[Fact]
	public void PhysicalDocsetContainsExpectedFolders()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var folderNames = docSet.TableOfContents.OfType<FolderRef>().Select(f => f.PathRelativeToDocumentationSet).ToList();

		folderNames.Should().Contain("getting-started");
		folderNames.Should().Contain("syntax");
		folderNames.Should().Contain("documentation");
		folderNames.Should().Contain("data");
		folderNames.Should().Contain("integrations");

		// development is a toc: reference, not a folder
		var tocRefs = docSet.TableOfContents.OfType<IsolatedTableOfContentsRef>().Select(t => t.PathRelativeToDocumentationSet).ToList();
		tocRefs.Should().Contain("development");

		var cliRef = docSet.TableOfContents.OfType<CliReferenceRef>().FirstOrDefault();
		cliRef.Should().NotBeNull();
	}

	[Fact]
	public void PhysicalDocsetHasValidNestedStructure()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var documentationFolder = docSet.TableOfContents.OfType<FolderRef>().First(f => f.PathRelativeToDocumentationSet == "documentation");
		documentationFolder.Children.Should().NotBeEmpty();

		var nestedFolders = documentationFolder.Children.OfType<FolderRef>().Select(f => f.PathRelativeToDocumentationSet).ToList();
		nestedFolders.Should().Contain("isolated");
		nestedFolders.Should().Contain("assembler");
		nestedFolders.Should().Contain("codex");
		nestedFolders.Should().Contain("catalog");

		var cliRef = docSet.TableOfContents.OfType<CliReferenceRef>().First();
		cliRef.Children.Should().BeEmpty();
	}

	[Fact]
	public void PhysicalTestDocsetContainsFileReferencesWithChildren()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs-tests", "docset.yml");
		File.Exists(docsetPath).Should().BeTrue($"Expected test docset file to exist at {docsetPath}");

		var yaml = File.ReadAllText(docsetPath);
		var docSet = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var fileWithChildren = docSet.TableOfContents.OfType<FileRef>()
			.FirstOrDefault(f => f.PathRelativeToDocumentationSet == "cross-links.md" && f.Children.Count > 0);

		fileWithChildren.Should().NotBeNull();
		fileWithChildren.Children.Should().NotBeEmpty();
		fileWithChildren.Children.Should().Contain(c => c is CrossLinkRef);
	}
}
