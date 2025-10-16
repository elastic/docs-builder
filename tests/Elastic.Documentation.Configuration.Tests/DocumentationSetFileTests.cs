// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class DocumentationSetFileTests
{
	// Tests use direct deserialization to test YAML parsing without TOC loading/resolution
	private DocumentationSetFile Deserialize(string yaml) =>
		ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

	[Fact]
	public void DeserializesBasicProperties()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           max_toc_depth: 3
		           dev_docs: true
		           cross_links:
		             - docs-content
		             - other-docs
		           exclude:
		             - '_*.md'
		             - '*.tmp'
		           """;

		var result = Deserialize(yaml);

		result.Project.Should().Be("test-project");
		result.MaxTocDepth.Should().Be(3);
		result.DevDocs.Should().BeTrue();
		result.CrossLinks.Should().HaveCount(2)
			.And.Contain("docs-content")
			.And.Contain("other-docs");
		result.Exclude.Should().HaveCount(2)
			.And.Contain("_*.md")
			.And.Contain("*.tmp");
	}

	[Fact]
	public void DeserializesSubstitutions()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           subs:
		             stack: Elastic Stack
		             ecloud: Elastic Cloud
		             dbuild: docs-builder
		           """;

		var result = Deserialize(yaml);

		result.Subs.Should().HaveCount(3)
			.And.ContainKey("stack").WhoseValue.Should().Be("Elastic Stack");
		result.Subs.Should().ContainKey("ecloud").WhoseValue.Should().Be("Elastic Cloud");
		result.Subs.Should().ContainKey("dbuild").WhoseValue.Should().Be("docs-builder");
	}

	[Fact]
	public void DeserializesFeatures()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           features:
		             primary-nav: false
		           """;

		var result = Deserialize(yaml);

		result.Features.Should().NotBeNull();
		result.Features.PrimaryNav.Should().BeFalse();
	}

	[Fact]
	public void DeserializesApiConfiguration()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           api:
		             elasticsearch: elasticsearch-openapi.json
		             kibana: kibana-openapi.json
		           """;

		var result = Deserialize(yaml);

		result.Api.Should().HaveCount(2)
			.And.ContainKey("elasticsearch").WhoseValue.Should().Be("elasticsearch-openapi.json");
		result.Api.Should().ContainKey("kibana").WhoseValue.Should().Be("kibana-openapi.json");
	}

	[Fact]
	public void DeserializesFileReference()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: getting-started.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(2);
		result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("index.md");
		result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("getting-started.md");
	}

	[Fact]
	public void DeserializesHiddenFileReference()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - hidden: 404.md
		             - hidden: developer-notes.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(3);
		result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Hidden.Should().BeFalse();
		result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Hidden.Should().BeTrue();
		result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("404.md");
		result.TableOfContents.ElementAt(2).Should().BeOfType<FileRef>()
			.Which.Hidden.Should().BeTrue();
	}

	[Fact]
	public void DeserializesFolderReference()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: contribute
		               children:
		                 - file: index.md
		                 - file: locally.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(1);
		var folder = result.TableOfContents.ElementAt(0).Should().BeOfType<FolderRef>().Subject;
		folder.Path.Should().Be("contribute");
		folder.Children.Should().HaveCount(2);
		folder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("index.md");
		folder.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("locally.md");
	}

	[Fact]
	public void DeserializesTocReference()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: development
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(2);
		result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>();
		result.TableOfContents.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>()
			.Which.Path.Should().Be("development");
	}

	[Fact]
	public void DeserializesCrossLinkReference()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: cross-links.md
		               children:
		                 - title: "Getting Started Guide"
		                   crosslink: docs-content://get-started/introduction.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(2);
		var fileWithChildren = result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		fileWithChildren.Children.Should().HaveCount(1);
		var crosslink = fileWithChildren.Children.ElementAt(0).Should().BeOfType<CrossLinkRef>().Subject;
		crosslink.Title.Should().Be("Getting Started Guide");
		crosslink.CrossLinkUri.ToString().Should().Be("docs-content://get-started/introduction.md");
	}

	[Fact]
	public void DeserializesNestedStructure()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: configure
		               children:
		                 - file: index.md
		                 - folder: site
		                   children:
		                     - file: index.md
		                     - file: content.md
		                     - file: navigation.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(1);
		var topFolder = result.TableOfContents.ElementAt(0).Should().BeOfType<FolderRef>().Subject;
		topFolder.Path.Should().Be("configure");
		topFolder.Children.Should().HaveCount(2);

		topFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("index.md");

		var nestedFolder = topFolder.Children.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		nestedFolder.Path.Should().Be("site");
		nestedFolder.Children.Should().HaveCount(3);
		nestedFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("index.md");
		nestedFolder.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("content.md");
		nestedFolder.Children.ElementAt(2).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("navigation.md");
	}

	[Fact]
	public void DeserializesCompleteDocsetYaml()
	{
		// language=yaml
		var yaml = """
		           project: 'doc-builder'
		           max_toc_depth: 2
		           dev_docs: true
		           cross_links:
		             - docs-content
		           exclude:
		             - '_*.md'
		           subs:
		             stack: Elastic Stack
		             serverless-short: Serverless
		             ecloud: Elastic Cloud
		           features:
		             primary-nav: false
		           api:
		             elasticsearch: elasticsearch-openapi.json
		             kibana: kibana-openapi.json
		           toc:
		             - file: index.md
		             - hidden: 404.md
		             - folder: configure
		               children:
		                 - file: index.md
		                 - folder: site
		                   children:
		                     - file: index.md
		                     - file: content.md
		                     - file: navigation.md
		                 - file: page.md
		                   children:
		                     - title: "Getting Started Guide"
		                       crosslink: docs-content://get-started/introduction.md
		             - toc: development
		           """;

		var result = Deserialize(yaml);

		// Assert top-level docset properties
		result.Project.Should().Be("doc-builder");
		result.MaxTocDepth.Should().Be(2);
		result.DevDocs.Should().BeTrue();
		result.CrossLinks.Should().ContainSingle().Which.Should().Be("docs-content");
		result.Exclude.Should().ContainSingle().Which.Should().Be("_*.md");
		result.Subs.Should().HaveCount(3);
		result.Features.PrimaryNav.Should().BeFalse();
		result.Api.Should().HaveCount(2);

		// Assert TOC structure - 4 root items
		result.TableOfContents.Should().HaveCount(4);

		// First item: simple file reference
		var firstItem = result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		firstItem.Path.Should().Be("index.md");
		firstItem.Hidden.Should().BeFalse();
		firstItem.Children.Should().BeEmpty();

		// Second item: hidden file reference
		var secondItem = result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		secondItem.Path.Should().Be("404.md");
		secondItem.Hidden.Should().BeTrue();
		secondItem.Children.Should().BeEmpty();

		// Third item: folder with a deeply nested structure
		var configureFolder = result.TableOfContents.ElementAt(2).Should().BeOfType<FolderRef>().Subject;
		configureFolder.Path.Should().Be("configure");
		configureFolder.Children.Should().HaveCount(3);

		// First child: file reference
		var configureIndexFile = configureFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		configureIndexFile.Path.Should().Be("index.md");
		configureIndexFile.Hidden.Should().BeFalse();

		// Second child: nested folder with 3 files
		var siteFolder = configureFolder.Children.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		siteFolder.Path.Should().Be("site");
		siteFolder.Children.Should().HaveCount(3);

		// Assert nested folder's children
		var siteIndexFile = siteFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		siteIndexFile.Path.Should().Be("index.md");

		var contentFile = siteFolder.Children.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		contentFile.Path.Should().Be("content.md");

		var navigationFile = siteFolder.Children.ElementAt(2).Should().BeOfType<FileRef>().Subject;
		navigationFile.Path.Should().Be("navigation.md");

		// Third child: file with crosslink child
		var pageFile = configureFolder.Children.ElementAt(2).Should().BeOfType<FileRef>().Subject;
		pageFile.Path.Should().Be("page.md");
		pageFile.Children.Should().HaveCount(1);

		// Assert crosslink reference as a child of page.md
		var crosslink = pageFile.Children.ElementAt(0).Should().BeOfType<CrossLinkRef>().Subject;
		crosslink.Title.Should().Be("Getting Started Guide");
		crosslink.CrossLinkUri.ToString().Should().Be("docs-content://get-started/introduction.md");
		crosslink.Hidden.Should().BeFalse();
		crosslink.Children.Should().BeEmpty();

		// Fourth item: toc reference
		var tocRef = result.TableOfContents.ElementAt(3).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		tocRef.Path.Should().Be("development");
		tocRef.Children.Should().BeEmpty();
	}

	[Fact]
	public void DeserializesFileWithChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: chapter1.md
		                 - file: chapter2.md
		                 - file: chapter3.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(1);
		var guide = result.TableOfContents.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		guide.Path.Should().Be("guide.md");
		guide.Children.Should().HaveCount(3);
		guide.Children.ElementAt(0).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("chapter1.md");
		guide.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("chapter2.md");
		guide.Children.ElementAt(2).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("chapter3.md");
	}

	[Fact]
	public void DeserializesFileWithNestedPathsAsChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: api/guide.md
		               children:
		                 - file: api/section1.md
		                 - file: api/section2.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(1);
		var guide = result.TableOfContents.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		guide.Path.Should().Be("api/guide.md");
		guide.Children.Should().HaveCount(2);
		guide.Children.ElementAt(0).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("api/section1.md");
		guide.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("api/section2.md");
	}

	[Fact]
	public void DeserializesDefaultValues()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		           """;

		var result = Deserialize(yaml);

		result.MaxTocDepth.Should().Be(2); // Default value
		result.DevDocs.Should().BeFalse(); // Default value
		result.CrossLinks.Should().BeEmpty();
		result.Exclude.Should().BeEmpty();
		result.Subs.Should().BeEmpty();
		result.Api.Should().BeEmpty();
		result.Features.PrimaryNav.Should().BeNull();
	}

	[Fact]
	public void DeserializesEmptyToc()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc: []
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().BeEmpty();
	}

	[Fact]
	public void DeserializesCrossLinkWithoutTitle()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		               children:
		                 - crosslink: docs-content://get-started.md
		           """;

		var result = Deserialize(yaml);

		var file = result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		var crosslink = file.Children.ElementAt(0).Should().BeOfType<CrossLinkRef>().Subject;
		crosslink.CrossLinkUri.ToString().Should().Be("docs-content://get-started.md/"); // URI normalization adds trailing slash
		crosslink.Title.Should().BeNull();
	}

	[Fact]
	public void DeserializesMixedHiddenAndVisibleItems()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - hidden: _internal.md
		             - file: public.md
		             - hidden: _draft.md
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(4);
		result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Hidden.Should().BeFalse();
		result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Hidden.Should().BeTrue();
		result.TableOfContents.ElementAt(2).Should().BeOfType<FileRef>()
			.Which.Hidden.Should().BeFalse();
		result.TableOfContents.ElementAt(3).Should().BeOfType<FileRef>()
			.Which.Hidden.Should().BeTrue();
	}

	[Fact]
	public void DeserializesDeeplyNestedFileWithChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: chapter1.md
		                   children:
		                     - file: section1.md
		                       children:
		                         - file: subsection1.md
		           """;

		var result = Deserialize(yaml);

		var guide = result.TableOfContents.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		var chapter1 = guide.Children.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		var section1 = chapter1.Children.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		var subsection1 = section1.Children.ElementAt(0).Should().BeOfType<FileRef>().Subject;

		subsection1.Path.Should().Be("subsection1.md");
		subsection1.Children.Should().BeEmpty();
	}

	[Fact]
	public void DeserializesMultipleExcludePatterns()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           exclude:
		             - '_*.md'
		             - '*.tmp'
		             - '*.draft'
		             - '.DS_Store'
		             - 'node_modules/**'
		           toc:
		             - file: index.md
		           """;

		var result = Deserialize(yaml);

		result.Exclude.Should().HaveCount(5)
			.And.ContainInOrder("_*.md", "*.tmp", "*.draft", ".DS_Store", "node_modules/**");
	}

	[Fact]
	public void DeserializesMultipleCrossLinks()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           cross_links:
		             - elasticsearch
		             - kibana
		             - docs-content
		             - cloud
		           toc:
		             - file: index.md
		           """;

		var result = Deserialize(yaml);

		result.CrossLinks.Should().HaveCount(4)
			.And.ContainInOrder("elasticsearch", "kibana", "docs-content", "cloud");
	}

	[Fact]
	public void DeserializesFolderWithMixedChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api
		               children:
		                 - file: index.md
		                 - folder: rest
		                   children:
		                     - file: index.md
		                 - file: overview.md
		           """;

		var result = Deserialize(yaml);

		var apiFolder = result.TableOfContents.ElementAt(0).Should().BeOfType<FolderRef>().Subject;
		apiFolder.Children.Should().HaveCount(3);
		apiFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>();
		apiFolder.Children.ElementAt(1).Should().BeOfType<FolderRef>();
		apiFolder.Children.ElementAt(2).Should().BeOfType<FileRef>();
	}

	[Fact]
	public void LoadAndResolveResolvesIsolatedTocReferences()
	{
		// Create a mock file system with docset and nested TOC files
		var fileSystem = new MockFileSystem();

		// Main docset.yml
		// language=yaml
		var docsetYaml = """
		                 project: 'test-project'
		                 toc:
		                   - file: index.md
		                   - toc: development
		                   - folder: guides
		                     children:
		                       - file: getting-started.md
		                       - toc: advanced
		                 """;

		// development/toc.yml
		// language=yaml
		var developmentTocYaml = """
		                         toc:
		                           - file: index.md
		                           - file: contributing.md
		                           - folder: internals
		                             children:
		                               - file: architecture.md
		                         """;

		// guides/advanced/toc.yml
		// language=yaml
		var advancedTocYaml = """
		                      toc:
		                        - file: index.md
		                        - file: patterns.md
		                      """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/development/toc.yml", new MockFileData(developmentTocYaml));
		fileSystem.AddFile("/docs/guides/advanced/toc.yml", new MockFileData(advancedTocYaml));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var result = DocumentationSetFile.LoadAndResolve(docsetPath, fileSystem);

		// Verify TOC references have been preserved (not flattened)
		// We have 3 top-level items: index.md, development TOC, and guides folder
		result.TableOfContents.Should().HaveCount(3);

		// First item: file from main docset
		result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("index.md");

		// Second item: development TOC (preserved as IsolatedTableOfContentsRef with resolved children)
		var developmentToc = result.TableOfContents.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		developmentToc.Path.Should().Be("development");
		developmentToc.Children.Should().HaveCount(3, "should have index, contributing file, and internals folder");

		developmentToc.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("development/index.md", "TOC path should be prepended");

		developmentToc.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("development/contributing.md");

		var internalsFolder = developmentToc.Children.ElementAt(2).Should().BeOfType<FolderRef>().Subject;
		internalsFolder.Path.Should().Be("development/internals");
		internalsFolder.Children.Should().HaveCount(1);
		internalsFolder.Children.ElementAt(0).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("development/internals/architecture.md");

		// Third item: guides folder (preserved with its children including nested advanced TOC)
		var guidesFolder = result.TableOfContents.ElementAt(2).Should().BeOfType<FolderRef>().Subject;
		guidesFolder.Path.Should().Be("guides");
		guidesFolder.Children.Should().HaveCount(2, "should have getting-started file and advanced TOC");

		guidesFolder.Children.ElementAt(0).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("guides/getting-started.md");

		// Advanced TOC preserved as IsolatedTableOfContentsRef within guides folder
		var advancedToc = guidesFolder.Children.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		advancedToc.Path.Should().Be("advanced");
		advancedToc.Children.Should().HaveCount(2);

		advancedToc.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("guides/advanced/index.md");

		advancedToc.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("guides/advanced/patterns.md");
	}

	[Fact]
	public void LoadAndResolvePrependsParentPathsToFileReferences()
	{
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml = """
		                 project: 'test-project'
		                 toc:
		                   - file: guide.md
		                     children:
		                       - file: chapter1.md
		                         children:
		                           - file: section1.md
		                   - folder: api
		                     children:
		                       - file: index.md
		                       - file: reference.md
		                 """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var result = DocumentationSetFile.LoadAndResolve(docsetPath, fileSystem);

		result.TableOfContents.Should().HaveCount(2);

		// First item: file with nested children
		var guide = result.TableOfContents.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		guide.Path.Should().Be("guide.md");
		guide.Children.Should().HaveCount(1);

		var chapter1 = guide.Children.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		chapter1.Path.Should().Be("guide/chapter1.md", "parent path 'guide' should be prepended");
		chapter1.Children.Should().HaveCount(1);

		var section1 = chapter1.Children.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		section1.Path.Should().Be("guide/chapter1/section1.md", "full parent path 'guide/chapter1' should be prepended");

		// Second item: folder with children
		var apiFolder = result.TableOfContents.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		apiFolder.Path.Should().Be("api");
		apiFolder.Children.Should().HaveCount(2);

		apiFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Path.Should().Be("api/index.md", "folder path 'api' should be prepended");

		apiFolder.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Path.Should().Be("api/reference.md", "folder path 'api' should be prepended");
	}

	[Fact]
	public void LoadAndResolveSetsContextForAllItems()
	{
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml = """
		                 project: 'test-project'
		                 toc:
		                   - file: index.md
		                   - folder: guides
		                     children:
		                       - file: getting-started.md
		                   - toc: development
		                 """;

		// development/toc.yml
		// language=yaml
		var developmentTocYaml = """
		                         toc:
		                           - file: contributing.md
		                         """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/development/toc.yml", new MockFileData(developmentTocYaml));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var result = DocumentationSetFile.LoadAndResolve(docsetPath, fileSystem);

		// All items from docset.yml should have context = /docs/docset.yml
		result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Context.Should().Be("/docs/docset.yml");

		var guidesFolder = result.TableOfContents.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		guidesFolder.Context.Should().Be("/docs/docset.yml");
		guidesFolder.Children.ElementAt(0).Should().BeOfType<FileRef>()
			.Which.Context.Should().Be("/docs/docset.yml");

		// The TOC ref itself has context = /docs/docset.yml (where it was referenced)
		var developmentToc = result.TableOfContents.ElementAt(2).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		developmentToc.Context.Should().Be("/docs/docset.yml");

		// But children of the TOC ref should have context = /docs/development/toc.yml (where they were defined)
		developmentToc.Children.ElementAt(0).Should().BeOfType<FileRef>()
			.Which.Context.Should().Be("/docs/development/toc.yml");
	}
}
