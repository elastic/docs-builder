// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.AgentPrompt;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class AgentPromptTests() : DirectiveTest<AgentPromptBlock>(
	"""
:::::{agent-prompt}
:title: Run this with your agent

```text
Follow the quickstart.

1. Ask me for my URL & API key.
2. Do the steps.
```
:::::
A regular paragraph.
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsDirective() => Block!.Directive.Should().Be("agent-prompt");

	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Run this with your agent");

	[Test]
	public void KeepsPromptTextVerbatim() =>
		Block!.Prompt.Should().Be("Follow the quickstart.\n\n1. Ask me for my URL & API key.\n2. Do the steps.");

	[Test]
	public void RendersCallToActionAndModal()
	{
		Html.Should().Contain("class=\"agent-prompt\"");
		Html.Should().Contain("<dialog class=\"agent-prompt-dialog\"");
		Html.Should().Contain("Copy prompt");
	}

	[Test]
	public void RendersCompactCardNamedForAssistiveTechnology()
	{
		Html.Should().Contain("role=\"group\" aria-label=\"Agent prompt\"");
		Html.Should().NotContain("agent-prompt-eyebrow");
	}

	[Test]
	public void DefaultDescriptionSaysThePromptNeedsAnAgentThatCanRunCommands() =>
		Html.Should().Contain("Copy this prompt into an AI coding agent that can run commands.");

	[Test]
	public void RendersPromptLength()
	{
		var length = Block!.Prompt!.Length.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
		Html.Should().Contain($"Review prompt &middot; {length} characters");
		Html.Should().Contain($"Prompt &middot; {length} characters");
	}

	[Test]
	public void RepeatsDescriptionInsideDialog() => Html.Split("class=\"agent-prompt-dialog-description\"").Length.Should().Be(2);

	[Test]
	public void EncodesPromptInsideDialogOnly()
	{
		Html.Should().Contain("URL &amp; API key");
		var beforeDialog = Html[..Html.IndexOf("<dialog", StringComparison.Ordinal)];
		beforeDialog.Should().NotContain("Follow the quickstart");
	}
}

[InheritsTests]
public class AgentPromptMissingBodyTests() : DirectiveTest<AgentPromptBlock>("""
:::{agent-prompt}
Not a code block.
:::
""")
{
	[Test]
	public void EmitsErrorWithoutCodeBlock() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("agent-prompt directive requires"));

	[Test]
	public void RendersNothing() => Html.Should().NotContain("agent-prompt");
}
