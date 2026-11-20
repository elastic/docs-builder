// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// What <c>redirects.yml</c> reads to today: every case is a whole file, and its block in the verified text is the
/// redirect map as compact JSON followed by the diagnostics the reader wrote.
/// </summary>
public class RedirectFileSnapshotTests
{
	private sealed class Context : IDocumentationSetContext
	{
		public required IDiagnosticsCollector Collector { get; init; }
		public required IDirectoryInfo DocumentationSourceDirectory { get; init; }
		public required GitCheckoutInformation Git { get; init; }
		public required IDocumentationFileSystem ReadFileSystem { get; init; }
		public required DocumentationWriteFileSystem WriteFileSystem { get; init; }
		public required IFileInfo ConfigurationPath { get; init; }
		public required IDirectoryInfo OutputDirectory { get; init; }
		public required BuildType BuildType { get; init; }
		public required IEnvironmentVariables Environment { get; init; }
	}

	private static string Read(string yaml)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/redirects.yml", new MockFileData(yaml));
		var collector = new CapturingDiagnosticsCollector();
		var context = new Context
		{
			Collector = collector,
			DocumentationSourceDirectory = fileSystem.DirectoryInfo.New("/docs"),
			Git = GitCheckoutInformation.Unavailable,
			ReadFileSystem = DocumentationFileSystem.Resolve(
				fileSystem.DirectoryInfo.New("/docs"),
				new DocumentationScopeOptions { Inner = fileSystem, ConfigurationFile = "/docs/docset.yml" }
			),
			WriteFileSystem = new DocumentationWriteFileSystem(
				fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
				null,
				fileSystem
			),
			ConfigurationPath = fileSystem.FileInfo.New("/docs/docset.yml"),
			OutputDirectory = fileSystem.DirectoryInfo.New("/.artifacts"),
			BuildType = BuildType.Isolated,
			Environment = SystemEnvironmentVariables.Instance
		};

		string redirects;
		try
		{
			var file = new RedirectFile(context);
			redirects = file.Redirects is null ? "(null)" : JsonNode.Parse(LinkJson.SerializeRedirects(file.Redirects))!.ToJsonString();
		}
		catch (Exception e)
		{
			redirects = $"throws {e.GetType().Name}";
		}

