// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Settings;
using Elastic.Markdown.Tests.Directives;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.SettingsInclusion;

[InheritsTests]
public class IncludeTests() : DirectiveTest<SettingsBlock>(
	$$"""
:::{settings} /{{SettingsPath.Replace("docs/", "")}}
:::
"""
)
{
	private static readonly string SettingsPath = "docs/syntax/kibana-alerting-action-settings.yml";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var realSettingsPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, SettingsPath);
		// language=markdown
		var inclusion = System.IO.File.ReadAllText(realSettingsPath);
		fileSystem.AddFile(SettingsPath, inclusion);
	}

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void HasNoErrors()
	{
		// `platforms` is unmodelled: reported once per file as a hint, never as an error.
		var diagnostic = Collector.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Severity.Should().Be(Severity.Hint);
		diagnostic.Message.Should().Contain("'platforms' is not supported");
		diagnostic.File.Should().EndWith("kibana-alerting-action-settings.yml");
	}

	[Test]
	public void IncludesInclusionHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class RandomFileEmitsAnError() : DirectiveTest<SettingsBlock>("""
:::{settings} _snippets/test.md
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.FirstOrDefault().File.Should().NotEndWith("test.md");
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("Can not be parsed as a valid settings file"));
	}
}

[InheritsTests]
public class NewSchemaRendersMetadataAndNestedSettings() : DirectiveTest<SettingsBlock>("""
:::{settings} _settings/new-schema.yml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/new-schema.yml",
			"""
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
"""
		);
	}

	[Test]
	public void GeneratesHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class LegacySourceBlocksRenderAsMarkdownCode() : DirectiveTest<SettingsBlock>("""
:::{settings} _settings/legacy-source.yml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/legacy-source.yml",
			"""
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
"""
		);
	}

	[Test]
	public void RendersAsFencedCodeBlock() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<h2 id="example">Example</h2>
			<dl>
				<dt id="xpack-legacy-example">xpack.legacy.example
				</dt>
				<dd>
					<p>Example code:</p>
					<div class="highlight-yaml notranslate">
						<div class="highlight">
							<pre><code class="language-yaml">xpack.legacy.example:
			  enabled: true
			</code>		</pre>
						</div>
					</div>
				</dd>
			</dl>
			""",
			DocsHtml.Body()
		);
}

[InheritsTests]
public class SettingsTopMatterAndTitlesRender() : DirectiveTest<SettingsBlock>("""
:::{settings} _settings/top-matter.yml
::::
""")
{
	protected override IReadOnlyList<string>? GetDocsetProducts() => ["kibana"];

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/top-matter.yml",
			"""
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
"""
		);
	}

	[Test]
	public void RendersPageDescriptionNotesAndInterpolatedGroupTitle() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class SettingsApplicabilityRowsPreferUsefulBadges() : DirectiveTest<SettingsBlock>(
	"""
:::{settings} _settings/applicability-rows.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/applicability-rows.yml",
			"""
groups:
  - group: Example
    settings:
      - setting: xpack.sample.noop
        description: Generic stack GA should not render a stack badge.
        applies_to:
          stack: ga
          self: ga
          ech: unavailable
"""
		);
	}

	[Test]
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
[InheritsTests]
public class DeploymentFilterEchOnKibanaGeneralSettings() : DirectiveTest<SettingsBlock>(
	$$"""
:::{settings} /{{GeneralSettingsPath.Replace("docs/", "")}}
:deployment: ech
:::
"""
)
{
	private static readonly string GeneralSettingsPath = "docs/testing/kibana-general-settings.yml";
	private static readonly string GeneralSettingsRealPath = "docs-tests/kibana-general-settings.yml";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var fullPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, GeneralSettingsRealPath);
		fileSystem.AddFile(GeneralSettingsPath, System.IO.File.ReadAllText(fullPath));
	}

	[Test]
	public void GeneratesHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// When a setting has applies_to but ECH is not mentioned at all (only self: ga),
/// it must be treated as unavailable for ECH — "missing means unavailable".
/// Uses the real kibana-general-settings.yml which has self-only and ech:unavailable patterns.
/// </summary>
[InheritsTests]
public class DeploymentFilterEchMissingMeansUnavailable() : DirectiveTest<SettingsBlock>(
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
		fileSystem.AddFile(
			"docs/_settings/self-only.yml",
			"""
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
"""
		);
	}

	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<h2 id="example">Example</h2>
		<dl>
			<dt id="ech-explicit-setting">ech.explicit.setting
				<span class="applies applies-inline">
				</span>
			</dt>
			<dd>
				<p class="settings-supported-on">
					<strong>Supported on:</strong>
					<span class="applies applies-inline">
						<applies-to-popover badge-key="ECH" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Hosted\u003C/strong\u003E lets you manage and configure one or more deployments of the versioned Elastic Stack, hosted on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="true"></applies-to-popover>
						<applies-to-popover badge-key="Self-managed" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003ESelf-managed\u003C/strong\u003E deployments are Elastic Stack deployments managed without the assistance of an orchestrator.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
					</span>
				</p>
				<p>ECH is explicitly listed as ga.</p>
			</dd>
			<dt id="no-applies-to-setting">no.applies.to.setting
			</dt>
			<dd>
				<p>No applies_to at all — universally available.</p>
			</dd>
		</dl>
		""",
			DocsHtml.Body()
		);
}

