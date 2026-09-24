// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.AgentSkill;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class AgentSkillTests() : DirectiveTest<AgentSkillBlock>(
	"""
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql
:::
A regular paragraph.
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsUrl() => Block!.Url.Should().Be("https://github.com/elastic/agent-skills@elasticsearch-esql");

	[Test]
	public void SetsSkillName() => Block!.SkillName.Should().Be("elasticsearch-esql");

	[Test]
	public void SetsInstallCommand() => Block!.InstallCommand.Should().Be("npx skills add elastic/agent-skills@elasticsearch-esql");

	[Test]
	public void SetsDirective() => Block!.Directive.Should().Be("agent-skill");

	[Test]
	public void RendersAgentSkillDiv()
	{
		Html.Should().Contain("class=\"agent-skill\"");
		Html.Should().Contain("class=\"agent-skill-header\"");
		Html.Should().Contain("class=\"agent-skill-content\"");
	}

	[Test]
	public void RendersTitle() => Html.Should().Contain("Agent skill available");

	[Test]
	public void RendersDefaultText() => Html.Should().Contain("A skill is available to help AI agents with this topic.");

	[Test]
	public void RendersLearnMoreLink()
	{
		Html.Should().Contain("Learn more about agent skills for Elastic");
		Html.Should().Contain("href=\"https://www.elastic.co/docs/explore-analyze/ai-features/agent-skills#available-skills\"");
	}

	[Test]
	public void RendersCopyButton()
	{
		Html.Should().Contain("class=\"agent-skill-button\"");
		Html.Should().Contain("Copy install command");
		Html.Should().Contain("data-copy-text=\"npx skills add elastic/agent-skills@elasticsearch-esql\"");
	}

	[Test]
	public void DoesNotRenderLinkButton() => Html.Should().NotContain("Get the skill");
}

[InheritsTests]
public class AgentSkillWithBodyTests() : DirectiveTest<AgentSkillBlock>(
	"""
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql

This skill helps agents write and optimize ES|QL queries.
:::
A regular paragraph.
"""
)
{
	[Test]
	public void RendersCustomBody() => Html.Should().Contain("This skill helps agents write and optimize ES|QL queries.");

	[Test]
	public void StillRendersDefaultText() => Html.Should().Contain("A skill is available to help AI agents with this topic.");

	[Test]
	public void StillRendersLearnMoreLink() => Html.Should().Contain("Learn more about agent skills for Elastic");

	[Test]
	public void StillRendersCopyButton() => Html.Should().Contain("Copy install command");
}

[InheritsTests]
public class AgentSkillMissingUrlTests() : DirectiveTest<AgentSkillBlock>("""
:::{agent-skill}
:::
A regular paragraph.
""")
{
	[Test]
	public void EmitsError() => Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires a :url: property"));
}

[InheritsTests]
public class AgentSkillRelativeUrlTests() : DirectiveTest<AgentSkillBlock>(
	"""
:::{agent-skill}
:url: /relative/path
:::
A regular paragraph.
"""
)
{
	[Test]
	public void EmitsError() => Collector.Diagnostics.Should().Contain(d => d.Message.Contains("must be an absolute URL"));
}

[InheritsTests]
public class AgentSkillNoSkillNameTests() : DirectiveTest<AgentSkillBlock>(
	"""
:::{agent-skill}
:url: https://github.com/elastic/agent-skills
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SkillNameIsNull() => Block!.SkillName.Should().BeNull();

	[Test]
	public void InstallCommandIsNull() => Block!.InstallCommand.Should().BeNull();

	[Test]
	public void FallsBackToLinkButton()
	{
		Html.Should().Contain("Get the skill");
		Html.Should().Contain("href=\"https://github.com/elastic/agent-skills\"");
		Html.Should().NotContain("data-copy-text");
	}
}
