// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Myst.Directives.Wizard;

namespace Elastic.Markdown.Tests.Directives;

internal static class WizardYaml
{
	/// <summary>
	/// A small but complete config: a self-managed branch that sets a fixed deployment, a cloud branch that scores
	/// between two, a multi-select, and two variables. The pieces tests tweak are parameters.
	/// </summary>
	public static string Valid(
		string extraOption = "",
		string nextAfterGoal = "done",
		string label = "Cloud",
		string cta = "",
		string heading = "{solution} on {deployment}",
		string variables = ""
	) =>
		$$"""
meta:
  title: Which Elastic is right for you?
  intro: Answer a few questions.
  start_button: Let's go
  start: where
  progress_label: Your progress
screens:
  where:
    type: question
    progress: 10
    question: Where do you want to run it?
    options:
      - label: "{{label}}"
        set: { family: cloud }
        next: goal
      - label: On my own servers
        set: { family: self, deployment: self_download }
        next: goal
{{extraOption}}
  goal:
    type: question
    progress: 40
    question: What do you want to do?
    options:
      - label: Search
        set: { solution: elasticsearch }
        next: route
      - label: Monitor
        set: { solution: observability }
        next: route
  route:
    type: route
    rules:
      - when: { key: family, equals: self }
        next: {{nextAfterGoal}}
    else: control
  control:
    type: question
    progress: 70
    question: Hands-off?
    options:
      - label: Yes
        add: { serverless: 2 }
        next: reqs
      - label: No
        add: { hosted: 2 }
        next: reqs
  reqs:
    type: question
    multi: true
    progress: 90
    question: Requirements?
    next: done
    options:
      - label: A region
        flag: check_region
      - label: Nothing special
        exclusive: true
  done:
    type: result
    progress: 100
results:
  heading: "{{heading}}"
  variables:
{{(variables.Length > 0 ? variables : DefaultVariables)}}
  cta:
{{(cta.Length > 0 ? cta : DefaultCta)}}
  flag_notes:
    check_region: Check your region is available.
""";

	public const string DefaultVariablesPublic = DefaultVariables;

	private const string DefaultVariables =
		"""
    solution:
      answer: solution
      items:
        elasticsearch: { name: Elasticsearch, docs: { title: Elasticsearch docs, href: https://www.elastic.co/docs/solutions/search } }
        observability: { name: Observability, docs: { title: Observability docs, href: https://www.elastic.co/docs/solutions/observability } }
    deployment:
      answer: deployment
      score: [serverless, hosted]
      items:
        serverless: { name: Elastic Cloud Serverless, docs: { title: Serverless, href: /deploy-manage/deploy/elastic-cloud/serverless } }
        hosted: { name: Elastic Cloud Hosted, docs: { title: Hosted, href: /deploy-manage/deploy/elastic-cloud/cloud-hosted } }
        self_download: { name: self-managed Elastic, docs: { title: Self-managed, href: /deploy-manage/deploy/self-managed } }
""";

	private const string DefaultCta =
		"""
    - when: { key: deployment, starts_with: self }
      label: Read the install guide
      href_from: deployment
    - label: "Create your {solution} project"
      href: https://cloud.elastic.co/projects/create
""";

	/// <summary>A tiny config with nothing to do with deployments, to show the directive is generic.</summary>
	public const string ContentTypes =
		"""
meta:
  title: Which type of page should I write?
  start: purpose
screens:
  purpose:
    type: question
    progress: 50
    question: What does the reader need?
    options:
      - label: To understand something
        set: { type: overview }
        next: done
      - label: To get something done
        set: { type: how-to }
        next: done
  done:
    type: result
    progress: 100
results:
  heading: "Write {type}"
  variables:
    type:
      answer: type
      items:
        overview: { name: an overview, docs: { title: Overviews, href: https://www.elastic.co/docs/contribute-docs/content-types/overviews } }
        how-to: { name: a how-to guide, docs: { title: How-tos, href: https://www.elastic.co/docs/contribute-docs/content-types/how-tos } }
  cta:
    - label: "Read the guidelines for {type}"
      href_from: type
""";
}

