// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.AgentSkill;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

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
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class AgentSkillMissingUrlTests() : DirectiveTest<AgentSkillBlock>("""
:::{agent-skill}
:::
A regular paragraph.
""")
{
	[Test]
	public void EmitsError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 agent-skill directive requires a :url: property");
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
	public void EmitsError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 agent-skill :url: must be an absolute URL, got '/relative/path'");
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
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}
