// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class DocumentationSetFileTests
{
	private DocumentationSetFile Deserialize(string yaml) => DocumentationSetFile.Deserialize(yaml);

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

		result.Toc.Should().HaveCount(2);
		result.Toc.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.RelativePath.Should().Be("index.md");
		result.Toc.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.RelativePath.Should().Be("getting-started.md");
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

		result.Toc.Should().HaveCount(3);
		result.Toc.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.Hidden.Should().BeFalse();
		result.Toc.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.Hidden.Should().BeTrue();
		result.Toc.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.RelativePath.Should().Be("404.md");
		result.Toc.ElementAt(2).Should().BeOfType<FileRef>()
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

		result.Toc.Should().HaveCount(1);
		var folder = result.Toc.ElementAt(0).Should().BeOfType<FolderRef>().Subject;
		folder.RelativePath.Should().Be("contribute");
		folder.Children.Should().HaveCount(2);
		folder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.RelativePath.Should().Be("index.md");
		folder.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.RelativePath.Should().Be("locally.md");
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

		result.Toc.Should().HaveCount(2);
		result.Toc.ElementAt(0).Should().BeOfType<IndexFileRef>();
		result.Toc.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>()
			.Which.Source.Should().Be("development");
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

		result.Toc.Should().HaveCount(2);
		var fileWithChildren = result.Toc.ElementAt(1).Should().BeOfType<FileRef>().Subject;
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

		result.Toc.Should().HaveCount(1);
		var topFolder = result.Toc.ElementAt(0).Should().BeOfType<FolderRef>().Subject;
		topFolder.RelativePath.Should().Be("configure");
		topFolder.Children.Should().HaveCount(2);

		topFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.RelativePath.Should().Be("index.md");

		var nestedFolder = topFolder.Children.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		nestedFolder.RelativePath.Should().Be("site");
		nestedFolder.Children.Should().HaveCount(3);
		nestedFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>()
			.Which.RelativePath.Should().Be("index.md");
		nestedFolder.Children.ElementAt(1).Should().BeOfType<FileRef>()
			.Which.RelativePath.Should().Be("content.md");
		nestedFolder.Children.ElementAt(2).Should().BeOfType<FileRef>()
			.Which.RelativePath.Should().Be("navigation.md");
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
		result.Toc.Should().HaveCount(4);

		// First item: simple file reference
		var firstItem = result.Toc.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		firstItem.RelativePath.Should().Be("index.md");
		firstItem.Hidden.Should().BeFalse();
		firstItem.Children.Should().BeEmpty();

		// Second item: hidden file reference
		var secondItem = result.Toc.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		secondItem.RelativePath.Should().Be("404.md");
		secondItem.Hidden.Should().BeTrue();
		secondItem.Children.Should().BeEmpty();

		// Third item: folder with a deeply nested structure
		var configureFolder = result.Toc.ElementAt(2).Should().BeOfType<FolderRef>().Subject;
		configureFolder.RelativePath.Should().Be("configure");
		configureFolder.Children.Should().HaveCount(3);

		// First child: file reference
		var configureIndexFile = configureFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		configureIndexFile.RelativePath.Should().Be("index.md");
		configureIndexFile.Hidden.Should().BeFalse();

		// Second child: nested folder with 3 files
		var siteFolder = configureFolder.Children.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		siteFolder.RelativePath.Should().Be("site");
		siteFolder.Children.Should().HaveCount(3);

		// Assert nested folder's children
		var siteIndexFile = siteFolder.Children.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		siteIndexFile.RelativePath.Should().Be("index.md");

		var contentFile = siteFolder.Children.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		contentFile.RelativePath.Should().Be("content.md");

		var navigationFile = siteFolder.Children.ElementAt(2).Should().BeOfType<FileRef>().Subject;
		navigationFile.RelativePath.Should().Be("navigation.md");

		// Third child: file with crosslink child
		var pageFile = configureFolder.Children.ElementAt(2).Should().BeOfType<FileRef>().Subject;
		pageFile.RelativePath.Should().Be("page.md");
		pageFile.Children.Should().HaveCount(1);

		// Assert crosslink reference as a child of page.md
		var crosslink = pageFile.Children.ElementAt(0).Should().BeOfType<CrossLinkRef>().Subject;
		crosslink.Title.Should().Be("Getting Started Guide");
		crosslink.CrossLinkUri.ToString().Should().Be("docs-content://get-started/introduction.md");
		crosslink.Hidden.Should().BeFalse();
		crosslink.Children.Should().BeEmpty();

		// Fourth item: toc reference
		var tocRef = result.Toc.ElementAt(3).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		tocRef.Source.Should().Be("development");
		tocRef.Children.Should().BeEmpty();
	}
}
