// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Settings;
using Elastic.Markdown.Tests.Directives;

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
		var realSettingsPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, SettingsPath);
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
          stack: ga 7.0
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

	[Fact]
	public void DotsInSettingNamesAreHyphensInAnchors()
	{
		Html.Should().Contain("id=\"xpack-actions-customhostsettings\"");
		Html.Should().NotContain("id=\"xpack.actions.customhostsettings\"");
	}

	[Fact]
	public void NestedSettingAnchorIncludesParentPrefix()
	{
		Html.Should().Contain("id=\"xpack-actions-customhostsettingsn-url\"");
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

public class SettingsTopMatterAndTitlesRender(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/top-matter.yml
::::
"""
)
{
	protected override IReadOnlyList<string>? GetDocsetProducts() => ["kibana"];

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/top-matter.yml", """
product: Kibana
collection: Test collection
page_description: |
  Read the [preconfigured connectors](/reference/connectors-kibana/pre-configured-connectors.md) guide.
note: "Top-level note for {{product.kibana}}."
groups:
  - group: General {{product.kibana}} settings
    note: "Group-level note for {{product.kibana}}."
    settings:
      - setting: xpack.sample.enabled
        description: "Enables sample behavior."
""");
	}

	[Fact]
	public void RendersPageDescriptionNotesAndInterpolatedGroupTitle()
	{
		Html.Should().Contain("General Kibana settings");
		Html.Should().Contain("Top-level note for Kibana.");
		Html.Should().Contain("Group-level note for Kibana.");
		Html.Should().Contain("https://www.elastic.co/docs/reference/kibana/connectors-kibana/pre-configured-connectors");
	}
}

public class SettingsApplicabilityRowsPreferUsefulBadges(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/applicability-rows.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/applicability-rows.yml", """
groups:
  - group: Example
    settings:
      - setting: xpack.sample.noop
        description: Generic stack GA should not render a stack badge.
        applies_to:
          stack: ga
          self: ga
          ech: unavailable
""");
	}

	[Fact]
	public void DoesNotRenderGenericStackBadgeOrUnavailableSupportedOnEntry()
	{
		var config = TestHelpers.CreateConfigurationContext(new MockFileSystem());
		var viewModel = new SettingsViewModel
		{
			SettingsCollection = new YamlSettings(),
			RenderMarkdown = s => s,
			VersionsConfig = config.VersionsConfiguration,
			GroupHeadingLevel = 2
		};
		var appliesTo = new Elastic.Documentation.AppliesTo.ApplicableTo
		{
			Stack = (Elastic.Documentation.AppliesTo.AppliesCollection)"ga",
			Deployment = new Elastic.Documentation.AppliesTo.DeploymentApplicability
			{
				Self = (Elastic.Documentation.AppliesTo.AppliesCollection)"ga",
				Ess = (Elastic.Documentation.AppliesTo.AppliesCollection)"unavailable"
			}
		};

		viewModel.RenderStackRowBadges(appliesTo).Should().NotContain("badge-key=\"Stack\"");
		viewModel.RenderSupportedOnBadges(appliesTo).Should().Contain("Self-managed");
		viewModel.RenderSupportedOnBadges(appliesTo).Should().NotContain("ECH");

		Html.Should().Contain("badge-key=\"Self-managed\"");
		Html.Should().NotContain("badge-key=\"ECH\"");
		Html.Should().NotContain("Unavailable");
	}
}

/// <summary>
/// Uses the real kibana-general-settings.yml fixture.
/// In that file every setting has an explicit applies_to with either ech: ga or ech: unavailable.
/// - execution_context.enabled → ech: ga, self: ga  → ECH visible
/// - console.ui.enabled        → ech: unavailable, self: ga → ECH hidden
/// Settings with no applies_to at all (universally available) are also visible.
/// </summary>
public class DeploymentFilterEchOnKibanaGeneralSettings(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
$$"""
:::{settings} /{{GeneralSettingsPath.Replace("docs/", "")}}
:deployment: ech
:::
"""
)
{
	private static readonly string GeneralSettingsPath = "docs/testing/kibana-general-settings.yml";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var fullPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, GeneralSettingsPath);
		fileSystem.AddFile(GeneralSettingsPath, System.IO.File.ReadAllText(fullPath));
	}

	[Fact]
	public void ShowsEchGaSetting() =>
		Html.Should().Contain("execution_context.enabled");

	[Fact]
	public void HidesEchUnavailableSetting() =>
		Html.Should().NotContain("console.ui.enabled");
}

/// <summary>
/// When a setting has applies_to but ECH is not mentioned at all (only self: ga),
/// it must be treated as unavailable for ECH — "missing means unavailable".
/// Uses the real kibana-general-settings.yml which has self-only and ech:unavailable patterns.
/// </summary>
public class DeploymentFilterEchMissingMeansUnavailable(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/self-only.yml
:deployment: ech
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/self-only.yml", """
groups:
  - group: Example
    settings:
      - setting: self.only.setting
        description: Only self is listed — ech is missing so unavailable.
        applies_to:
          self: ga
      - setting: ech.explicit.setting
        description: ECH is explicitly listed as ga.
        applies_to:
          ech: ga
          self: ga
      - setting: no.applies.to.setting
        description: No applies_to at all — universally available.
""");
	}

	[Fact]
	public void HidesSettingWhenEchIsMissing() =>
		Html.Should().NotContain("self.only.setting");

	[Fact]
	public void ShowsSettingWithExplicitEchGa() =>
		Html.Should().Contain("ech.explicit.setting");

	[Fact]
	public void ShowsSettingWithNoAppliesTo() =>
		Html.Should().Contain("no.applies.to.setting");
}

public class DeploymentFilterWithUnknownValueEmitsWarning(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
$$"""
:::{settings} /{{GeneralSettingsPath.Replace("docs/", "")}}
:deployment: invalid-deployment
:::
"""
)
{
	private static readonly string GeneralSettingsPath = "docs/testing/kibana-general-settings.yml";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var fullPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, GeneralSettingsPath);
		fileSystem.AddFile(GeneralSettingsPath, System.IO.File.ReadAllText(fullPath));
	}

	[Fact]
	public void EmitsWarning() =>
		Collector.Diagnostics.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("invalid-deployment"));

	[Fact]
	public void StillRendersAllSettingsWhenFilterIsInvalid() =>
		Html.Should().Contain("execution_context.enabled");
}

public class AppliesToInlineRoleInDescriptionRendersAsBadge(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/applies-to-in-description.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/applies-to-in-description.yml", """
groups:
  - group: Example
    settings:
      - setting: xpack.sample.versioned
        description: |
          Behavior depends on version:

          * {applies_to}`stack: ga 9.2` Defaults to `model-a`.
          * {applies_to}`stack: ga 9.1` Defaults to `model-b`.
""");
	}

	[Fact]
	public void RendersAppliesToRoleAsBadgeNotLiteralText()
	{
		Html.Should().Contain("applies-to-popover");
		Html.Should().NotContain("Applies to (stack: ga 9.2)");
		Html.Should().NotContain("Applies to (stack: ga 9.1)");
	}
}

/// <summary>
/// Reproduces the "Internationalization settings in Kibana" confusion: a setting whose
/// stack badge renders as "Planned" (because every stack entry targets a future version)
/// should not advertise itself as already supported on ECH or Self-managed.
/// The test stack current is 8.0.0 (see <see cref="TestHelpers.CreateConfigurationContext"/>),
/// so <c>stack: ga 9.5</c> is unreleased.
/// </summary>
public class HidesSupportedOnLineWhenStackIsFullyPlanned(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/stack-fully-planned.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/stack-fully-planned.yml", """
groups:
  - group: Example
    settings:
      - setting: i18n.defaultLocale
        description: Locale setting planned for a future stack version.
        applies_to:
          stack: ga 99.99
          ech: ga
          self: ga
""");
	}

	[Fact]
	public void RendersPlannedStackBadge() =>
		Html.Should().Contain("badge-key=\"Stack\"").And.Contain("Planned");

	[Fact]
	public void DoesNotRenderSupportedOnLine()
	{
		Html.Should().NotContain("settings-supported-on");
		Html.Should().NotContain("Supported on:");
	}

	[Fact]
	public void DoesNotRenderEchOrSelfManagedBadges()
	{
		Html.Should().NotContain("badge-key=\"ECH\"");
		Html.Should().NotContain("badge-key=\"Self-managed\"");
	}
}

/// <summary>
/// A setting whose stack is released today (<c>stack: ga 7.0</c> with test current 8.0.0)
/// must continue to render the "Supported on" line with ECH and Self-managed badges.
/// </summary>
public class KeepsSupportedOnLineWhenStackIsReleased(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/stack-released.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/stack-released.yml", """
groups:
  - group: Example
    settings:
      - setting: xpack.sample.released
        description: A setting that has been GA since 7.0.
        applies_to:
          stack: ga 7.0
          ech: ga
          self: ga
""");
	}

	[Fact]
	public void RendersSupportedOnLineWithBothBadges()
	{
		Html.Should().Contain("settings-supported-on");
		Html.Should().Contain("badge-key=\"ECH\"");
		Html.Should().Contain("badge-key=\"Self-managed\"");
	}
}

/// <summary>
/// A setting whose stack collection mixes past and future versions
/// (e.g. <c>stack: ga 7.0, deprecated 9.0</c>) is still usable today,
/// so the "Supported on" line must remain visible.
/// </summary>
public class KeepsSupportedOnLineWhenStackHasMixedReleaseAndFutureVersions(ITestOutputHelper output) : DirectiveTest<SettingsBlock>(output,
"""
:::{settings} _settings/stack-mixed-versions.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile("docs/_settings/stack-mixed-versions.yml", """
groups:
  - group: Example
    settings:
      - setting: xpack.sample.mixed
        description: GA since an existing version, scheduled for deprecation in a future version.
        applies_to:
          stack: ga 7.0, deprecated 99.0
          ech: ga
          self: ga
""");
	}

	[Fact]
	public void RendersSupportedOnLine()
	{
		Html.Should().Contain("settings-supported-on");
		Html.Should().Contain("badge-key=\"ECH\"");
		Html.Should().Contain("badge-key=\"Self-managed\"");
	}
}
