// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.LegacyDocs.Migration.Asciidoc;
using Elastic.LegacyDocs.Migration.Asciidoc.Ast;

namespace Elastic.LegacyDocs.Migration.Tests;

/// <summary>
/// Unit tests for PageChunker that verify it replicates the legacy asciidoctor chunker behaviour.
/// Key rule: a section becomes its own page iff it is not [discrete]/[float] AND its level is
/// &lt;= (conf.yaml chunk + 1).  Level-0 sections are pages only when they come from an include.
/// </summary>
public class ChunkerTests
{
	private static MarkdownEmitter Emitter() =>
		new(new MarkdownEmitterOptions { BookPrefix = "test", Version = "1.0" });

	private static Elastic.LegacyDocs.Migration.Asciidoc.Ast.AsciidocDocument Parse(string content, Dictionary<string, string>? files = null)
	{
		var opts = new AsciidocParserOptions
		{
			FileReader = files is not null
				? path => files.TryGetValue(path, out var c) ? c : null
				: null
		};
		return new AsciidocParser(opts).Parse(content, "/base");
	}

	// ── DocTitle becomes the index page ───────────────────────────────────────

	[Fact]
	public void DocTitle_BecomesIndexPage_NoDuplicatePage()
	{
		// The Level-0 `= Book Title` section is the transparent wrapper; it becomes `index.md`.
		// There must NOT be a separate `book_title` slug.
		const string source = """
            = Book Title

            Some landing text.
            """;

		var pages = PageChunker.Chunk(Parse(source), chunkLevel: 1, Emitter());

		pages.Should().HaveCount(1);
		pages[0].Slug.Should().Be("index");
		pages[0].MarkdownContent.Should().Contain("Some landing text");
	}

	// ── Discrete sections never become pages ──────────────────────────────────

	[Fact]
	public void DiscreteSection_IsNeverChunked()
	{
		// quickstart/index.asciidoc shape via include:
		//   = Elasticsearch Guide  (Level-0 wrapper → index)
		//     = Quick starts       (Level-0 include → its own page)
		//       [discrete] == Requirements  (discrete → inline on quickstart page)
		//       == Index and search…        (Level 1 ≤ 2 → child page)
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
                = Elasticsearch Guide

                include::quickstart.adoc[]
                """,
			["/base/quickstart.adoc"] = """
                [[quickstart]]
                = Quick starts

                [discrete]
                [[quickstart-requirements]]
                == Requirements

                Requirements text.

                [[getting-started]]
                == Index and search using APIs

                Getting started text.
                """,
		};

		var pages = PageChunker.Chunk(Parse(files["/base/index.adoc"], files), chunkLevel: 1, Emitter());

		// Top-level: index + quickstart.
		pages.Should().HaveCount(2);
		var quickstart = pages.First(p => p.Slug == "quickstart");

		// [discrete] == Requirements → inline heading on the quickstart page, NOT a child page.
		quickstart.Children.Should().HaveCount(1);
		quickstart.Children[0].Slug.Should().Be("getting-started");
		quickstart.MarkdownContent.Should().Contain("Requirements text");
		quickstart.MarkdownContent.Should().NotContain("Getting started text"); // in child page
	}

	// ── Section deeper than chunkLevel stays inline, rebased as ## ───────────

	[Fact]
	public void Section_DeeperThanChunkLevel_StaysOnParentPage()
	{
		// setup/install/targz.asciidoc shape (chunk:1 → effectiveChunkLevel 2):
		//   = Book  (Level-0 wrapper → index)
		//     == Installing   (Level 1 → page)
		//       === Install from archive  (Level 2 ≤ 2 → page, child of Installing)
		//         ==== Next steps         (Level 3 > 2 → inline ## on the targz page)
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
                = Book

                include::setup.adoc[]
                """,
			["/base/setup.adoc"] = """
                [[setup]]
                == Installing Elasticsearch

                include::targz.adoc[]
                """,
			["/base/targz.adoc"] = """
                [[targz]]
                === Install from archive on Linux

                Intro text.

                ==== Next steps

                Next steps text.
                """,
		};

