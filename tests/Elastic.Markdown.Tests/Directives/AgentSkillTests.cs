// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.AgentSkill;
using FluentAssertions;

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
	public void RendersFixedText() =>
		Html.Should().Contain("A skill is available to help AI agents with this topic.");

	[Fact]
	public void RendersLearnMoreLink()
	{
		Html.Should().Contain("refer to the");
		Html.Should().Contain("href=\"/explore-analyze/ai-features/agent-skills\"");
	}

	[Fact]
	public void RendersButton()
	{
		Html.Should().Contain("class=\"agent-skill-button\"");
		Html.Should().Contain("Get the skill");
		Html.Should().Contain("href=\"https://github.com/elastic/agent-skills@elasticsearch-esql\"");
		Html.Should().Contain("target=\"_blank\"");
		Html.Should().Contain("rel=\"noopener noreferrer\"");
	}
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
