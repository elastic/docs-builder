// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Tests.DocSet;

public class RedirectValidationTests
{
	private static IReadOnlyCollection<Diagnostic> Build(string redirectsYaml)
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				{ "docs/index.md", new MockFileData("# Index") },
				{ "docs/new-page.md", new MockFileData("# New page") },
				{ "docs/redirects.yml", new MockFileData(redirectsYaml) }
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var root = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/"));
		fileSystem.GenerateDocSetYaml(root);
		var collector = new TestDiagnosticsCollector();
		var context = new BuildContext(
			collector,
			TestHelpers.CreateDocumentationFileSystem(fileSystem, root),
			TestHelpers.CreateConfigurationContext(fileSystem)
		);
		_ = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver());
		return collector.Diagnostics;
	}

	[Test]
	public void ManyTargetMissing_WithoutTopLevelTo_IsAnError()
	{
		var diagnostics = Build(
			"redirects:\n  old-page.md:\n    many:\n      - to: missing.md\n        anchors:\n          old-anchor: new-anchor\n"
		);
		foreach (var d in diagnostics)
			diagnostics
				.Should()
				.ContainSingle(d => d.Message == "Redirect old-page.md points to missing.md which does not exist")
				.Which
				.Severity
				.Should()
				.Be(Severity.Error);
	}

	[Test]
	public void ManyTargetMissing_NextToTopLevelTo_IsAWarning()
	{
		var diagnostics = Build(
			"redirects:\n  old-page.md:\n    to: new-page.md\n    many:\n      - to: missing.md\n        anchors:\n          old-anchor: new-anchor\n"
		);

		diagnostics
			.Should()
			.ContainSingle(d => d.Message == "Redirect old-page.md points to missing.md which does not exist")
			.Which
			.Severity
			.Should()
			.Be(Severity.Warning);
		diagnostics.Should().NotContain(d => d.Severity == Severity.Error);
	}
}