[InheritsTests]
public class WizardValidConfigTests() : DirectiveTest<WizardBlock>("""
:::{wizard} wizard.yaml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/wizard.yaml", new MockFileData(WizardYaml.Valid()));

	[Test]
	public void ParsesTheBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsDirectiveName() => Block!.Directive.Should().Be("wizard");

	[Test]
	public void LoadsTheConfig()
	{
		Block!.Config.Should().NotBeNull();
		Block.Config!.Meta.Start.Should().Be("where");
		Block.Config.Screens.Should().ContainKeys("where", "goal", "route", "control", "reqs", "done");
		Block.Config.Screens["reqs"].Multi.Should().BeTrue();
		Block.Config.Screens["where"].Options[1].Set.Should().Contain("deployment", "self_download");
		Block.Config.Screens["control"].Options[0].Add.Should().Contain("serverless", 2);
		Block.Config.Results.Variables.Should().ContainKeys("solution", "deployment");
		Block.Config.Results.Variables["deployment"].Score.Should().Equal("serverless", "hosted");
		Block.Config.Results.Cta.Should().HaveCount(2);
		Block.Config.Results.Cta[1].When.Should().BeNull();
	}

	[Test]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	[Test]
	public void EmitsSnakeCaseJson()
	{
		Block!.ConfigJson.Should().Contain("\"progress_label\"");
		Block.ConfigJson.Should().Contain("\"flag_notes\"");
		Block.ConfigJson.Should().Contain("\"href_from\":\"deployment\"");
		Block.ConfigJson.Should().Contain("\"else\":\"control\"");
		Block.ConfigJson.Should().Contain("\"equals\":\"self\"");
		Block.ConfigJson.Should().Contain("\"starts_with\":\"self\"");
	}

	[Test]
	public void RendersTheWebComponentWithEmbeddedJson()
	{
		Html.Should().Contain("<docs-wizard>");
		Html.Should().Contain("data-wizard-config");
		Html.Should().Contain("application/json");
	}
}

[InheritsTests]
public class WizardIsGenericTests() : DirectiveTest<WizardBlock>("""
:::{wizard} wizard.yaml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/wizard.yaml", new MockFileData(WizardYaml.ContentTypes));

	[Test]
	public void AcceptsAWizardThatHasNothingToDoWithDeployments()
	{
		Collector.Diagnostics.Should().BeEmpty();
		Block!.Config.Should().NotBeNull();
		Block.Config!.Results.Variables.Should().ContainKey("type");
		Html.Should().Contain("<docs-wizard>");
	}
}

[InheritsTests]
public class WizardMissingArgumentTests() : DirectiveTest<WizardBlock>("""
:::{wizard}
:::
""")
{
	[Test]
	public void EmitsErrorForMissingArgument()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("requires an argument"));
	}
}

[InheritsTests]
public class WizardMissingFileTests() : DirectiveTest<WizardBlock>("""
:::{wizard} nope.yaml
:::
""")
{
	[Test]
	public void EmitsErrorForMissingFile()
	{
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("does not exist"));
		Block!.Config.Should().BeNull();
	}

	[Test]
	public void RendersNothing() => Html.Should().NotContain("<docs-wizard>");
}