		var diagnostics = collector.Captured.ToSnapshotText(firstLineOnly: true);
		return $"{redirects}\n{(diagnostics.Length == 0 ? "(no diagnostics)\n" : diagnostics)}";
	}

	private static string Table(params (string Name, string Yaml)[] cases)
	{
		var sb = new StringBuilder();
		foreach (var (name, yaml) in cases)
		{
			_ = sb.Append("## ").AppendLine(name);
			foreach (var line in yaml.TrimEnd().Split('\n'))
				_ = sb.Append("  | ").AppendLine(line.TrimEnd('\r'));
			_ = sb.Append(Read(yaml)).AppendLine();
		}

		return sb.ToString();
	}

	[Test]
	public void Read_ScalarEntries_ExpandToTargetsAndCatchAlls() =>
		Table(
			("plain-target", "redirects:\n  'old.md': 'new.md'"),
			("bang-prefix-is-a-catch-all", "redirects:\n  'old.md': '!new.md'"),
			("empty-scalar-redirects-to-index", "redirects:\n  'old.md': ''"),
			("null-scalar-redirects-to-index", "redirects:\n  'old.md': ~"),
			("bare-key-without-value", "redirects:\n  'old.md':"),
			("cross-repo-target", "redirects:\n  'old.md': 'other-repo://page.md'"),
			("cross-repo-catch-all", "redirects:\n  'old.md': '!other-repo://page.md'"),
			("many-bangs", "redirects:\n  'old.md': '!!new.md'"),
			("keys-are-kept-verbatim", "redirects:\n  'Old/Page.md': 'new.md'\n  'trailing/': 'new.md'\n  '/leading.md': 'new.md'"),
			("duplicate-key", "redirects:\n  'old.md': 'a.md'\n  'old.md': 'b.md'"),
			("keys-differing-by-case", "redirects:\n  'old.md': 'a.md'\n  'OLD.md': 'b.md'"),
			("numeric-and-boolean-scalars", "redirects:\n  'a.md': 123\n  'b.md': true")
		).Should().MatchSnapshot();

	[Test]
	public void Read_MappingEntries_ReadToAnchorsAndMany() =>
		Table(
			("to-only", "redirects:\n  'old.md':\n    to: 'new.md'"),
			("to-and-anchors", "redirects:\n  'old.md':\n    to: 'new.md'\n    anchors: { 'a': 'b', 'c': null }"),
			("catch-all-anchors", "redirects:\n  'old.md':\n    to: 'new.md'\n    anchors: { '!': null }"),
			("anchors-only-takes-the-key-as-target", "redirects:\n  'old.md':\n    anchors: { 'a': 'b' }"),
			("empty-anchors", "redirects:\n  'old.md':\n    to: 'new.md'\n    anchors: {}"),
			("empty-mapping", "redirects:\n  'old.md': {}"),
			("empty-to", "redirects:\n  'old.md':\n    to: ''"),
			("null-to-with-anchors", "redirects:\n  'old.md':\n    to: ~\n    anchors: { 'a': 'b' }"),
			("anchors-as-a-list", "redirects:\n  'old.md':\n    to: 'new.md'\n    anchors: [ 'a', 'b' ]"),
			("anchors-as-a-scalar", "redirects:\n  'old.md':\n    to: 'new.md'\n    anchors: 'a'"),
			("to-as-a-mapping", "redirects:\n  'old.md':\n    to: { a: b }"),
			("sequence-instead-of-mapping", "redirects:\n  'old.md':\n    - 'new.md'")
		).Should().MatchSnapshot();

	[Test]
	public void Read_ManyEntries_KeepOnlyCompleteRules() =>
		Table(
			("to-and-many", "redirects:\n  'old.md':\n    to: 'new.md'\n    many:\n      - to: 'a.md'\n        anchors: { 'x': 'y' }\n      - to: 'b.md'\n        anchors: { '!': null }"),
			("many-only", "redirects:\n  'old.md':\n    many:\n      - to: 'a.md'\n        anchors: { 'x': 'y' }"),
			("many-rule-without-anchors-is-dropped", "redirects:\n  'old.md':\n    to: 'new.md'\n    many:\n      - to: 'a.md'"),
			("many-rule-without-to-takes-the-key", "redirects:\n  'old.md':\n    to: 'new.md'\n    many:\n      - anchors: { 'x': 'y' }"),
			("many-rule-with-empty-anchors", "redirects:\n  'old.md':\n    to: 'new.md'\n    many:\n      - to: 'a.md'\n        anchors: {}"),
			("empty-many", "redirects:\n  'old.md':\n    to: 'new.md'\n    many: []"),
			("many-only-empty", "redirects:\n  'old.md':\n    many: []"),
			("many-as-a-mapping", "redirects:\n  'old.md':\n    to: 'new.md'\n    many: { to: 'a.md' }"),
			("many-with-scalar-items", "redirects:\n  'old.md':\n    to: 'new.md'\n    many:\n      - 'a.md'"),
			("many-with-unknown-key", "redirects:\n  'old.md':\n    to: 'new.md'\n    many:\n      - to: 'a.md'\n        anchors: { 'x': 'y' }\n        extra: 1")
		).Should().MatchSnapshot();

	[Test]
	public void Read_UnknownKeys_AreReportedAsErrors() =>
		Table(
			("unknown-top-level-key", "redirects:\n  'old.md': 'new.md'\nredirect_rules: {}"),
			("misspelled-top-level-key", "redirect:\n  'old.md': 'new.md'"),
			("unknown-key-in-an-entry", "redirects:\n  'old.md':\n    to: 'new.md'\n    anchor: { 'a': 'b' }"),
			("misspelled-to", "redirects:\n  'old.md':\n    too: 'new.md'"),
			("only-unknown-keys-in-an-entry", "redirects:\n  'old.md':\n    foo: bar")
		).Should().MatchSnapshot();

	[Test]
	public void Read_EmptyAndMalformedFiles_ShowTheEdges() =>
		Table(
			("empty-redirects-mapping", "redirects: {}"),
			("redirects-is-null", "redirects:"),
			("redirects-is-a-list", "redirects:\n  - 'old.md'"),
			("redirects-is-a-scalar", "redirects: 'old.md'"),
			("empty-file", ""),
			("only-a-comment", "# nothing here"),
			("top-level-list", "- a\n- b"),
			("top-level-scalar", "just text"),
			("invalid-yaml", "redirects:\n  'old.md': [unterminated"),
			("tabs-for-indentation", "redirects:\n\t'old.md': 'new.md'")
		).Should().MatchSnapshot();

	[Test]
	public void Read_MissingFile_LeavesTheRedirectsUnset()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/docset.yml", new MockFileData(string.Empty));
		var collector = new CapturingDiagnosticsCollector();
		var context = new Context
		{
			Collector = collector,
			DocumentationSourceDirectory = fileSystem.DirectoryInfo.New("/docs"),
			Git = GitCheckoutInformation.Unavailable,
			ReadFileSystem = DocumentationFileSystem.Resolve(
				fileSystem.DirectoryInfo.New("/docs"),
				new DocumentationScopeOptions { Inner = fileSystem, ConfigurationFile = "/docs/docset.yml" }
			),
			WriteFileSystem = new DocumentationWriteFileSystem(
				fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
				null,
				fileSystem
			),
			ConfigurationPath = fileSystem.FileInfo.New("/docs/docset.yml"),
			OutputDirectory = fileSystem.DirectoryInfo.New("/.artifacts"),
			BuildType = BuildType.Isolated,
			Environment = SystemEnvironmentVariables.Instance
		};

		var file = new RedirectFile(context);

		file.Redirects.Should().BeNull();
		file.Source.Name.Should().Be("redirects.yml");
		collector.Captured.ToSnapshotText().Should().MatchInlineSnapshot("");
	}

	[Test]
	public void Read_UnderscoreConfiguration_ReadsTheUnderscoredFileName()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/_docset.yml", new MockFileData(string.Empty));
		fileSystem.AddFile("/docs/_redirects.yml", new MockFileData("redirects:\n  'old.md': 'new.md'"));
		fileSystem.AddFile("/docs/redirects.yml", new MockFileData("redirects:\n  'ignored.md': 'new.md'"));
		var context = new Context
		{
			Collector = new CapturingDiagnosticsCollector(),
			DocumentationSourceDirectory = fileSystem.DirectoryInfo.New("/docs"),
			Git = GitCheckoutInformation.Unavailable,
			ReadFileSystem = DocumentationFileSystem.Resolve(
				fileSystem.DirectoryInfo.New("/docs"),
				new DocumentationScopeOptions { Inner = fileSystem, ConfigurationFile = "/docs/_docset.yml" }
			),
			WriteFileSystem = new DocumentationWriteFileSystem(
				fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
				null,
				fileSystem
			),
			ConfigurationPath = fileSystem.FileInfo.New("/docs/_docset.yml"),
			OutputDirectory = fileSystem.DirectoryInfo.New("/.artifacts"),
			BuildType = BuildType.Isolated,
			Environment = SystemEnvironmentVariables.Instance
		};

		var file = new RedirectFile(context);

		file.Source.Name.Should().Be("_redirects.yml");
		file.Redirects.Should().NotBeNull().And.ContainKey("old.md").And.NotContainKey("ignored.md");
	}
}
