// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// What a malformed <c>docset.yml</c>, <c>toc.yml</c> or <c>_redirects.yml</c> produces today: the diagnostics and the table of contents
/// that survives, or the exception. Results that look like bugs are recorded as they are.
/// </summary>
public class DocsetDiagnosticsGoldenTests
{
	private static string Toc(string toc, params DocFile[] files) => DocsetGolden.Toc($"project: cases\ntoc:\n{toc}", files);

	private static string Whole(string docset, params DocFile[] files) => DocsetGolden.Toc(docset, files);

	private static string Config(string docset, params DocFile[] files) => DocsetGolden.Configure(docset, true, [], files);

	private static string Config(string member, string docset, params DocFile[] files) =>
		DocsetGolden.Configure(docset, true, [member], files);

	[Test]
	public void EmptyFile_Resolved_Outcome() =>
		Whole("")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.Toc.DocumentationSetFile'.
		"""
			);

	[Test]
	public void ProjectOnly_Resolved_Outcome() => Whole("project: cases\n").Should().MatchInlineSnapshot("diagnostics:");

	[Test]
	public void EmptyToc_Resolved_Outcome() => Whole("project: cases\ntoc: []\n").Should().MatchInlineSnapshot("diagnostics:");

	[Test]
	public void UnknownTopLevelKey_Resolved_Outcome() =>
		Whole("proyect: cases\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		error /docs/docset.yml:1 Unknown key 'proyect'. Did you mean 'project'?
		"""
			);

	[Test]
	public void TocAsMap_Resolved_Outcome() =>
		Whole("project: cases\ntoc:\n  file: index.md\n").Should().MatchInlineSnapshot("diagnostics:");

	[Test]
	public void TocItemUnknownKey_Resolved_Outcome() =>
		Toc("  - file: index.md\n  - fiel: other.md\n", new DocFile("index.md"), new DocFile("other.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml:4 Unknown key 'fiel' in $.toc[1]. Did you mean 'file'?
			IndexFileRef  index.md
			"""
			);

	[Test]
	public void TocItemThatIsAScalar_Resolved_Outcome() =>
		Toc("  - index.md\n  - file: other.md\n", new DocFile("index.md"), new DocFile("other.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml 'index.md' is not a valid toc entry. Each entry must be a mapping with a 'file:', 'folder:', or 'toc:' key.
			FileRef  other.md
			"""
			);

	[Test]
	public void TocItemThatNamesAYamlFile_Resolved_Outcome() =>
		Toc("  - sub.yml\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		error /docs/docset.yml 'sub.yml' is not a valid toc entry. Each entry must be a mapping with a 'file:', 'folder:', or 'toc:' key. An adjacent 'sub.yml' next to docset.yml is not supported. Inline its entries directly in docset.yml, or use 'toc: <folder>' to point to a subdirectory that contains a toc.yml.
		"""
			);

	[Test]
	public void TocItemWithoutAnyKnownKind_Resolved_Outcome() =>
		Toc("  - children:\n      - file: a.md\n", new DocFile("a.md")).Should().MatchInlineSnapshot("diagnostics:");

	[Test]
	public void FileThatDoesNotExist_Resolved_Outcome() =>
		Toc("  - file: missing.md\n").Should().MatchInlineSnapshot("""
		diagnostics:
		FileRef  missing.md
		""");

	[Test]
	public void HiddenFileThatDoesNotExist_Resolved_Outcome() =>
		Toc("  - hidden: missing.md\n").Should().MatchInlineSnapshot("""
		diagnostics:
		FileRef  missing.md [hidden]
		""");

	[Test]
	public void FolderThatDoesNotExist_Resolved_Outcome() =>
		Toc("  - folder: missing\n").Should().MatchInlineSnapshot("""
		diagnostics:
		FolderRef  missing
		""");

	[Test]
	public void FolderWithoutPages_Resolved_Outcome() =>
		Toc("  - folder: empty\n", new DocFile("empty/image.png", "x"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FolderRef  empty
			""");

	[Test]
	public void FileWithoutMarkdownExtension_Resolved_Outcome() =>
		Toc("  - file: notes.txt\n  - file: noext\n", new DocFile("notes.txt"), new DocFile("noext"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  notes.txt
			FileRef  noext
			""");

	[Test]
	public void FileOutsideTheDocset_Resolved_Outcome() =>
		Toc("  - file: ../outside.md\n", new DocFile("../outside.md"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  ../outside.md
			""");

	[Test]
	public void FolderOutsideTheDocset_Resolved_Outcome() =>
		Toc("  - folder: ../elsewhere\n", new DocFile("../elsewhere/a.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			throws ScopedFileSystemException: Access denied: '/docs/../elsewhere' resolves to '/elsewhere' which is outside all configured scope roots.
			"""
			);

	[Test]
	public void TocOutsideTheDocset_Resolved_Outcome() =>
		Toc("  - toc: ../elsewhere\n", new DocFile("../elsewhere/toc.yml", "toc:\n  - file: a.md\n"), new DocFile("../elsewhere/a.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml Table of contents file not found: ../elsewhere/toc.yml
				IsolatedTableOfContentsRef  ../elsewhere
				"""
			);

	[Test]
	public void FileWithDotDotInsideThePath_Resolved_Outcome() =>
		Toc("  - file: a/../b.md\n", new DocFile("b.md"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  a/../b.md
			""");

	[Test]
	public void FileWithAnAbsolutePath_Resolved_Outcome() =>
		Toc("  - file: /docs/b.md\n", new DocFile("b.md"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  /docs/b.md
			""");

	[Test]
	public void FileListedTwice_Resolved_Outcome() =>
		Toc("  - file: a.md\n  - file: a.md\n", new DocFile("a.md"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  a.md
			FileRef  a.md
			""");

	[Test]
	public void FileListedAndAlsoInADiscoveredFolder_Resolved_Outcome() =>
		Toc("  - file: dir/a.md\n  - folder: dir\n", new DocFile("dir/a.md"), new DocFile("dir/b.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			FileRef  dir/a.md
			FolderRef  dir
			  FileRef  dir/a.md
			  FileRef  dir/b.md
			"""
			);

	[Test]
	public void FolderIndexListedAsChildAndDiscovered_Resolved_Outcome() =>
		Toc("  - folder: dir\n    children:\n      - file: index.md\n      - file: index.md\n", new DocFile("dir/index.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				FolderRef  dir
				  IndexFileRef  dir/index.md
				  IndexFileRef  dir/index.md
				"""
			);

	[Test]
	public void FileInDifferentCase_Resolved_Outcome() =>
		Toc("  - file: A.md\n", new DocFile("a.md")).Should().MatchInlineSnapshot("""
		diagnostics:
		FileRef  A.md
		""");

	[Test]
	public void FileAndFolderKeysTogether_Resolved_Outcome() =>
		Toc("  - folder: dir\n    file: x.md\n", new DocFile("dir/x.md"), new DocFile("dir/y.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			hint /docs/docset.yml File name 'x.md' does not match folder name 'dir'. Best practice is to name the file the same as the folder (e.g., 'folder: dir, file: dir.md').
			FolderRef  dir
			  FolderIndexFileRef  dir/x.md
			"""
			);

	[Test]
	public void HiddenAndFileKeysTogether_Resolved_Outcome() =>
		Toc("  - file: a.md\n    hidden: b.md\n", new DocFile("a.md"), new DocFile("b.md"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  a.md
			""");

	[Test]
	public void FolderChildrenWithAnAbsentIndex_Resolved_Outcome() =>
		Toc("  - folder: dir\n    children:\n      - file: a.md\n", new DocFile("dir/a.md"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FolderRef  dir
			  FileRef  dir/a.md
			""");

	[Test]
	public void SortWithAnUnknownValue_Resolved_Outcome() =>
		Toc("  - folder: dir\n    sort: sideways\n", new DocFile("dir/a.md"), new DocFile("dir/b.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Unknown sort order 'sideways' for folder 'dir'. Valid values are: asc, ascending, desc, descending.
			FolderRef  dir sort=sideways
			  FileRef  dir/a.md
			  FileRef  dir/b.md
			"""
			);

	[Test]
	public void ExcludeAsAScalar_Resolved_Outcome() =>
		Toc("  - folder: dir\n    exclude: 'a.md'\n", new DocFile("dir/a.md"), new DocFile("dir/b.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			FolderRef  dir
			  FileRef  dir/a.md
			  FileRef  dir/b.md
			"""
			);

	[Test]
	public void TocWithChildrenInTheParent_Resolved_Outcome() =>
		Toc(
			"  - toc: sub\n    children:\n      - file: x.md\n",
			new DocFile("sub/toc.yml", "toc:\n  - file: y.md\n"),
			new DocFile("sub/y.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml TableOfContents 'sub' may not contain children, define children in 'sub/toc.yml' instead.
			"""
			);

	[Test]
	public void TocWithoutATocFile_Resolved_Outcome() =>
		Toc("  - toc: sub\n", new DocFile("sub/a.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		error /docs/docset.yml Table of contents file not found: sub/toc.yml
		IsolatedTableOfContentsRef  sub
		"""
			);

	[Test]
	public void TocFileWithoutChildren_Resolved_Outcome() =>
		Toc("  - toc: sub\n", new DocFile("sub/toc.yml", "toc: []\n"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/sub/toc.yml Table of contents 'sub' has no children defined
			IsolatedTableOfContentsRef  sub
			"""
			);

	[Test]
	public void TocFileWithAnUnknownKey_Resolved_Outcome() =>
		Toc("  - toc: sub\n", new DocFile("sub/toc.yml", "bogus: 1\ntoc:\n  - file: a.md\n"), new DocFile("sub/a.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/sub/toc.yml:1 Unknown key 'bogus'. Valid keys: default_cta, island, project, suppress, toc.
				IsolatedTableOfContentsRef  sub
				  FileRef  sub/a.md (container-relative: a.md)
				"""
			);

	[Test]
	public void TocFileThatIsNotYaml_Resolved_Outcome() =>
		Toc("  - toc: sub\n", new DocFile("sub/toc.yml", "toc: [unclosed\n"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			throws SemanticErrorException: (Lin: 1, Col: 0, Chr: 15) - (Lin: 1, Col: 0, Chr: 15): While parsing a flow sequence, did not find expected ',' or ']'.
			"""
			);

	[Test]
	public void TocFileWithAMissingFile_Resolved_Outcome() =>
		Toc("  - toc: sub\n", new DocFile("sub/toc.yml", "toc:\n  - file: gone.md\n"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			IsolatedTableOfContentsRef  sub
			  FileRef  sub/gone.md (container-relative: gone.md)
			"""
			);

	// A toc that includes itself from the docset root (`toc: .` in a root toc.yml) overflows the stack and ends the test process,
	// so that case is not here. These variants stop with an exception instead.
	[Test]
	public void TocThatIncludesItselfThroughAParentPath_Resolved_Outcome() =>
		Toc("  - toc: a\n", new DocFile("a/toc.yml", "toc:\n  - toc: ../a\n"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				throws ArgumentOutOfRangeException: (message omitted)
				"""
			);

	[Test]
	public void TwoTocsThatIncludeEachOther_Resolved_Outcome() =>
		Toc("  - toc: a\n", new DocFile("a/toc.yml", "toc:\n  - toc: ../b\n"), new DocFile("b/toc.yml", "toc:\n  - toc: ../a\n"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				throws ArgumentOutOfRangeException: (message omitted)
				"""
			);

	[Test]
	public void TocThatIncludesItselfWithADotSlash_Resolved_Outcome() =>
		Toc("  - toc: a\n", new DocFile("a/toc.yml", "toc:\n  - toc: ./\n  - file: x.md\n"), new DocFile("a/x.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				throws ArgumentOutOfRangeException: (message omitted)
				"""
			);

	[Test]
	public void TocThatIncludesItsOwnFolderThroughDotDot_Resolved_Outcome() =>
		Toc("  - toc: a\n", new DocFile("a/toc.yml", "toc:\n  - toc: a/..\n"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				throws ArgumentOutOfRangeException: (message omitted)
				"""
			);

	[Test]
	public void TocThatIncludesItselfByAFullPath_Resolved_Outcome() =>
		Toc("  - toc: a/b\n", new DocFile("a/b/toc.yml", "toc:\n  - toc: b/../b\n"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/a/b/toc.yml Table of contents file not found: a/b/b/../b/toc.yml
			IsolatedTableOfContentsRef  a/b
			  IsolatedTableOfContentsRef  a/b/b/../b (container-relative: b/../b)
			"""
			);

	[Test]
	public void TocFileListedTwice_Resolved_Outcome() =>
		Toc("  - toc: sub\n  - toc: sub\n", new DocFile("sub/toc.yml", "toc:\n  - file: a.md\n"), new DocFile("sub/a.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				IsolatedTableOfContentsRef  sub
				  FileRef  sub/a.md (container-relative: a.md)
				IsolatedTableOfContentsRef  sub
				  FileRef  sub/a.md (container-relative: a.md)
				"""
			);

	[Test]
	public void TocNameWithSlashIsRelativeToTheContext_Resolved_Outcome() =>
		Toc("  - toc: a/b\n", new DocFile("a/b/toc.yml", "toc:\n  - file: x.md\n"), new DocFile("a/b/x.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			IsolatedTableOfContentsRef  a/b
			  FileRef  a/b/x.md (container-relative: x.md)
			"""
			);

	[Test]
	public void TocInsideTocWithSlash_Resolved_Outcome() =>
		Toc(
			"  - toc: a\n",
			new DocFile("a/toc.yml", "toc:\n  - toc: b/c\n"),
			new DocFile("a/b/c/toc.yml", "toc:\n  - file: x.md\n"),
			new DocFile("a/b/c/x.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			IsolatedTableOfContentsRef  a
			  IsolatedTableOfContentsRef  a/b/c (container-relative: b/c)
			    FileRef  a/b/c/x.md (container-relative: x.md)
			"""
			);

	[Test]
	public void CrossLinkWithHiddenTrue_Resolved_BecomesAHiddenFileNamedTrue() =>
		Toc("  - crosslink: docs-content://page/\n    hidden: true\n")
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  true [hidden]
			""");

	[Test]
	public void CrossLinkWithTitleAndHiddenFalse_Resolved_Outcome() =>
		Toc("  - crosslink: docs-content://page/\n    title: Page\n    hidden: false\n")
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  false [hidden]
			""");

	[Test]
	public void CrossLinkThatIsNotAUri_Resolved_Outcome() =>
		Toc("  - crosslink: not a uri\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: /docs/docset.yml: (Lin: 3, Col: 0, Chr: 45) - (Lin: 3, Col: 0, Chr: 45): Invalid URI: The format of the URI could not be determined.
		inner UriFormatException: Invalid URI: The format of the URI could not be determined.
		"""
			);

	[Test]
	public void CrossLinkWithoutScheme_Resolved_Outcome() =>
		Toc("  - crosslink: page/only\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: /docs/docset.yml: (Lin: 3, Col: 0, Chr: 45) - (Lin: 3, Col: 0, Chr: 45): Invalid URI: The format of the URI could not be determined.
		inner UriFormatException: Invalid URI: The format of the URI could not be determined.
		"""
			);

	[Test]
	public void CrossLinkWithChildren_Resolved_Outcome() =>
		Toc("  - crosslink: docs-content://page/\n    children:\n      - file: a.md\n", new DocFile("a.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			CrossLinkRef  docs-content://page/
			  FileRef  a.md
			"""
			);

	[Test]
	public void HiddenTrueOnItsOwn_Resolved_Outcome() =>
		Toc("  - hidden: true\n").Should().MatchInlineSnapshot("""
		diagnostics:
		FileRef  true [hidden]
		""");

	[Test]
	public void CliSchemaThatDoesNotExist_Resolved_Outcome() =>
		Toc("  - cli: nope.json\n")
			.Should()
			.MatchInlineSnapshot("""
		diagnostics:
		error /docs/docset.yml CLI schema file not found: nope.json
		""");

	[Test]
	public void CliSupplementalFolderThatDoesNotExist_Resolved_Outcome() =>
		Toc("  - cli: schema.json\n    folder: supplemental\n", new DocFile("schema.json", "{}"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			warning /docs/docset.yml CLI supplemental docs folder not found: supplemental
			CliReferenceRef  supplemental
			"""
			);

	[Test]
	public void ListingFolderThatDoesNotExist_Resolved_Outcome() =>
		Toc("  - listing: posts\n")
			.Should()
			.MatchInlineSnapshot("""
		diagnostics:
		error /docs/docset.yml Listing folder not found: posts
		""");

	[Test]
	public void ListingWithANonMarkdownGlobAndNoExtension_Resolved_Outcome() =>
		Toc("  - listing: posts\n    glob: '*.json'\n", new DocFile("posts/a.json", "{}"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Listing 'posts': glob '*.json' may match non-.md files — 'extension:' is required to handle them.
			ListingRef  posts listing=ListingOptions { Glob = *.json, Sort = , Exclude = , Visual = None, Groups = , Extension = , Island = False }
			  IndexFileRef  posts/index.md
			  FileRef  posts/a.json [hidden]
			"""
			);

	[Test]
	public void ListingThatMatchesNothing_Resolved_Outcome() =>
		Toc("  - listing: posts\n    glob: 'zzz*.md'\n", new DocFile("posts/a.md")).Should().MatchInlineSnapshot("diagnostics:");

	[Test]
	public void ListingWithAnUnknownVisual_Resolved_Outcome() =>
		Toc("  - listing: posts\n    visual: sparkly\n", new DocFile("posts/a.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			ListingRef  posts listing=ListingOptions { Glob = , Sort = , Exclude = , Visual = None, Groups = , Extension = , Island = False }
			  IndexFileRef  posts/index.md
			  FileRef  posts/a.md [hidden]
			"""
			);

	[Test]
	public void DetectionRulesFolderThatDoesNotExist_Resolved_Outcome() =>
		Toc("  - file: rules/index.md\n    detection_rules:\n      - ../missing\n", new DocFile("rules/index.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				throws DirectoryNotFoundException: Could not find a part of the path '/docs/missing'.
				"""
			);

	[Test]
	public void DuplicateKeyInATocItem_Resolved_Outcome() =>
		Toc("  - file: a.md\n    file: b.md\n", new DocFile("a.md"), new DocFile("b.md"))
			.Should()
			.MatchInlineSnapshot("""
			diagnostics:
			FileRef  b.md
			""");

	[Test]
	public void MaxTocDepthAsText_Resolved_Outcome() =>
		Whole("project: cases\nmax_toc_depth: deep\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			throws YamlException: /docs/docset.yml: (Lin: 1, Col: 15, Chr: 30) - (Lin: 1, Col: 19, Chr: 34): Invalid integer scalar 'deep'.
			"""
			);

	[Test]
	public void DevDocsAsText_Resolved_Outcome() =>
		Whole("project: cases\ndev_docs: maybe\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: /docs/docset.yml: (Lin: 1, Col: 10, Chr: 25) - (Lin: 1, Col: 15, Chr: 30): Invalid boolean scalar 'maybe'.
		"""
			);

	[Test]
	public void SubsAsList_Resolved_Outcome() =>
		Whole("project: cases\nsubs:\n  - a\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: /docs/docset.yml: (Lin: 2, Col: 2, Chr: 23) - (Lin: 2, Col: 2, Chr: 23): Expected a StartMapping token but found 'StartSequence'.
		"""
			);

	[Test]
	public void CrossLinksAsMap_Resolved_Outcome() =>
		Whole("project: cases\ncross_links:\n  a: b\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			throws YamlException: /docs/docset.yml: (Lin: 2, Col: 2, Chr: 30) - (Lin: 2, Col: 2, Chr: 30): Expected a StartSequence token but found 'StartMapping'.
			"""
			);

	[Test]
	public void FeaturesUnknownKey_Resolved_Outcome() =>
		Whole("project: cases\nfeatures:\n  colour: true\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml:3 Unknown key 'colour' in $.features. Valid keys: disable-github-edit-link, guide-nav, primary-nav.
			"""
			);

	[Test]
	public void FeaturesNotABoolean_Resolved_Outcome() =>
		Whole("project: cases\nfeatures:\n  primary-nav: sometimes\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			throws YamlException: /docs/docset.yml: (Lin: 2, Col: 15, Chr: 40) - (Lin: 2, Col: 24, Chr: 49): Invalid boolean scalar 'sometimes'.
			"""
			);

	[Test]
	public void ProductsAsStrings_Resolved_Outcome() =>
		Whole("project: cases\nproducts:\n  - elasticsearch\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			throws YamlException: /docs/docset.yml: (Lin: 2, Col: 4, Chr: 29) - (Lin: 2, Col: 17, Chr: 42): Expected a StartMapping token but found 'Scalar'.
			"""
			);

	[Test]
	public void SuppressWithAnUnknownHint_Resolved_Outcome() =>
		Whole("project: cases\nsuppress:\n  - NotAHint\ntoc: []\n").Should().MatchInlineSnapshot("diagnostics:");

	[Test]
	public void RegistryWithAnUnknownValue_Configured_Outcome() =>
		Config("Registry", "project: cases\nregistry: elsewhere\ntoc:\n  - file: index.md\n", new DocFile("index.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <ConfigurationFile>:
				  Registry: Public
				"""
			);

	[Test]
	public void CrossLinkWithAnUnknownRegistryPrefix_Configured_Outcome() =>
		Config(
			"CrossLinkEntries",
			"project: cases\ncross_links:\n  - nowhere://elasticsearch\n  - docs-content\ntoc:\n  - file: index.md\n",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml Cross-link 'nowhere://elasticsearch' uses unknown registry 'nowhere'. Use 'public' or 'internal'.
				model <ConfigurationFile>:
				  CrossLinkEntries:
				    - <CrossLinkEntry>:
				      Repository: "docs-content"
				"""
			);

	[Test]
	public void CrossLinkRegistryPrefixOnAPublicDocset_Configured_Outcome() =>
		Config(
			"CrossLinkEntries",
			"project: cases\nregistry: codex\ncross_links:\n  - public://elasticsearch\n  - codex://other\n  - bare\ntoc:\n  - file: index.md\n",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml Cross-link 'codex://other' uses unknown registry 'codex'. Use 'public' or 'internal'.
				model <ConfigurationFile>:
				  CrossLinkEntries:
				    - <CrossLinkEntry>:
				      Repository: "elasticsearch"
				    - <CrossLinkEntry>:
				      Repository: "bare"
				"""
			);

	[Test]
	public void ReleaseNotesWithoutAProduct_Configured_Outcome() =>
		Config(
			"ReleaseNotesProducts",
			"project: cases\nrelease_notes:\n  - product: ''\ntoc:\n  - file: index.md\n",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml A 'release_notes' entry is missing a 'product' value.
				model <ConfigurationFile>:
				  ReleaseNotesProducts: []
				"""
			);

	[Test]
	public void ReleaseNotesWithAnUnknownProduct_Configured_Outcome() =>
		Config(
			"ReleaseNotesProducts",
			"project: cases\nrelease_notes:\n  - product: elasticsarch\ntoc:\n  - file: index.md\n",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml Unknown 'release_notes' product 'elasticsarch'. It must be a product id defined in products.yml.
				model <ConfigurationFile>:
				  ReleaseNotesProducts: []
				"""
			);

	[Test]
	public void ReleaseNotesWithAProductThatIsNotOnTheCdn_Configured_Outcome() =>
		Config(
			"ReleaseNotesProducts",
			"project: cases\nrelease_notes:\n  - product: agent-skills\n  - product: elasticsearch\n  - product: elasticsearch\ntoc:\n  - file: index.md\n",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <ConfigurationFile>:
				  ReleaseNotesProducts:
				    - "agent-skills"
				    - "elasticsearch"
				"""
			);

	[Test]
	public void DefaultCtaThatIsNotDefined_Configured_Outcome() =>
		Config("Ctas", "project: cases\ndefault_cta: nothing\ntoc:\n  - file: index.md\n    default_cta: other\n", new DocFile("index.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml 'default_cta: other' on page 'index.md' does not match any 'cta' template in docset.yml.
				model <ConfigurationFile>:
				  Ctas:
				    trial <Cta>:
				      Name: "trial"
				      Label: "Get started free"
				      Url: "https://cloud.elastic.co/registration?page=docs&placement=docs-siderail"
				      Benefits:
				        - "14-day free trial"
				        - "All features included"
				        - "No setup required"
				"""
			);

	[Test]
	public void CtaWithoutLabelUrlAndWithTooManyBenefits_Configured_Outcome() =>
		Config(
			"Ctas",
			"""
			project: cases
			cta:
			  no-label:
			    button:
			      url: https://example.com
			  no-url:
			    button:
			      label: Go
			  no-button: {}
			  many:
			    button:
			      label: Go
			      url: https://example.com
			    benefits: [a, b, c, d]
			  bad-url:
			    button:
			      label: Go
			      url: 'javascript:alert(1)'
			toc:
			  - file: index.md
			""",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml 'cta.no-label' must define both 'button.label' and 'button.url'.
				error /docs/docset.yml 'cta.no-url' must define both 'button.label' and 'button.url'.
				error /docs/docset.yml 'cta.no-button' must define both 'button.label' and 'button.url'.
				error /docs/docset.yml 'cta.many.benefits' has 4 entries; a maximum of 3 is allowed.
				error /docs/docset.yml 'cta.bad-url.button.url' must use http/https or a relative URL.
				model <ConfigurationFile>:
				  Ctas:
				    trial <Cta>:
				      Name: "trial"
				      Label: "Get started free"
				      Url: "https://cloud.elastic.co/registration?page=docs&placement=docs-siderail"
				      Benefits:
				        - "14-day free trial"
				        - "All features included"
				        - "No setup required"
				"""
			);

	[Test]
	public void BrandingWithMissingAndEscapingImages_Configured_Outcome() =>
		Config(
			"Branding",
			"""
			project: cases
			branding:
			  icon: images/missing.svg
			  og-image: ../outside.png
			  favicon: /abs/path.ico
			  header-bg: not-a-colour
			toc:
			  - file: index.md
			""",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml 'branding.icon' file 'images/missing.svg' does not exist.
				error /docs/docset.yml 'branding.og-image' path '../outside.png' escapes the documentation source directory.
				error /docs/docset.yml 'branding.favicon' file '/abs/path.ico' does not exist.
				model <ConfigurationFile>:
				  Branding <Branding>:
				    Icon: null
				    HeaderBg: "not-a-colour"
				    OgImage: null
				    Favicon: null
				    AppleTouchIcon: null
				    Copyright: null
				"""
			);

	[Test]
	public void BrandingWithPrimaryNav_Configured_Outcome() =>
		Config(
			"Branding",
			"project: cases\nbranding:\n  copyright: x\nfeatures:\n  primary-nav: true\ntoc:\n  - file: index.md\n",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml 'features.primary-nav' cannot be used together with 'branding': the primary nav requires Elastic global navigation.
				model <ConfigurationFile>:
				  Branding <Branding>:
				    Icon: null
				    HeaderBg: null
				    OgImage: null
				    Favicon: null
				    AppleTouchIcon: null
				    Copyright: "x"
				"""
			);

	[Test]
	public void ApiEntriesThatAreMalformed_Configured_Outcome() =>
		Config(
			"ApiConfigurations",
			"""
			project: cases
			api:
			  no-product:
			    - spec: a.json
			  unknown-product:
			    - spec: a.json
			      product: elasticsarch
			  no-spec:
			    - product: elasticsearch
			  bad-repository:
			    - spec: a.json
			      product: elasticsearch
			      repository: not-a-repo
			  bad-category:
			    - spec: a.json
			      product: elasticsearch
			      catalog:
			        categories: [nowhere]
			  two-entries:
			    - spec: a.json
			      product: elasticsearch
			    - spec: b.json
			      product: elasticsearch
			  escaping-spec:
			    - spec: ../a.json
			      product: elasticsearch
			  blank-spec:
			    - spec: 'dir/'
			      product: elasticsearch
			toc:
			  - file: index.md
			""",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml:4 API 'no-product' is missing required 'product:'. It must match a product id defined in products.yml.
				error /docs/docset.yml:7 Unknown 'product: elasticsarch' for API 'unknown-product'. It must be a product id defined in products.yml. Did you mean "elasticsearch"?
				error /docs/docset.yml:9 API 'no-spec' is missing required 'spec:'. Its basename is required to resolve the remote version index, even when the file is not present locally.
				error /docs/docset.yml:13 'repository: not-a-repo' for API 'bad-repository' must be in 'org/repo' form, e.g. 'elastic/elasticsearch-specification'.
				error /docs/docset.yml:18 Unknown catalog category 'nowhere' for API 'bad-category'. Valid categories: ece, ess, self, serverless (or ech for ess).
				error /docs/docset.yml API configuration for 'two-entries' must have exactly one entry, found 2.
				error /docs/docset.yml:25 'spec: ../a.json' for API 'escaping-spec' escapes the documentation source directory.
				error /docs/docset.yml:28 'spec: dir/' for API 'blank-spec' does not resolve to a file name.
				model <ConfigurationFile>:
				  ApiConfigurations:
				    bad-category <ResolvedApiConfiguration>:
				      ProductKey: "bad-category"
				      Product: <Product elasticsearch>
				      SpecFileName: "a.json"
				      LocalSpecFile: null
				      Repository: null
				      Children: []
				      ApiContentDirectory: <path /docs/api/bad-category>
				      CatalogCategories: []
				"""
			);

	[Test]
	public void StorybookWithAnUnlistedVariable_Configured_Outcome() =>
		Config(
			"StorybookRegistry",
			"project: cases\nstorybook:\n  registry: ${NOT_ALLOWED_VARIABLE}/sb\ntoc:\n  - file: index.md\n",
			new DocFile("index.md")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				warning /docs/docset.yml 'storybook.registry' references environment variable 'NOT_ALLOWED_VARIABLE' which is not allow-listed for interpolation and is left literal. Allowed: KIBANA_STORYBOOK_REGISTRY.
				model <ConfigurationFile>:
				  StorybookRegistry: "${NOT_ALLOWED_VARIABLE}/sb"
				"""
			);

	[Test]
	public void ProductThatDoesNotExist_Configured_Outcome() =>
		Config("Products", "project: cases\nproducts:\n  - id: nothing\ntoc:\n  - file: index.md\n", new DocFile("index.md"))
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <ConfigurationFile>:
				  Products: []
				"""
			);

	[Test]
	public void ExcludePatternThatIsNotAGlob_Configured_Outcome() =>
		Config("project: cases\nexclude:\n  - '[unclosed'\ntoc:\n  - file: index.md\n", new DocFile("index.md"))
			.Should()
			.MatchInlineSnapshot("""
				diagnostics:
				model <ConfigurationFile>:
				""");

	[Test]
	public void RedirectsThatAreMalformed_Configured_Outcome() =>
		Config(
			"Redirects",
			"project: cases\ntoc:\n  - file: index.md\n",
			new DocFile("index.md"),
			new DocFile(
				"redirects.yml",
				"""
				redirects:
				  'a.md': 'missing.md'
				  'b.md': '../outside.md'
				  'index.md': 'index.md'
				  'c.md':
				    to: ''
				  'd.md':
				    unknown: x
				"""
			)
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/redirects.yml:8 Unknown key 'unknown' in $.redirects['d.md']. Valid keys: anchors, many, to.
				model <ConfigurationFile>:
				  Redirects:
				    a.md <LinkRedirect>:
				      Anchors: null
				      To: "missing.md"
				      Many: null
				    b.md <LinkRedirect>:
				      Anchors: null
				      To: "../outside.md"
				      Many: null
				    index.md <LinkRedirect>:
				      Anchors: null
				      To: "index.md"
				      Many: null
				"""
			);

	[Test]
	public void RedirectsFileThatIsNotYaml_Configured_Outcome() =>
		Config(
			"project: cases\ntoc:\n  - file: index.md\n",
			new DocFile("index.md"),
			new DocFile("redirects.yml", "redirects: [unclosed\n")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/redirects.yml Could not load docset.yml
			throws SemanticErrorException: (Lin: 1, Col: 0, Chr: 21) - (Lin: 1, Col: 0, Chr: 21): While parsing a flow sequence, did not find expected ',' or ']'.
			"""
			);

	[Test]
	public void RedirectsWithADuplicateSource_Configured_Outcome() =>
		Config(
			"project: cases\ntoc:\n  - file: index.md\n",
			new DocFile("index.md"),
			new DocFile("redirects.yml", "redirects:\n  a.md: index.md\n  a.md: index.md\n")
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/redirects.yml Could not load docset.yml
				throws ArgumentException: (message omitted)
				"""
			);

	[Test]
	public void DocsetFileThatIsMissing_Configured_Warns()
	{
		var collector = new CapturingDiagnosticsCollector();
		var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem(
			new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>(),
			DocsetGolden.Root
		);
		var docSet = new Elastic.Documentation.Configuration.Toc.DocumentationSetFile();
		var config = DocsetGolden.CreateConfiguration(docSet, collector, fileSystem, withGlobalConfig: false);

		$"{collector.Captured.ToSnapshotText()}project: {config.Project}".Should().MatchInlineSnapshot(
			"""
			warning /docs/docset.yml No configuration file found
			project: unknown
			"""
		);
	}
}
