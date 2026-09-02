// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>Captures diagnostics in-memory; the channel-based base class needs a reader started to surface them.</summary>
internal sealed class RecordingDiagnosticsCollector() : DiagnosticsCollector([])
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

	public override void Write(Diagnostic diagnostic)
	{
		IncrementSeverityCount(diagnostic);
		_diagnostics.Add(diagnostic);
	}
}

/// <summary>Covers the <c>source:</c> key, which points a TOC entry at content outside the documentation set root.</summary>
public class TocSourceTests
{
	/// <summary>
	/// Resolution is pure path arithmetic over the supplied filesystem, so assertions have to compute their
	/// expected paths from that same filesystem — on Windows a mock rooted on <c>C:</c> does not match a real
	/// <see cref="Path.GetFullPath(string)"/> anchored on the drive the tests run from.
	/// </summary>
	private static (DocumentationSetFile Result, MockFileSystem FileSystem) LoadAndResolve(
		RecordingDiagnosticsCollector collector,
		string docsetYaml,
		params (string path, string content)[] additionalFiles
	)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/repo/docs/docset.yml", new MockFileData(docsetYaml));
		foreach (var (path, content) in additionalFiles)
			fileSystem.AddFile(path, new MockFileData(content));

		var docsetPath = fileSystem.FileInfo.New("/repo/docs/docset.yml");
		var result = DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, "/repo"));
		return (result, fileSystem);
	}

	[Fact]
	public void Deserialize_SourceOnFileEntry_IsCaptured()
	{
		// language=yaml
		var yaml =
			"""
			project: 'test-project'
			toc:
			  - file: feedback.md
			    source: ../packages/kbn-ui/feedback/feedback.md
			""";

		var result = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(yaml);

		var fileRef = result.TableOfContents.ElementAt(0).Should().BeOfType<FileRef>().Subject;
		fileRef.Source.Should().Be("../packages/kbn-ui/feedback/feedback.md");
		fileRef.SourceFullPath.Should().BeNull("resolution happens during LoadAndResolve");
	}

	[Fact]
	public void LoadAndResolve_SourceOnFileEntry_KeepsVirtualPathAndResolvesSource()
	{
		// language=yaml
		var yaml =
			"""
			project: 'test-project'
			toc:
			  - file: index.md
			  - file: feedback.md
			    source: ../packages/kbn-ui/feedback/feedback.md
			""";

		var collector = new RecordingDiagnosticsCollector();
		var (result, fileSystem) = LoadAndResolve(collector, yaml);

		collector.Errors.Should().Be(0);
		var fileRef = result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		fileRef.PathRelativeToDocumentationSet.Should().Be("feedback.md", "'file:' stays the virtual, docset-relative path");
		fileRef.Source.Should().Be("../packages/kbn-ui/feedback/feedback.md");
		fileRef.SourceFullPath.Should().Be(fileSystem.Path.GetFullPath("/repo/packages/kbn-ui/feedback/feedback.md"));
		result.ExternallySourcedFiles.Should().ContainSingle().Which.Should().BeSameAs(fileRef);
	}

	[Fact]
	public void LoadAndResolve_SourceInsideNestedToc_ResolvesRelativeToThatTocYml()
	{
		// language=yaml
		var yaml =
			"""
			project: 'test-project'
			toc:
			  - file: index.md
			  - toc: guides
			""";

		var collector = new RecordingDiagnosticsCollector();
		var (result, fileSystem) = LoadAndResolve(
			collector,
			yaml,
			("/repo/docs/guides/toc.yml",
			// language=yaml
			"""
				toc:
				  - file: index.md
				  - file: feedback.md
				    source: ../../packages/kbn-ui/feedback.md
				""")
		);

		collector.Errors.Should().Be(0);
		var toc = result.TableOfContents.ElementAt(1).Should().BeOfType<IsolatedTableOfContentsRef>().Subject;
		var fileRef = toc.Children.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		fileRef.PathRelativeToDocumentationSet.Should().Be("guides/feedback.md", "the virtual path still carries the toc folder");
		fileRef.SourceFullPath.Should().Be(fileSystem.Path.GetFullPath("/repo/packages/kbn-ui/feedback.md"));
	}

	[Fact]
	public void LoadAndResolve_SourceOnFolderIndexFile_IsCarriedToTheIndexRef()
	{
		// language=yaml
		var yaml =
			"""
			project: 'test-project'
			toc:
			  - file: index.md
			  - folder: feedback
			    file: index.md
			    source: ../packages/kbn-ui/feedback/readme.md
			""";

		var collector = new RecordingDiagnosticsCollector();
		var (result, fileSystem) = LoadAndResolve(collector, yaml);

		collector.Errors.Should().Be(0);
		var folder = result.TableOfContents.ElementAt(1).Should().BeOfType<FolderRef>().Subject;
		var indexRef = folder.Children.ElementAt(0).Should().BeOfType<FolderIndexFileRef>().Subject;
		indexRef.PathRelativeToDocumentationSet.Should().Be("feedback/index.md");
		indexRef.SourceFullPath.Should().Be(fileSystem.Path.GetFullPath("/repo/packages/kbn-ui/feedback/readme.md"));
	}

	[Fact]
	public void LoadAndResolve_SourceOnHiddenEntry_IsCaptured()
	{
		// language=yaml
		var yaml =
			"""
			project: 'test-project'
			toc:
			  - file: index.md
			  - hidden: internals.md
			    source: ../packages/kbn-ui/internals.md
			""";

		var collector = new RecordingDiagnosticsCollector();
		var (result, fileSystem) = LoadAndResolve(collector, yaml);

		collector.Errors.Should().Be(0);
		var fileRef = result.TableOfContents.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		fileRef.Hidden.Should().BeTrue();
		fileRef.SourceFullPath.Should().Be(fileSystem.Path.GetFullPath("/repo/packages/kbn-ui/internals.md"));
	}

	[Fact]
	public void LoadAndResolve_SourceInsideDocumentationSetRoot_EmitsError()
	{
		// language=yaml
		var yaml =
			"""
			project: 'test-project'
			toc:
			  - file: index.md
			  - file: feedback.md
			    source: ./reference/feedback.md
			""";

		var collector = new RecordingDiagnosticsCollector();
		var (result, _) = LoadAndResolve(collector, yaml);

		collector.Errors.Should().Be(1);
		collector.Diagnostics.Should().Contain(d => d.Message.Contains("use 'file: reference/feedback.md' instead"));
		result.ExternallySourcedFiles.Should().BeEmpty();
	}

	[Fact]
	public void LoadAndResolve_SourceIsNotMarkdown_EmitsError()
	{
		// language=yaml
		var yaml =
			"""
			project: 'test-project'
			toc:
			  - file: index.md
			  - file: feedback.md
			    source: ../packages/kbn-ui/feedback.txt
			""";

		var collector = new RecordingDiagnosticsCollector();
		var (result, _) = LoadAndResolve(collector, yaml);

		collector.Errors.Should().Be(1);
		collector.Diagnostics.Should().Contain(d => d.Message.Contains("must point to a markdown file"));
		result.ExternallySourcedFiles.Should().BeEmpty();
	}

	[Fact]
	public void LoadAndResolve_NoSource_LeavesFileRefUnchanged()
	{
		// language=yaml
		var yaml = """
			project: 'test-project'
			toc:
			  - file: index.md
			""";

		var collector = new RecordingDiagnosticsCollector();
		var (result, _) = LoadAndResolve(collector, yaml);

		var fileRef = result.TableOfContents.ElementAt(0).Should().BeOfType<IndexFileRef>().Subject;
		fileRef.Source.Should().BeNull();
		fileRef.SourceFullPath.Should().BeNull();
		result.ExternallySourcedFiles.Should().BeEmpty();
	}
}