		var pages = PageChunker.Chunk(Parse(files["/base/index.adoc"], files), chunkLevel: 1, Emitter());

		// index + setup at top level.
		pages.Should().HaveCount(2);
		var setup = pages.First(p => p.Slug == "setup");
		// targz is a child of setup.
		setup.Children.Should().HaveCount(1);
		var targz = setup.Children[0];
		targz.Slug.Should().Be("targz");
		targz.Children.Should().BeEmpty();                       // no further child pages
		targz.MarkdownContent.Should().Contain("Intro text");
		targz.MarkdownContent.Should().Contain("## Next steps"); // Level 3, rebased: effective=3-2=1 → ##
	}

	// ── TitleAbbrev emits navigation_title frontmatter ────────────────────────

	[Fact]
	public void TitleAbbrev_EmitsNavigationTitleFrontmatter()
	{
		// `== ...` (Level 1) is the doc title in a bare file; use a book wrapper.
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
                = Book

                include::getting-started.adoc[]
                """,
			["/base/getting-started.adoc"] = """
                [[getting-started]]
                == Index and search data using Elasticsearch APIs

                <titleabbrev>Basics: Index and search using APIs</titleabbrev>

                Page content.
                """,
		};

		var pages = PageChunker.Chunk(Parse(files["/base/index.adoc"], files), chunkLevel: 1, Emitter());

		var page = pages.First(p => p.Slug == "getting-started");
		page.NavigationTitle.Should().Be("Basics: Index and search using APIs");
		page.MarkdownContent.Should().StartWith("---\n");
		page.MarkdownContent.Should().Contain("navigation_title: \"Basics: Index and search using APIs\"");
		page.MarkdownContent.Should().NotContain("<titleabbrev>");
	}

	[Fact]
	public void TitleAbbrev_MatchingTitle_DoesNotEmitFrontmatter()
	{
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
                = Book

                include::mypage.adoc[]
                """,
			["/base/mypage.adoc"] = """
                [[mypage]]
                == My Page

                <titleabbrev>My Page</titleabbrev>

                Content.
                """,
		};

		var pages = PageChunker.Chunk(Parse(files["/base/index.adoc"], files), chunkLevel: 1, Emitter());

		var page = pages.First(p => p.Slug == "mypage");
		page.NavigationTitle.Should().BeNull();
		page.MarkdownContent.Should().NotContain("navigation_title:");
	}

	// ── Auto-id derivation ────────────────────────────────────────────────────

	[Fact]
	public void IdLessSection_UsesAutoId()
	{
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
                = Book

                include::install.adoc[]
                """,
			["/base/install.adoc"] = """
                == Install from Archive on Linux/MacOS

                Content.
                """,
		};

		var pages = PageChunker.Chunk(Parse(files["/base/index.adoc"], files), chunkLevel: 1, Emitter());

		// Auto-id: lowercase, non-alphanumeric runs → '_', leading/trailing '_' trimmed.
		var page = pages.First(p => p.Slug != "index");
		page.Slug.Should().Be("install_from_archive_on_linux_macos");
	}

	[Fact]
	public void DuplicateSlug_IsSuffixed()
	{
		// Two included files with the same section id would collide; the second gets _2.
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
                = Book

                include::a.adoc[]
                include::b.adoc[]
                """,
			["/base/a.adoc"] = """
                [[dup]]
                == Section A

                Content A.
                """,
			["/base/b.adoc"] = """
                [[dup]]
                == Section B

                Content B.
                """,
		};

		var diagnostics = new List<string>();
		var doc = Parse(files["/base/index.adoc"], files);
		var pages = PageChunker.Chunk(doc, chunkLevel: 1, Emitter(), d => diagnostics.Add(d));

		var slugs = FlatSlugs(pages);
		slugs.Should().Contain("dup");
		slugs.Should().Contain("dup_2");
		diagnostics.Should().HaveCount(1);
		diagnostics[0].Should().Contain("Slug collision");
	}

	// ── Include-file cross-level nesting ─────────────────────────────────────

	[Fact]
	public void CrossInclude_Level1FollowingLevel0_NestedAsChildren()
	{
		// Mirrors the migration guide pattern:
		//   migration/index.asciidoc includes migration_intro (= L0) then
		//   individual migrate_8_N files (== L1), all via ProcessInclude.
		// The L1 sections should become children of the L0 section, not siblings.
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
                = Book

                include::migration/index.adoc[]
                """,
			["/base/migration/index.adoc"] = """
                include::intro.adoc[]
                include::migrate-1.adoc[]
                include::migrate-2.adoc[]
                """,
			["/base/migration/intro.adoc"] = """
                [[migration-guide]]
                = Migration guide

                Intro text.
                """,
			["/base/migration/migrate-1.adoc"] = """
                [[migrating-1]]
                == Migrating to 1.0

                Migration 1 content.
                """,
			["/base/migration/migrate-2.adoc"] = """
                [[migrating-2]]
                == Migrating to 2.0

                Migration 2 content.
                """,
		};

		var pages = PageChunker.Chunk(Parse(files["/base/index.adoc"], files), chunkLevel: 1, Emitter());

		// Top level: index + migration-guide.
		pages.Should().HaveCount(2);
		var migrationGuide = pages.First(p => p.Slug == "migration-guide");
		migrationGuide.MarkdownContent.Should().Contain("Intro text");

		// Version pages are children of migration-guide, not top-level siblings.
		migrationGuide.Children.Should().HaveCount(2);
		migrationGuide.Children[0].Slug.Should().Be("migrating-1");
		migrationGuide.Children[1].Slug.Should().Be("migrating-2");
	}

	// ── Nested toc.yml indentation ────────────────────────────────────────────

	[Fact]
	public void WriteTocYaml_NestedChildren_IndentsCorrectly()
	{
		var entries = new List<TocEntry>
		{
			new() { File = "index.md" },
			new()
			{
				File = "quickstart.md",
				Children =
				[
					new TocEntry { File = "getting-started.md" },
					new TocEntry { File = "full-text.md" }
				]
			},
		};

		var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "toc.yml");
		YamlWriter.WriteTocYaml(path, entries);
		var yaml = File.ReadAllText(path);

		yaml.Should().StartWith("toc:\n");
		yaml.Should().Contain("  - file: index.md\n");
		yaml.Should().Contain("  - file: quickstart.md\n");
		yaml.Should().Contain("    children:\n");
		yaml.Should().Contain("      - file: getting-started.md\n");
		yaml.Should().Contain("      - file: full-text.md\n");
	}

	[Fact]
	public void WriteTocYaml_IslandToc_EmitsIslandKey()
	{
		var entries = new List<TocEntry>
		{
			new() { File = "index.md" },
			new() { Toc = "8.19", Island = true },
			new() { Toc = "8.18", Island = true },
		};

		var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "toc.yml");
		YamlWriter.WriteTocYaml(path, entries);
		var yaml = File.ReadAllText(path);

		yaml.Should().Contain("  - toc: 8.19\n");
		yaml.Should().Contain("    island: true\n");
		yaml.Should().Contain("  - toc: 8.18\n");
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private static IReadOnlyList<string> FlatSlugs(IReadOnlyList<PageOutput> pages)
	{
		var result = new List<string>();
		Collect(pages, result);
		return result;

		static void Collect(IReadOnlyList<PageOutput> ps, List<string> acc)
		{
			foreach (var p in ps)
			{
				acc.Add(p.Slug);
				Collect(p.Children, acc);
			}
		}
	}
}
