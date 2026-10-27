// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class ListingGroupInferenceTests() : DocumentationSetNavigationTestBase()
{
	// language=yaml
	private const string Yaml =
		"""
		project: 'docs-builder'
		toc:
		  - file: index.md
		  - listing: rules
		    glob: '**/*.md'
		    visual: groups
		""";

	private ListingRef ResolveListing(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/rules/index.md", new MockFileData("# Rules"));
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, Yaml, fileSystem.NewDirInfo("docs"));
		return docSet.TableOfContents.OfType<ListingRef>().Should().ContainSingle().Subject;
	}

	private static IEnumerable<string> PagesIn(ListingRef listing, string groupKey) =>
		listing
			.Children
			.OfType<ListingGroupRef>()
			.Should()
			.ContainSingle(g => g.GroupKey == groupKey)
			.Subject
			.Children
			.OfType<FileRef>()
			.Where(f => f is not IndexFileRef)
			.Select(f => f.PathRelativeToDocumentationSet);

	private static IEnumerable<string> UngroupedPages(ListingRef listing) =>
		listing.Children.OfType<FileRef>().Where(f => f is not IndexFileRef).Select(f => f.PathRelativeToDocumentationSet);

	[Test]
	public void FileInGroupFolderWithoutFrontMatter_IsGroupedByItsFolder()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/rules/aws/index.md", new MockFileData("# AWS"));
		fileSystem.AddFile("/docs/rules/aws/rule1.md", new MockFileData("# Rule 1"));

		var listing = ResolveListing(fileSystem);

		PagesIn(listing, "aws").Should().Equal("rules/aws/rule1.md");
		UngroupedPages(listing).Should().BeEmpty();
	}

	[Test]
	public void ExplicitListingFrontMatter_WinsOverTheFolder()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/rules/aws/index.md", new MockFileData("# AWS"));
		fileSystem.AddFile("/docs/rules/gcp/index.md", new MockFileData("# GCP"));
		fileSystem.AddFile("/docs/rules/aws/rule1.md", new MockFileData("---\nlisting: gcp\n---\n# Rule 1"));

		var listing = ResolveListing(fileSystem);

		PagesIn(listing, "gcp").Should().Equal("rules/aws/rule1.md");
		listing.Children.OfType<ListingGroupRef>().Should().NotContain(g => g.GroupKey == "aws");
	}

	[Test]
	public void FileInFolderWithoutGroupIndex_StaysUngrouped()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/rules/misc/rule1.md", new MockFileData("# Rule 1"));

		var listing = ResolveListing(fileSystem);

		listing.Children.OfType<ListingGroupRef>().Should().BeEmpty();
		UngroupedPages(listing).Should().Equal("rules/misc/rule1.md");
	}
}
