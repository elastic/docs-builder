// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

public class DocumentationSetAuthoringTests
{
	/// <summary>The reader drops keys, coerces text and drops entries; the definition keeps what it hid (D6, D30, D45-D49, D52, D62, D79, D87, D90, D103).</summary>
	[Test]
	public void KeepsWhatTheReaderHides()
	{
		var fileSystem = new MockFileSystem();
		// language=yaml
		fileSystem.AddFile(
			"/docs/docset.yml",
			new MockFileData(
				"""
				project: test
				suppress:
				  - DeepLinkingVirtualFile
				  - NotAHint
				api:
				  elasticsearch:
				    - spec: spec.yaml
				      product: elasticsearch
				      spce: typo
				toc:
				  - file: index.md
				    hidden: true
				    toc: other
				    flie: typo
				  - detection_rules: [rules]
				    folder: guides
				    sort: desc
				    children:
				      - file: a.md
				  - listing: posts
				    visual: everything
				    sort: newest
				    island: yes
				  - crosslink: docs-content://a.md
				    hidden: maybe
				  - just: junk
				  - toc: nested
				"""
			)
		);
		// language=yaml
		fileSystem.AddFile(
			"/docs/nested/toc.yml",
			new MockFileData("""
				toc:
				  - file: index.md
				    colour: red
				""")
		);
		// language=yaml
		fileSystem.AddFile(
			"/docs/redirects.yml",
			new MockFileData(
				"""
				redirects:
				  old.md: {}
				  fine.md: new.md
				  many.md:
				    many:
				      - to: index.md
				"""
			)
		);

		var result = DocumentationSetLoader.ParseDefinition(
			fileSystem.FileInfo.New("/docs/docset.yml"),
			fileSystem.DirectoryInfo.New("/docs"),
			fileSystem,
			new ScopedFileSystem(fileSystem, "/docs")
		);
		var docset = result.Value!;

		docset.RedirectsFile!.Name.Should().Be("redirects.yml");
		docset.UnknownSuppressions.Should().Equal("NotAHint");
		docset.Api.Single().Entries.Single().UnknownKeys.Should().Equal("spce");
		docset.DroppedRedirects.Should().Equal(new DroppedRedirect("old.md"), new DroppedRedirect("many.md", 0));

		var entries = docset.TocAuthoring;
		var file = entries.Single(
			e => e.Entry == "file: index.md" && e.UnknownKeys.Count > 0 && e.File!.EndsWith("docset.yml", StringComparison.Ordinal)
		);
		file.Kind.Should().Be(TocEntryKind.File);
		file.UnknownKeys.Should().Equal("flie");
		file.IgnoredKeys.Should().Equal("hidden", "toc");
		file.Location!.Value.Line.Should().Be(11);
		var folder = entries.Single(e => e.Kind == TocEntryKind.Folder);
		folder.IgnoredKeys.Should().Equal("detection_rules");
		folder.ExplicitChildren.Should().BeTrue();
		var listing = entries.Single(e => e.Kind == TocEntryKind.Listing);
		(listing.IslandRaw, listing.VisualRaw, listing.SortRaw).Should().Be(("yes", "everything", "newest"));
		entries.Single(e => e.Kind == TocEntryKind.CrossLink).HiddenRaw.Should().Be("maybe");
		entries.Single(e => e.Kind == TocEntryKind.Dropped).WrittenKeys.Should().Equal("just");
		entries.Single(e => e.UnknownKeys.Contains("colour")).File.Should().EndWith("nested/toc.yml");

		var products = ProductCatalog.FromPublicProducts(new Dictionary<string, Product>
		{
			["elasticsearch"] = new() { Id = "elasticsearch", DisplayName = "Elasticsearch" }
		});
		var ids = DocumentationSetConstraints
			.All
			.Validate(new DocumentationSetInCatalog(docset, products))
			.Select(v => v.ConstraintId)
			.ToHashSet();
		ids.Should().Contain([
			"suppress-hint-known",
			"api-entry-keys-known",
			"redirect-not-dropped",
			"toc-entry-selects-a-shape",
			"toc-entry-keys-known",
			"toc-file-hidden-title-ignored",
			"toc-detection-rules-needs-file",
			"toc-folder-sort-exclude-need-no-children",
			"toc-island-is-boolean",
			"toc-crosslink-hidden-is-boolean",
			"listing-visual-known",
			"listing-sort-known"
		]);
		ids.Should().NotContain("redirects-file-named-for-docset");
	}

	[Test]
	public void ARedirectsFileNextToAnUnderscoreDocsetIsUnderscored()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/_docset.yml", new MockFileData("project: test\ntoc:\n  - file: index.md\n"));
		fileSystem.AddFile("/docs/_redirects.yml", new MockFileData("redirects:\n  old.md: index.md\n"));

		var result = DocumentationSetLoader.ParseDefinition(
			fileSystem.FileInfo.New("/docs/_docset.yml"),
			fileSystem.DirectoryInfo.New("/docs"),
			fileSystem,
			new ScopedFileSystem(fileSystem, "/docs")
		);

		result.Value!.RedirectsFile!.Name.Should().Be("_redirects.yml");
		result.Value!.Redirects.Should().ContainKey("old.md");
	}
}
