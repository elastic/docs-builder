// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Settings;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.SettingsInclusion;

public class IncludeTests(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
$$"""
:::{settings} /{{SettingsPath.Replace("docs/", "")}}
:::
"""
)
{
	private static readonly string SettingsPath =
		"docs/syntax/kibana-alerting-action-settings.yml";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var realSettingsPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, SettingsPath);
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
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
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

public class NewSchemaRendersMetadataAndNestedSettings(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/new-schema.yml
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/new-schema.yml", """
product: Kibana
collection: Test collection
groups:
  - group: General settings
    settings:
      - setting: xpack.actions.customHostSettings
        description: Parent setting description.
        datatype: object
        default: "[]"
        applies_to:
          stack: ga 9.2
        options:
          - option: strict
            description: Strict mode.
        settings:
          - setting: "[n].url"
            description: Child setting description.
            datatype: string
""");
	}

	[Fact]
	public void RendersAppliesToAndMetadata()
	{
		Html.Should().Contain("applies-to-popover");
		Html.Should().Contain("settings-supported-on");
		Html.Should().Contain("<strong>Datatype:</strong>");
		Html.Should().Contain("<strong>Default:</strong>");
		Html.Should().Contain("<strong>Options:</strong>");
	}

	[Fact]
	public void RendersNestedSettingName()
	{
		Html.Should().Contain("xpack.actions.customHostSettings[n].url");
	}
}

public class LegacySourceBlocksRenderAsMarkdownCode(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/legacy-source.yml
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/legacy-source.yml", """
groups:
  - group: Example
    settings:
      - setting: xpack.legacy.example
        description: |
          Example code:
          
          [source,yaml]
          --
          xpack.legacy.example:
            enabled: true
          --
""");
	}

	[Fact]
	public void RendersAsFencedCodeBlock()
	{
		Html.Should().Contain("language-yaml");
		Html.Should().NotContain("[source,yaml]");
	}
}
