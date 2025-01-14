// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.SettingsInclusion;

public class IncludeTests(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
$$"""
:::{settings} /{{SettingsPath.Replace("docs/source/", "")}}
:::
"""
)
{
	private static readonly string SettingsPath =
		"docs/source/syntax/kibana-alerting-action-settings.yml";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var realSettingsPath = Path.Combine(Paths.Root.FullName, SettingsPath);
		// language=markdown
		var inclusion = System.IO.File.ReadAllText(realSettingsPath);
		fileSystem.AddFile(SettingsPath, inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void IncludesInclusionHtml() =>
		Html.Should()
			.Contain("xpack.encryptedSavedObjects.encryptionKey");
}
public class RandomFileEmitsAnError(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _snippets/test.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/source/_snippets/test.md", inclusion);
	}

	[Fact]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.FirstOrDefault().File.Should().NotEndWith("test.md");
		Collector.Diagnostics.Should()
			.OnlyContain(d => d.Message.Contains("Can not be parsed as a valid settings file"));
	}
}
