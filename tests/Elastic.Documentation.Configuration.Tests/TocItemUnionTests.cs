// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Tests;

public class TocItemUnionTests
{
	private static FileRef File(string path, params ITableOfContentsItem[] children) =>
		new(DocsetPath.From(path), ContainerPath.From(path), false, children, "docset.yml");

	private static FolderRef Folder(string path, params ITableOfContentsItem[] children) =>
		new(DocsetPath.From(path), ContainerPath.From(path), children, "docset.yml");

	[Test]
	public void From_FileSubtype_WrapsAsFileCase()
	{
		var index = new IndexFileRef(DocsetPath.From("a/index.md"), ContainerPath.From("a/index.md"), false, [], "docset.yml");

		TocItems.From(index)!.Value.Value.Should().BeSameAs(index);
	}

	[Test]
	public void From_InvalidEntry_IsNull() => TocItems.From(new InvalidTocItemRef("toc.yml")).Should().BeNull();

	[Test]
	public void GetFileRefs_NestedFolder_ListsFilesInDeclarationOrder()
	{
		var tree = Folder("a", File("a/one.md", File("a/two.md")), Folder("a/b", File("a/b/three.md")));

		DocumentationSetDto
			.GetFileRefs(tree)
			.Select(f => f.PathRelativeToDocumentationSet.Value)
			.Should()
			.Equal("a/one.md", "a/b/three.md");
	}

	[Test]
	public void GetFileRefs_ListingRef_ThrowsWithItsTypeName()
	{
		var listing = new ListingRef(DocsetPath.From("l"), ContainerPath.From("l"), [], "docset.yml", new ListingOptions());

		var act = () => DocumentationSetDto.GetFileRefs(listing);

		act.Should().Throw<Exception>().WithMessage("Unexpected item type ListingRef");
	}

	[Test]
	public void GetFileRefs_InvalidEntry_ThrowsWithItsTypeName()
	{
		var act = () => DocumentationSetDto.GetFileRefs(new InvalidTocItemRef("x"));

		act.Should().Throw<Exception>().WithMessage("Unexpected item type InvalidTocItemRef");
	}

	[Test]
	public void DocsetPath_EqualValues_AreEqual() => DocsetPath.From("a.md").Should().Be(DocsetPath.From("a.md"));
}