[InheritsTests]
public class DeploymentFilterWithUnknownValueEmitsWarning() : DirectiveTest<SettingsBlock>(
	$$"""
:::{settings} /{{GeneralSettingsPath.Replace("docs/", "")}}
:deployment: invalid-deployment
:::
"""
)
{
	private static readonly string GeneralSettingsPath = "docs/testing/kibana-general-settings.yml";
	private static readonly string GeneralSettingsRealPath = "docs-tests/kibana-general-settings.yml";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var fullPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, GeneralSettingsRealPath);
		fileSystem.AddFile(GeneralSettingsPath, System.IO.File.ReadAllText(fullPath));
	}

	[Test]
	public void EmitsWarning() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
		warning {repo}/docs/index.md:3 Unknown deployment filter 'invalid-deployment'. Valid values are: ech, ece, eck, self.
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:3 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:3 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:3 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:3 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {hosted-ems} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:3 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:9 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:6 Substitution key {ecloud} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {kib} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:1 Substitution key {es} is undefined
		error {repo}/docs/testing/kibana-general-settings.yml:2 Substitution key {ecloud} is undefined
		"""
			);

	[Test]
	public void StillRendersAllSettingsWhenFilterIsInvalid() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class AppliesToInlineRoleInDescriptionRendersAsBadge() : DirectiveTest<SettingsBlock>(
	"""
:::{settings} _settings/applies-to-in-description.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/applies-to-in-description.yml",
			"""
groups:
  - group: Example
    settings:
      - setting: xpack.sample.versioned
        description: |
          Behavior depends on version:

          * {applies_to}`stack: ga 9.2` Defaults to `model-a`.
          * {applies_to}`stack: ga 9.1` Defaults to `model-b`.
"""
		);
	}

	[Test]
	public void RendersAppliesToRoleAsBadgeNotLiteralText() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<h2 id="example">Example</h2>
		<dl>
			<dt id="xpack-sample-versioned">xpack.sample.versioned
			</dt>
			<dd>
				<p>Behavior depends on version:</p>
				<ul>
					<li>
						<span class="applies applies-inline">
							<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
						</span>
						Defaults to
						<code>model-a</code>.</li>
					<li>
						<span class="applies applies-inline">
							<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
						</span>
						Defaults to
						<code>model-b</code>.</li>
				</ul>
			</dd>
		</dl>
		""",
			DocsHtml.Body()
		);
}

/// <summary>
/// Reproduces the "Internationalization settings in Kibana" confusion: a setting whose
/// stack badge renders as "Planned" (because every stack entry targets a future version)
/// should not advertise itself as already supported on ECH or Self-managed.
/// The test stack current is 8.0.0 (see <see cref="TestHelpers.CreateConfigurationContext"/>),
/// so <c>stack: ga 9.5</c> is unreleased.
/// </summary>
[InheritsTests]
public class HidesSupportedOnLineWhenStackIsFullyPlanned() : DirectiveTest<SettingsBlock>(
	"""
:::{settings} _settings/stack-fully-planned.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/stack-fully-planned.yml",
			"""
groups:
  - group: Example
    settings:
      - setting: i18n.defaultLocale
        description: Locale setting planned for a future stack version.
        applies_to:
          stack: ga 99.99
          ech: ga
          self: ga
"""
		);
	}

	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<h2 id="example">Example</h2>
		<dl>
			<dt id="i18n-defaultlocale">i18n.defaultLocale
				<span class="applies applies-inline">
					<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
				</span>
			</dt>
			<dd>
				<p>Locale setting planned for a future stack version.</p>
			</dd>
		</dl>
		""",
			DocsHtml.Body()
		);
}

/// <summary>
/// A setting whose stack is released today (<c>stack: ga 7.0</c> with test current 8.0.0)
/// must continue to render the "Supported on" line with ECH and Self-managed badges.
/// </summary>
[InheritsTests]
public class KeepsSupportedOnLineWhenStackIsReleased() : DirectiveTest<SettingsBlock>(
	"""
