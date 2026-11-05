// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

public class DocumentationSetFileTests
{
	// Tests use direct deserialization to test YAML parsing without TOC loading/resolution
	private DocumentationSetFile Deserialize(string yaml) => ConfigurationFileProvider.Deserialize<DocumentationSetFile>(yaml);

	[Test]
	public void DeserializesBasicProperties()
	{
		// language=yaml
		var yaml =
			"""
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
		result.CrossLinks.Should().HaveCount(2).And.Contain("docs-content").And.Contain("other-docs");
		result.Exclude.Should().HaveCount(2).And.Contain("_*.md").And.Contain("*.tmp");
	}

	[Test]
	public void DeserializesSubstitutions()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           subs:
		             stack: Elastic Stack
		             ecloud: Elastic Cloud
		             dbuild: docs-builder
		           """;

		var result = Deserialize(yaml);

		result.Subs.Should().HaveCount(3).And.ContainKey("stack").WhoseValue.Should().Be("Elastic Stack");
		result.Subs.Should().ContainKey("ecloud").WhoseValue.Should().Be("Elastic Cloud");
		result.Subs.Should().ContainKey("dbuild").WhoseValue.Should().Be("docs-builder");
	}

	[Test]
	public void DeserializesFeatures()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           features:
		             primary-nav: false
		           """;

		var result = Deserialize(yaml);

		result.Features.Should().NotBeNull();
		result.Features.PrimaryNav.Should().BeFalse();
	}

	[Test]
	public void DeserializesApiConfiguration()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           api:
		             elasticsearch:
		               - spec: elasticsearch-openapi.json
		                 product: elasticsearch
		             kibana:
		               - spec: kibana-openapi.json
		                 product: kibana
		           """;

		var result = Deserialize(yaml);

