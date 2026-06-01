// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.AgentSkill;

namespace Elastic.Markdown.Tests.Directives;

public class AgentSkillTests(ITestOutputHelper output) : DirectiveTest<AgentSkillBlock>(output,
"""
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsUrl() => Block!.Url.Should().Be("https://github.com/elastic/agent-skills@elasticsearch-esql");

	[Fact]
	public void SetsSkillName() => Block!.SkillName.Should().Be("elasticsearch-esql");

	[Fact]
	public void SetsInstallCommand() => Block!.InstallCommand.Should().Be("npx skills add @elasticsearch-esql");

	[Fact]
	public void SetsDirective() => Block!.Directive.Should().Be("agent-skill");

	[Fact]
	public void RendersAgentSkillDiv()
	{
		Html.Should().Contain("class=\"agent-skill\"");
		Html.Should().Contain("class=\"agent-skill-header\"");
		Html.Should().Contain("class=\"agent-skill-content\"");
	}

	[Fact]
	public void RendersTitle() =>
		Html.Should().Contain("Agent skill available");

	[Fact]
	public void RendersDefaultText() =>
		Html.Should().Contain("A skill is available to help AI agents with this topic.");

	[Fact]
	public void RendersLearnMoreLink()
	{
		Html.Should().Contain("Learn more about agent skills for Elastic");
		Html.Should().Contain("href=\"/explore-analyze/ai-features/agent-skills#available-skills\"");
	}

	[Fact]
	public void RendersCopyButton()
	{
		Html.Should().Contain("class=\"agent-skill-button\"");
		Html.Should().Contain("Copy install command");
		Html.Should().Contain("data-copy-text=\"npx skills add @elasticsearch-esql\"");
	}

	[Fact]
	public void DoesNotRenderLinkButton() =>
		Html.Should().NotContain("Get the skill");
}

public class AgentSkillWithBodyTests(ITestOutputHelper output) : DirectiveTest<AgentSkillBlock>(output,
"""
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql

This skill helps agents write and optimize ES|QL queries.
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void RendersCustomBody() =>
		Html.Should().Contain("This skill helps agents write and optimize ES|QL queries.");

	[Fact]
	public void StillRendersDefaultText() =>
		Html.Should().Contain("A skill is available to help AI agents with this topic.");

	[Fact]
	public void StillRendersLearnMoreLink() =>
		Html.Should().Contain("Learn more about agent skills for Elastic");

	[Fact]
	public void StillRendersCopyButton() =>
		Html.Should().Contain("Copy install command");
}

public class AgentSkillMissingUrlTests(ITestOutputHelper output) : DirectiveTest<AgentSkillBlock>(output,
"""
:::{agent-skill}
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires a :url: property"));
}

public class AgentSkillRelativeUrlTests(ITestOutputHelper output) : DirectiveTest<AgentSkillBlock>(output,
"""
:::{agent-skill}
:url: /relative/path
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("must be an absolute URL"));
}

public class AgentSkillNoSkillNameTests(ITestOutputHelper output) : DirectiveTest<AgentSkillBlock>(output,
"""
:::{agent-skill}
:url: https://github.com/elastic/agent-skills
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SkillNameIsNull() => Block!.SkillName.Should().BeNull();

	[Fact]
	public void InstallCommandIsNull() => Block!.InstallCommand.Should().BeNull();

	[Fact]
	public void FallsBackToLinkButton()
	{
		Html.Should().Contain("Get the skill");
		Html.Should().Contain("href=\"https://github.com/elastic/agent-skills\"");
		Html.Should().NotContain("data-copy-text");
	}
}