:::{settings} _settings/stack-released.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/stack-released.yml",
			"""
groups:
  - group: Example
    settings:
      - setting: xpack.sample.released
        description: A setting that has been GA since 7.0.
        applies_to:
          stack: ga 7.0
          ech: ga
          self: ga
"""
		);
	}

	[Test]
	public void RendersSupportedOnLineWithBothBadges() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<h2 id="example">Example</h2>
		<dl>
			<dt id="xpack-sample-released">xpack.sample.released
				<span class="applies applies-inline">
					<applies-to-popover badge-key="Stack" badge-version="7.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
				</span>
			</dt>
			<dd>
				<p class="settings-supported-on">
					<strong>Supported on:</strong>
					<span class="applies applies-inline">
						<applies-to-popover badge-key="ECH" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Hosted\u003C/strong\u003E lets you manage and configure one or more deployments of the versioned Elastic Stack, hosted on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="true"></applies-to-popover>
						<applies-to-popover badge-key="Self-managed" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003ESelf-managed\u003C/strong\u003E deployments are Elastic Stack deployments managed without the assistance of an orchestrator.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
					</span>
				</p>
				<p>A setting that has been GA since 7.0.</p>
			</dd>
		</dl>
		""",
			DocsHtml.Body()
		);
}

/// <summary>
/// A setting whose stack collection mixes past and future versions
/// (e.g. <c>stack: ga 7.0, deprecated 9.0</c>) is still usable today,
/// so the "Supported on" line must remain visible.
/// </summary>
[InheritsTests]
public class KeepsSupportedOnLineWhenStackHasMixedReleaseAndFutureVersions() : DirectiveTest<SettingsBlock>(
	"""
:::{settings} _settings/stack-mixed-versions.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/stack-mixed-versions.yml",
			"""
groups:
  - group: Example
    settings:
      - setting: xpack.sample.mixed
        description: GA since an existing version, scheduled for deprecation in a future version.
        applies_to:
          stack: ga 7.0, deprecated 99.0
          ech: ga
          self: ga
"""
		);
	}

	[Test]
	public void RendersSupportedOnLine() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<h2 id="example">Example</h2>
		<dl>
			<dt id="xpack-sample-mixed">xpack.sample.mixed
				<span class="applies applies-inline">
					<applies-to-popover badge-key="Stack" badge-version="7.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="true" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for deprecation&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be deprecated in a future Elastic Stack update. Subject to changes.&quot;},{&quot;text&quot;:&quot;Generally available since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
				</span>
			</dt>
			<dd>
				<p class="settings-supported-on">
					<strong>Supported on:</strong>
					<span class="applies applies-inline">
						<applies-to-popover badge-key="ECH" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Hosted\u003C/strong\u003E lets you manage and configure one or more deployments of the versioned Elastic Stack, hosted on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="true"></applies-to-popover>
						<applies-to-popover badge-key="Self-managed" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003ESelf-managed\u003C/strong\u003E deployments are Elastic Stack deployments managed without the assistance of an orchestrator.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
					</span>
				</p>
				<p>GA since an existing version, scheduled for deprecation in a future version.</p>
			</dd>
		</dl>
		""",
			DocsHtml.Body()
		);
}

/// <summary>
/// Settings YAML anchors an <c>applies_to</c> block once and reuses it with an alias
/// (<c>&amp;shared</c> / <c>*shared</c>) to avoid repeating it on every setting it covers.
/// The generated reader must resolve the alias instead of throwing on it.
/// </summary>
[InheritsTests]
public class SettingReusesAnAnchoredAppliesToBlock() : DirectiveTest<SettingsBlock>(
	"""
:::{settings} _settings/shared-applies-to.yml
::::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=yaml
		fileSystem.AddFile(
			"docs/_settings/shared-applies-to.yml",
			"""
groups:
  - group: Example
    settings:
      - setting: xpack.sample.one
        description: First setting.
        applies_to: &shared
          stack: ga 7.0
      - setting: xpack.sample.two
        description: Second setting, reuses the same applies_to block as the one above.
        applies_to: *shared
"""
		);
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void RendersBothSettings() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}