		result.Api.Should().HaveCount(2);
		result.Api["elasticsearch"].SingleEntry!.Spec.Should().Be("elasticsearch-openapi.json");
		result.Api["kibana"].SingleEntry!.Spec.Should().Be("kibana-openapi.json");
	}

	[Test]
	public void DeserializesFileReference()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: getting-started.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot("""
			IndexFileRef  index.md
			FileRef  getting-started.md
			""");
	}

	[Test]
	public void DeserializesHiddenFileReference()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - hidden: 404.md
		             - hidden: developer-notes.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			IndexFileRef  index.md
			FileRef  404.md [hidden]
			FileRef  developer-notes.md [hidden]
			"""
			);
	}

	[Test]
	public void DeserializesFolderReference()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - folder: contribute
		               children:
		                 - file: index.md
		                 - file: locally.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			FolderRef  contribute
			  IndexFileRef  index.md
			  FileRef  locally.md
			"""
			);
	}

	[Test]
	public void DeserializesTocReference()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: development
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot("""
			IndexFileRef  index.md
			IsolatedTableOfContentsRef  development
			""");
	}

	[Test]
	public void DeserializesCrossLinkReference()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: cross-links.md
		               children:
		                 - title: "Getting Started Guide"
		                   crosslink: docs-content://get-started/introduction.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			IndexFileRef  index.md
			FileRef  cross-links.md
			  CrossLinkRef  docs-content://get-started/introduction.md title="Getting Started Guide"
			"""
			);
	}

	[Test]
	public void DeserializesFloatingSectionReference()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - api: elasticsearch
		           """;

		var result = Deserialize(yaml);

		result.TableOfContents.Should().HaveCount(2);
		var section = result.TableOfContents.ElementAt(1).Should().BeOfType<FloatingSectionRef>().Subject;
		section.Kind.Should().Be("api");
		section.Key.Should().Be("elasticsearch");
	}

	[Test]
	public void DeserializesNestedStructure()
	{
		// language=yaml
		var yaml =
			"""
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

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			FolderRef  configure
			  IndexFileRef  index.md
			  FolderRef  site
			    IndexFileRef  index.md
			    FileRef  content.md
			    FileRef  navigation.md
			"""
			);
	}

	[Test]
	public void DeserializesCompleteDocsetYaml()
	{
		// language=yaml
		var yaml =
			"""
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
		             elasticsearch:
		               - spec: elasticsearch-openapi.json
		                 product: elasticsearch
		             kibana:
		               - spec: kibana-openapi.json
		                 product: kibana
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

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			IndexFileRef  index.md
			FileRef  404.md [hidden]
			FolderRef  configure
			  IndexFileRef  index.md
			  FolderRef  site
			    IndexFileRef  index.md
			    FileRef  content.md
			    FileRef  navigation.md
			  FileRef  page.md
			    CrossLinkRef  docs-content://get-started/introduction.md title="Getting Started Guide"
			IsolatedTableOfContentsRef  development
			"""
			);
	}

	[Test]
	public void DeserializesFileWithChildren()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: chapter1.md
		                 - file: chapter2.md
		                 - file: chapter3.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			FileRef  guide.md
			  FileRef  chapter1.md
			  FileRef  chapter2.md
			  FileRef  chapter3.md
			"""
			);
	}

	[Test]
	public void DeserializesFileWithNestedPathsAsChildren()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: api/guide.md
		               children:
		                 - file: api/section1.md
		                 - file: api/section2.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			FileRef  api/guide.md
			  FileRef  api/section1.md
			  FileRef  api/section2.md
			"""
			);
	}

	[Test]
	public void DeserializesDefaultValues()
	{
		// language=yaml
		var yaml =
			"""
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

	[Test]
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

	[Test]
	public void DeserializesCrossLinkWithoutTitle()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		               children:
		                 - crosslink: docs-content://get-started.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot("""
			IndexFileRef  index.md
			  CrossLinkRef  docs-content://get-started.md/
			""");
	}

	[Test]
	public void DeserializesMixedHiddenAndVisibleItems()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - hidden: _internal.md
		             - file: public.md
		             - hidden: _draft.md
		           """;

		var result = Deserialize(yaml);

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			IndexFileRef  index.md
			FileRef  _internal.md [hidden]
			FileRef  public.md
			FileRef  _draft.md [hidden]
			"""
			);
	}

	[Test]
	public void DeserializesDeeplyNestedFileWithChildren()
	{
		// language=yaml
		var yaml =
			"""
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

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			FileRef  guide.md
			  FileRef  chapter1.md
			    FileRef  section1.md
			      FileRef  subsection1.md
			"""
			);
	}

	[Test]
	public void DeserializesMultipleExcludePatterns()
	{
		// language=yaml
		var yaml =
			"""
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

		result.Exclude.Should().HaveCount(5).And.ContainInOrder("_*.md", "*.tmp", "*.draft", ".DS_Store", "node_modules/**");
	}

	[Test]
	public void DeserializesMultipleCrossLinks()
	{
		// language=yaml
		var yaml =
			"""
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

		result.CrossLinks.Should().HaveCount(4).And.ContainInOrder("elasticsearch", "kibana", "docs-content", "cloud");
	}

	[Test]
	public void DeserializesFolderWithMixedChildren()
	{
		// language=yaml
		var yaml =
			"""
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

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			FolderRef  api
			  IndexFileRef  index.md
			  FolderRef  rest
			    IndexFileRef  index.md
			  FileRef  overview.md
			"""
			);
	}

	[Test]
	public void LoadAndResolve_UnknownDocsetKey_EmitsErrorWithLocation()
	{
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml =
			"""
		           project: 'test-project'
		           totally_unknown_key: some-value
		           toc:
		             - file: index.md
		           """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Test"));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var collector = new CapturingDiagnosticsCollector();
		_ = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		collector
			.Captured
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error /docs/docset.yml:2 Unknown key 'totally_unknown_key'. Valid keys: api, branding, codex, cross_links, cta, default_cta, description, dev_docs, display_name, exclude, extensions, features, …."
			);
		collector.Captured.Should().ContainSingle().Which.Column.Should().Be(1);
	}

	[Test]
	public void LoadAndResolve_UnknownNestedTocKey_EmitsErrorWithLocation()
	{
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml =
			"""
		           project: 'test-project'
		           toc:
		             - toc: nested
		           """;
		// language=yaml
		var nestedTocYaml =
			"""
		           totally_unknown_key: some-value
		           toc:
		             - file: index.md
		           """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/nested/toc.yml", new MockFileData(nestedTocYaml));
		fileSystem.AddFile("/docs/nested/index.md", new MockFileData("# Test"));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var collector = new CapturingDiagnosticsCollector();
		_ = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		collector
			.Captured
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error /docs/nested/toc.yml:1 Unknown key 'totally_unknown_key'. Valid keys: default_cta, island, project, suppress, toc."
			);
		collector.Captured.Should().ContainSingle().Which.Column.Should().Be(1);
	}

	[Test]
	public void LoadAndResolvePreservesCliReferenceAppliesTo()
	{
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml =
			"""
		           project: 'test-project'
		           toc:
		             - cli: cli/schema.json
		               folder: cli
		               applies_to:
		                 stack: preview
		                 serverless: preview
		           """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/cli/schema.json", new MockFileData("{}"));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var collector = new DiagnosticsCollector([]);
		var result = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		var cliRef = result.TableOfContents.OfType<CliReferenceRef>().Single();
		cliRef.AppliesTo.Should().NotBeNull();
		cliRef.AppliesTo.Stack.Should().NotBeNull();
		cliRef.AppliesTo.Serverless.Should().NotBeNull();
	}

	[Test]
	public void LoadAndResolveResolvesIsolatedTocReferences()
	{
		// Create a mock file system with docset and nested TOC files
		var fileSystem = new MockFileSystem();

		// Main docset.yml
		// language=yaml
		var docsetYaml =
			"""
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
		var developmentTocYaml =
			"""
		                         toc:
		                           - file: index.md
		                           - file: contributing.md
		                           - folder: internals
		                             children:
		                               - file: architecture.md
		                         """;

		// guides/advanced/toc.yml
		// language=yaml
		var advancedTocYaml =
			"""
		                      toc:
		                        - file: index.md
		                        - file: patterns.md
		                      """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/development/toc.yml", new MockFileData(developmentTocYaml));
		fileSystem.AddFile("/docs/guides/advanced/toc.yml", new MockFileData(advancedTocYaml));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var collector = new DiagnosticsCollector([]);
		var result = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		// Verify TOC references have been preserved (not flattened)
		// We have 3 top-level items: index.md, development TOC, and guides folder
		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			IndexFileRef  index.md
			IsolatedTableOfContentsRef  development
			  IndexFileRef  development/index.md (container-relative: index.md)
			  FileRef  development/contributing.md (container-relative: contributing.md)
			  FolderRef  development/internals (container-relative: internals)
			    FileRef  development/internals/architecture.md (container-relative: internals/architecture.md)
			FolderRef  guides
			  FileRef  guides/getting-started.md
			  IsolatedTableOfContentsRef  guides/advanced
			    IndexFileRef  guides/advanced/index.md (container-relative: index.md)
			    FileRef  guides/advanced/patterns.md (container-relative: patterns.md)
			"""
			);
	}

	[Test]
	public void LoadAndResolvePrependsParentPathsToFileReferences()
	{
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml =
			"""
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
		var collector = new DiagnosticsCollector([]);
		var result = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		TocDump
			.ToText(result.TableOfContents)
			.Should()
			.MatchInlineSnapshot(
				"""
			FileRef  guide.md
			  FileRef  chapter1.md
			    FileRef  section1.md
			FolderRef  api
			  IndexFileRef  api/index.md
			  FileRef  api/reference.md
			"""
			);
	}

	[Test]
	public void LoadAndResolveSetsContextForAllItems()
	{
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml =
			"""
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
		var developmentTocYaml =
			"""
		                         toc:
		                           - file: contributing.md
		                         """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/development/toc.yml", new MockFileData(developmentTocYaml));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var collector = new DiagnosticsCollector([]);
		var result = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		var docset = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:/docs/docset.yml" : "/docs/docset.yml";
		var toc = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:/docs/development/toc.yml" : "/docs/development/toc.yml";

		// All items from docset.yml should have context = /docs/docset.yml
		result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>().Which.Context.OptionalWindowsReplace().Should().Be(docset);

		var guidesFolder = result.TableOfContents.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		guidesFolder.Context.Should().Be(docset);
		guidesFolder.Children.ElementAt(0).Should().BeOfType<FileRef>().Which.Context.OptionalWindowsReplace().Should().Be(docset);

		// The TOC ref itself has context = /docs/docset.yml (where it was referenced)
		var developmentToc = result.TableOfContents.ElementAt(2).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		developmentToc.Context.OptionalWindowsReplace().Should().Be(docset);

		// But children of the TOC ref should have context = /docs/development/toc.yml (where they were defined)
		developmentToc.Children.ElementAt(0).Should().BeOfType<FileRef>().Which.Context.OptionalWindowsReplace().Should().Be(toc);
	}

	// ──────────────────────────────────────────────────────────────
	// island: true parsing
	// ──────────────────────────────────────────────────────────────

	[Test]
	public void DocsetRoot_IslandTrue_Deserializes()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           island: true
		           toc:
		             - file: index.md
		           """;

		var result = Deserialize(yaml);

		result.Island.Should().BeTrue("island: true at the docset root must survive deserialization");
	}

	[Test]
	public void DocsetRoot_NoIsland_DefaultsFalse()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		           """;

		var result = Deserialize(yaml);

		result.Island.Should().BeFalse("island defaults to false when omitted");
	}

	[Test]
	public void InlineTocEntry_IslandTrue_Deserializes()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: reference
		               island: true
		           """;

		var result = Deserialize(yaml);

		var tocRef = result.TableOfContents.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		tocRef.PathRelativeToDocumentationSet.Should().Be("reference");
		tocRef.Island.Should().BeTrue("inline island: true must be captured on IsolatedTableOfContentsRef");
	}

	[Test]
	public void InlineTocEntry_NoIsland_DefaultsFalse()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: reference
		           """;

		var result = Deserialize(yaml);

		var tocRef = result.TableOfContents.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		tocRef.Island.Should().BeFalse("inline island defaults to false when omitted");
	}

	[Test]
	public void NestedTocYml_IslandTrue_PropagatesToRef()
	{
		// island: true in a nested toc.yml root propagates to the IsolatedTableOfContentsRef.Island
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetYaml =
			"""
		                 project: 'test-project'
		                 toc:
		                   - file: index.md
		                   - toc: reference
		                 """;

		// language=yaml
		var referenceTocYaml =
			"""
		                       island: true
		                       toc:
		                         - file: index.md
		                         - file: page.md
		                       """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/reference/toc.yml", new MockFileData(referenceTocYaml));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var collector = new DiagnosticsCollector([]);
		var result = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		var tocRef = result.TableOfContents.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		tocRef.Island.Should().BeTrue("island: true in the nested toc.yml root propagates to the resolved IsolatedTableOfContentsRef");
	}

	[Test]
	public void InlineTocEntry_IslandTrue_OrSemantics_BothInlineAndTocYml()
	{
		// OR semantics: inline island: true + toc.yml island: true both produce Island = true
		var fileSystem = new MockFileSystem();

		// language=yaml
		var docsetWithInlineIsland =
			"""
		                             project: 'test-project'
		                             toc:
		                               - file: index.md
		                               - toc: reference
		                                 island: true
		                             """;

		// language=yaml
		var referenceTocNoIsland =
			"""
		                           toc:
		                             - file: index.md
		                           """;

		// language=yaml
		var docsetNoInlineIsland =
			"""
		                           project: 'test-project'
		                           toc:
		                             - file: index.md
		                             - toc: reference
		                           """;

		// language=yaml
		var referenceTocWithIsland =
			"""
		                             island: true
		                             toc:
		                               - file: index.md
		                             """;

		static IsolatedTableOfContentsRef Resolve(string docsetYaml, string tocYml)
		{
			var fs = new MockFileSystem();
			fs.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
			fs.AddFile("/docs/reference/toc.yml", new MockFileData(tocYml));
			var c = new DiagnosticsCollector([]);
			var r = DocumentationSetFile.LoadAndResolve(c, fs.FileInfo.New("/docs/docset.yml"), new ScopedFileSystem(fs, "/docs"));
			return r.TableOfContents.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		}

		// Only inline island: true → Island = true
		Resolve(docsetWithInlineIsland, referenceTocNoIsland).Island.Should().BeTrue("inline island: true alone is sufficient");

		// Only toc.yml island: true → Island = true
		Resolve(docsetNoInlineIsland, referenceTocWithIsland).Island.Should().BeTrue("toc.yml island: true alone is sufficient");

		// Both → Island = true
		Resolve(docsetWithInlineIsland, referenceTocWithIsland).Island.Should().BeTrue("both set → still island");

		// Neither → Island = false
		Resolve(docsetNoInlineIsland, referenceTocNoIsland).Island.Should().BeFalse("neither set → not island");
	}

	[Test]
	public void LoadAndResolveSetsPathRelativeToContainerCorrectly()
	{
		var fileSystem = new MockFileSystem();

		// Main docset.yml
		// language=yaml
		var docsetYaml =
			"""
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
		var developmentTocYaml =
			"""
		                         toc:
		                           - file: overview.md
		                           - folder: advanced
		                             children:
		                               - file: patterns.md
		                           - toc: internals
		                         """;

		// development/internals/toc.yml
		// language=yaml
		var internalsTocYaml =
			"""
		                       toc:
		                         - file: architecture.md
		                       """;

		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/development/toc.yml", new MockFileData(developmentTocYaml));
		fileSystem.AddFile("/docs/development/internals/toc.yml", new MockFileData(internalsTocYaml));

		var docsetPath = fileSystem.FileInfo.New("/docs/docset.yml");
		var collector = new DiagnosticsCollector([]);
		var result = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/docs"));

		// Items in docset.yml: PathRelativeToContainer should equal PathRelativeToDocumentationSet
		result
			.TableOfContents
			.ElementAt(0)
			.Should()
			.BeOfType<IndexFileRef>()
			.Which
			.PathRelativeToContainer
			.Should()
			.Be("index.md", "file in root docset.yml");

		var guidesFolder = result.TableOfContents.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		guidesFolder.PathRelativeToContainer.Should().Be("guides", "folder in root docset.yml");
		guidesFolder
			.Children
			.ElementAt(0)
			.Should()
			.BeOfType<FileRef>()
			.Which
			.PathRelativeToContainer
			.Should()
			.Be("guides/getting-started.md", "file's full path from container (docset.yml)");

		// Development TOC in docset.yml
		var developmentToc = result.TableOfContents.ElementAt(2).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		developmentToc.PathRelativeToContainer.Should().Be("development", "toc ref in root docset.yml");

		// Items in development/toc.yml: PathRelativeToContainer should be relative to development/
		developmentToc
			.Children
			.ElementAt(0)
			.Should()
			.BeOfType<FileRef>()
			.Which
			.PathRelativeToContainer
			.Should()
			.Be("overview.md", "file in development/toc.yml");

		var advancedFolder = developmentToc.Children.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		advancedFolder.PathRelativeToContainer.Should().Be("advanced", "folder in development/toc.yml");
		advancedFolder
			.Children
			.ElementAt(0)
			.Should()
			.BeOfType<FileRef>()
			.Which
			.PathRelativeToContainer
			.Should()
			.Be("advanced/patterns.md", "file's full path from container (development/toc.yml)");

		// Internals TOC in development/toc.yml
		var internalsToc = developmentToc.Children.ElementAt(2).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		internalsToc.PathRelativeToContainer.Should().Be("internals", "toc ref in development/toc.yml");

		// Items in development/internals/toc.yml: PathRelativeToContainer should be relative to development/internals/
		internalsToc
			.Children
			.ElementAt(0)
			.Should()
			.BeOfType<FileRef>()
			.Which
			.PathRelativeToContainer
			.Should()
			.Be("architecture.md", "file in development/internals/toc.yml");

		// Verify PathRelativeToDocumentationSet is still correct (full paths from docset root)
		guidesFolder.PathRelativeToDocumentationSet.Should().Be("guides");
		guidesFolder
			.Children
			.ElementAt(0)
			.Should()
			.BeOfType<FileRef>()
			.Which
			.PathRelativeToDocumentationSet
			.Should()
			.Be("guides/getting-started.md");

		developmentToc
			.Children
			.ElementAt(0)
			.Should()
			.BeOfType<FileRef>()
			.Which
			.PathRelativeToDocumentationSet
			.Should()
			.Be("development/overview.md");

		internalsToc
			.Children
			.ElementAt(0)
			.Should()
			.BeOfType<FileRef>()
			.Which
			.PathRelativeToDocumentationSet
			.Should()
			.Be("development/internals/architecture.md");
	}
}