/// <summary>Each test builds a config with one thing wrong and checks the build reports exactly that.</summary>
public abstract class WizardInvalidConfigTests(string yaml, string expectedMessage) : DirectiveTest<WizardBlock>(
	"""
:::{wizard} wizard.yaml
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile("docs/wizard.yaml", new MockFileData(yaml));

	[Test]
	public void FailsTheBuildWithAHelpfulMessage()
	{
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains(expectedMessage));
		Block!.Config.Should().BeNull();
		Html.Should().NotContain("<docs-wizard>");
	}
}

[InheritsTests]
public class WizardUnknownScreenTests() : WizardInvalidConfigTests(WizardYaml.Valid(nextAfterGoal: "nowhere"), "unknown screen `nowhere`");

// The classic mistake: an unquoted comma inside a flow-style label splits it into a stray key.
[InheritsTests]
public class WizardUnknownKeyTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(extraOption: "      - { label: Yes, on Elastic Cloud, next: goal }"),
	""
);

[InheritsTests]
public class WizardUnsafeLinkTests() : WizardInvalidConfigTests(
	WizardYaml.Valid().Replace("https://cloud.elastic.co/projects/create", "javascript:alert(1)"),
	"starts with `https://` or `/`"
);

[InheritsTests]
public class WizardUnknownHeadingVariableTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(heading: "{solution} on {platform}"),
	"uses `{platform}`"
);

[InheritsTests]
public class WizardMissingFallbackCtaTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(
		cta: """
    - when: { key: deployment, starts_with: self }
      label: Read the install guide
      href_from: deployment
"""
	),
	"must not have a `when`"
);

[InheritsTests]
public class WizardUnknownCtaVariableTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(cta: """
    - label: Go
      href_from: platform
"""),
	"`href_from: platform`, which is not a variable"
);

[InheritsTests]
public class WizardTypoedConditionKeyTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(
		cta: """
    - when: { key: deploymnet, equals: hosted }
      label: Create a deployment
      href: https://cloud.elastic.co/deployments/create
    - label: Go
      href: https://cloud.elastic.co/projects/create
"""
	),
	"`deploymnet`, which is neither an answer"
);

[InheritsTests]
public class WizardUnsetAnswerTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(
		variables: """
    solution:
      answer: solution
      items:
        elasticsearch: { name: Elasticsearch, docs: { title: Docs, href: https://www.elastic.co/docs/solutions/search } }
        observability: { name: Observability, docs: { title: Docs, href: https://www.elastic.co/docs/solutions/observability } }
    deployment:
      answer: deployment
      items:
        self_download: { name: self-managed Elastic, docs: { title: Self-managed, href: /deploy-manage/deploy/self-managed } }
"""
	),
	"without `deployment` being set"
);

[InheritsTests]
public class WizardItemMissingForAnswerTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(
		variables: """
    solution:
      answer: solution
      items:
        elasticsearch: { name: Elasticsearch, docs: { title: Docs, href: https://www.elastic.co/docs/solutions/search } }
    deployment:
      answer: deployment
      score: [serverless, hosted]
      items:
        serverless: { name: Serverless, docs: { title: Docs, href: /a } }
        hosted: { name: Hosted, docs: { title: Docs, href: /b } }
        self_download: { name: self-managed Elastic, docs: { title: Docs, href: /c } }
"""
	),
	"`solution` = `observability`, which has no entry"
);

[InheritsTests]
public class WizardRouteOnAnswerNothingSetsTests() : WizardInvalidConfigTests(
	WizardYaml.Valid().Replace("key: family", "key: famly"),
	"tests answer `famly`"
);

[InheritsTests]
public class WizardEscapingTests() : DirectiveTest<WizardBlock>("""
:::{wizard} wizard.yaml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/wizard.yaml", new MockFileData(WizardYaml.Valid(label: "</script><b>x</b>")));

	[Test]
	public void ConfigTextCannotCloseTheScriptElement()
	{
		Collector.Diagnostics.Should().BeEmpty();
		Block!.ConfigJson.Should().NotContain("</script>");
		Block.ConfigJson.Should().Contain("\\u003C/script\\u003E");
		Html.Should().NotContain("</script><b>");
	}
}

/// <summary>The sample configs that ship in the docs are real wizards, so they have to build cleanly: no errors and no warnings.</summary>
public abstract class WizardShippedSampleTests(string sampleFile) : DirectiveTest<WizardBlock>("""
:::{wizard} wizard.yaml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/wizard.yaml",
			new MockFileData(System.IO.File.ReadAllText(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "syntax", sampleFile)))
		);

	[Test]
	public void BuildsWithoutErrorsOrWarnings()
	{
		Collector.Diagnostics.Should().BeEmpty();
		Block!.Config.Should().NotBeNull();
		Html.Should().Contain("<docs-wizard>");
	}
}

[InheritsTests]
public class WizardDeploymentSampleTests() : WizardShippedSampleTests("wizard-deployment-sample.yaml")
{
	[Test]
	public void CoversTheFourSolutionsAndEveryWayToRunThem()
	{
		var variables = Block!.Config!.Results.Variables;
		variables["solution"].Items.Keys.Should().BeEquivalentTo("elasticsearch", "vectordb", "observability", "security");
		variables["deployment"].Items.Keys.Should().BeEquivalentTo("serverless", "hosted", "self_download", "self_eck", "self_ece");
		variables["tier"].Optional.Should().BeTrue();
		variables["tier"].When!.Equal.Should().Be("serverless");
	}

	[Test]
	public void ShowsTheFirstQuestionAsCardsWithTags()
	{
		var goal = Block!.Config!.Screens["goal"];
		goal.Layout.Should().Be("cards");
		goal.Options.Should().Contain(o => o.Tags.Contains("Chatbots"));
		goal
			.Options
			.SelectMany(o => o.Tags)
			.Should()
			.NotContain(
				["Vector Database", "Elasticsearch", "Observability", "Security"],
				"the first question is about what the reader wants to do, not product names"
			);
		goal.Options.Should().Contain(o => o.Wide);
	}
}

[InheritsTests]
public class WizardContentTypeSampleTests() : WizardShippedSampleTests("wizard-content-type-sample.yaml");

[InheritsTests]
public class WizardCardsAndTagsTests() : DirectiveTest<WizardBlock>("""
:::{wizard} wizard.yaml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var yaml = WizardYaml
			.Valid()
			.Replace("    question: What do you want to do?", "    layout: cards\n    question: What do you want to do?")
			.Replace("      - label: Search\n", "      - label: Search\n        tags: [Elasticsearch, Full-text]\n        wide: true\n");
		fileSystem.AddFile("docs/wizard.yaml", new MockFileData(yaml));
	}

	[Test]
	public void PassesLayoutTagsAndWideToTheComponent()
	{
		Collector.Diagnostics.Should().BeEmpty();
		Block!.ConfigJson.Should().Contain("\"layout\":\"cards\"");
		Block.ConfigJson.Should().Contain("\"tags\":[\"Elasticsearch\",\"Full-text\"]");
		Block.ConfigJson.Should().Contain("\"wide\":true");
	}
}

[InheritsTests]
public class WizardUnknownLayoutTests() : WizardInvalidConfigTests(
	WizardYaml.Valid().Replace("    question: What do you want to do?", "    layout: carousel\n    question: What do you want to do?"),
	"Use `list` or `cards`"
);

[InheritsTests]
public class WizardOptionalConditionalVariableTests() : DirectiveTest<WizardBlock>("""
:::{wizard} wizard.yaml
:::
""")
{
	// `tier` is only set on one path and only matters on Serverless, so it must not have to resolve on every path.
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var yaml = WizardYaml.Valid(
			extraOption: "      - label: Gold\n        set: { tier: gold }\n        next: goal",
			variables: WizardYaml.DefaultVariablesPublic +
				"""

    tier:
      answer: tier
      optional: true
      when: { key: deployment, equals: serverless }
      items:
        gold: { name: Gold, docs: { title: Tiers, href: https://www.elastic.co/docs/solutions/observability } }
"""
		);
		fileSystem.AddFile("docs/wizard.yaml", new MockFileData(yaml));
	}

	[Test]
	public void AcceptsAnOptionalVariableThatIsNotSetOnEveryPath()
	{
		Collector.Diagnostics.Should().BeEmpty();
		Block!.Config!.Results.Variables["tier"].Optional.Should().BeTrue();
		Block.ConfigJson.Should().Contain("\"optional\":true");
	}
}

[InheritsTests]
public class WizardRequiredVariableNotSetEverywhereTests() : WizardInvalidConfigTests(
	WizardYaml.Valid(
		extraOption: "      - label: Gold\n        set: { tier: gold }\n        next: goal",
		variables: WizardYaml.DefaultVariablesPublic +
			"""

    tier:
      answer: tier
      items:
        gold: { name: Gold, docs: { title: Tiers, href: https://www.elastic.co/docs/solutions/observability } }
"""
	),
	"without `tier` being set"
);

[InheritsTests]
public class WizardInertQuestionWarningTests() : DirectiveTest<WizardBlock>("""
:::{wizard} wizard.yaml
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/wizard.yaml",
			new MockFileData(
				"""
meta:
  title: Which type of page should I write?
  start: warmup
screens:
  warmup:
    type: question
    progress: 20
    question: How are you feeling today?
    options:
      - label: Great
        set: { mood: great }
        next: purpose
      - label: Fine
        set: { mood: fine }
        next: purpose
  purpose:
    type: question
    progress: 60
    question: What does the reader need?
    options:
      - label: To understand something
        set: { type: overview }
        next: done
      - label: To get something done
        set: { type: how-to }
        next: done
  done:
    type: result
    progress: 100
results:
  heading: "Write {type}"
  variables:
    type:
      answer: type
      items:
        overview: { name: an overview, docs: { title: Overviews, href: https://www.elastic.co/docs/contribute-docs/content-types/overviews } }
        how-to: { name: a how-to guide, docs: { title: How-tos, href: https://www.elastic.co/docs/contribute-docs/content-types/how-tos } }
  cta:
    - label: Go
      href_from: type
"""
			)
		);

	[Test]
	public void WarnsAboutAQuestionThatChangesNothingButStillBuilds()
	{
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("Question `warmup` doesn't change anything"));
		Block!.Config.Should().NotBeNull();
		Html.Should().Contain("<docs-wizard>");
	}
}
