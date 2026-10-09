// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.DesignSystem;

/// <summary>Renders the document with <c>NAVIGATION_PREVIEW</c> on, so directives use design-system components.</summary>
public abstract class DesignSystemDirectiveTest : GeneratorTest
{
	protected abstract string Markdown { get; }

	protected override IReadOnlyCollection<TestFile> Files => [TestFile.Index($"# Test Document\n{Markdown}\n")];

	protected override SetupOptions Options => new() { NavigationPreview = true };
}

public class AdmonitionKinds : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		:::{warning}
		Back up first.
		:::
		""";

	[Test, DisplayName("renders the warning admonition component")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("<div class=\"admonition admonition--warning\" role=\"note\">");
		await Docs.ConvertsToContainingRawHtml("<span class=\"admonition__title\">Warning</span>");
		await Docs.ConvertsToContainingRawHtml("<div class=\"admonition__body\"><p>Back up first.</p></div>");
	}

	[Test, DisplayName("does not emit legacy admonition markup")]
	public async Task NoLegacyMarkup() => await Docs.DoesNotConvertToContainingHtml("admonition-header");

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class AdmonitionInListStaysNested : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		- List Item 1
		  :::::{note}
		  Hello, World!
		  :::::
		""";

	[Test, DisplayName("keeps the admonition inside the list item")]
	public async Task Renders() => await Docs.ConvertsToContainingRawHtml("<li>List Item 1");

	[Test, DisplayName("renders the note component")]
	public async Task NoteComponent() => await Docs.ConvertsToContainingRawHtml("class=\"admonition admonition--note\"");
}

public class DropdownComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		:::{dropdown} What is a shard?
		:open:
		A unit of storage.
		:::
		""";

	[Test, DisplayName("renders a native details element that is open")]
	public async Task Renders() => await Docs.ConvertsToContainingRawHtml("<details class=\"disclosure");

	[Test, DisplayName("keeps the open state and the title")]
	public async Task OpenAndTitle()
	{
		await Docs.ConvertsToContainingRawHtml(" open>");
		await Docs.ConvertsToContainingRawHtml("What is a shard?");
	}

	[Test, DisplayName("renders the body")]
	public async Task Body() => await Docs.ConvertsToContainingRawHtml("<p>A unit of storage.</p>");
}

public class TabsComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		::::{tab-set}
		:group: lang

		:::{tab-item} curl
		:sync: curl
		Use curl.
		:::

		:::{tab-item} Python
		:sync: python
		Use Python.
		:::
		::::
		""";

	[Test, DisplayName("renders radio tabs with sync attributes")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"tab-set\"");
		await Docs.ConvertsToContainingRawHtml("data-tab-sync-key=\"curl\"");
		await Docs.ConvertsToContainingRawHtml("data-tab-sync-group=\"lang\"");
	}

	[Test, DisplayName("checks only the first tab")]
	public async Task FirstChecked()
	{
		await Docs.ConvertsToContainingRawHtml("id=\"tabs-item-1-0\" name=\"tabs-set-1\" checked>");
		await Docs.ConvertsToContainingRawHtml("id=\"tabs-item-1-1\" name=\"tabs-set-1\">");
	}

	[Test, DisplayName("does not use the legacy tab classes the old script binds to")]
	public async Task NoLegacyHooks()
	{
		await Docs.DoesNotConvertToContainingHtml("tabs-label");
		await Docs.DoesNotConvertToContainingHtml("data-sync-id");
	}
}

public class StepperComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		::::{stepper}
		:::{step} Install
		Download it.
		:::

		:::{step} Start
		Run it.
		:::
		::::
		""";

	[Test, DisplayName("renders an ordered list of steps")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("<ol class=\"stepper__list\">");
		await Docs.ConvertsToContainingRawHtml("<li class=\"stepper__step\">");
	}

	[Test, DisplayName("renders step titles as headings with anchors")]
	public async Task Titles() =>
		await Docs.ConvertsToContainingRawHtml("<h2 class=\"stepper__title\" id=\"install\"><a href=\"#install\">Install</a></h2>");
}

public class ButtonDirectiveComponent : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		:::{button}
		[Get started](https://www.elastic.co)
		:::
		""";

	[Test, DisplayName("wraps the link in the button directive")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"button-directive button-directive--primary");
		await Docs.ConvertsToContainingRawHtml("Get started");
	}
}

/// <summary>Flag off must keep producing the legacy markup, whatever the design system adds.</summary>
public class FlagOffKeepsLegacyMarkup : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{warning}
		Back up first.
		:::

		:::{dropdown} Title
		Body
		:::
		""";

	[Test, DisplayName("uses the legacy admonition and dropdown")]
	public async Task Legacy()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"admonition warning");
		await Docs.ConvertsToContainingRawHtml("class=\"dropdown");
		await Docs.DoesNotConvertToContainingHtml("admonition__");
		await Docs.DoesNotConvertToContainingHtml("disclosure");
	}
}

public class CodeBlockComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		```json
		{ "size": 10 }
		```

		```yaml
		cluster.name: docs <1>
		```

		1. The cluster name.
		""";

	[Test, DisplayName("renders the code block component with the language label")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("<figure class=\"code-block notranslate\" data-language=\"json\">");
		await Docs.ConvertsToContainingRawHtml("<span class=\"code-block__language\">JSON</span>");
		await Docs.ConvertsToContainingRawHtml("<pre class=\"code-block__pre\">");
	}

	[Test, DisplayName("keeps pre > code so the highlighter finds the code")]
	public async Task KeepsPreCode() => await Docs.ConvertsToContainingRawHtml("<code class=\"language-json\">");

	[Test, DisplayName("renders a copy button that stays hidden until the script reveals it")]
	public async Task CopyButton() =>
		await Docs.ConvertsToContainingRawHtml("<button class=\"code-block__copy\" type=\"button\" data-copy hidden>");

	[Test, DisplayName("keeps the callout markers and the callout list")]
	public async Task Callouts()
	{
		await Docs.ConvertsToContainingRawHtml("<span class=\"code-callout\" data-index=\"1\"></span>");
		await Docs.ConvertsToContainingRawHtml("<ol class=\"code-callouts\">");
	}

	[Test, DisplayName("does not use the legacy highlight wrappers the old copy script binds to")]
	public async Task NoLegacyHooks() => await Docs.DoesNotConvertToContainingHtml("class=\"highlight\"");
}

public class PageShellComponents : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		## First section

		Some text.

		### A subsection

		More text.
		""";

	[Test, DisplayName("wraps the page in the design-system shell")]
	public async Task Shell()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"docs-shell\"");
		await Docs.ConvertsToContainingRawHtml("class=\"docs-shell__nav\"");
		await Docs.ConvertsToContainingRawHtml("class=\"docs-shell__rail\"");
	}

	[Test, DisplayName("styles the article with prose instead of the legacy markdown-content class")]
	public async Task Prose()
	{
		await Docs.ConvertsToContainingRawHtml("<article id=\"markdown-content\" class=\"prose min-w-0\"");
		await Docs.DoesNotConvertToContainingHtml("class=\"markdown-content");
	}

	[Test, DisplayName("renders the outline from the page headings")]
	public async Task Outline()
	{
		await Docs.ConvertsToContainingRawHtml("<nav class=\"toc\"");
		await Docs.ConvertsToContainingRawHtml("href=\"#first-section\"");
		await Docs.ConvertsToContainingRawHtml("toc__item--nested");
	}

	[Test, DisplayName("renders the design-system header and footer")]
	public async Task HeaderAndFooter()
	{
		await Docs.ConvertsToContainingRawHtml("<header class=\"site-header\"");
		await Docs.ConvertsToContainingRawHtml("<footer class=\"site-footer\"");
	}

	[Test, DisplayName("adds a search trigger that stays hidden until script can open the dialog")]
	public async Task SearchTrigger()
	{
		await Docs.ConvertsToContainingRawHtml(
			"<button class=\"search-field search-field--trigger\" type=\"button\" data-search-open hidden"
		);
		await Docs.ConvertsToContainingRawHtml("<dialog class=\"search-dialog\" data-search-dialog");
	}

	[Test, DisplayName("leaves out the legacy React header")]
	public async Task NoLegacyHeader() => await Docs.DoesNotConvertToContainingHtml("<elastic-docs-header");
}

public class AppliesToComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		:::{note}
		:applies_to: stack: ga 9.1
		This applies to the stack.
		:::

		Inline badge: {applies_to}`stack: preview 9.0`
		""";

	[Test, DisplayName("renders the design-system badge instead of the React element")]
	public async Task Badge()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"applies-badge applies-badge--ga");
		await Docs.ConvertsToContainingRawHtml("class=\"applies-badge__product\">Stack</span>");
		await Docs.DoesNotConvertToContainingHtml("<applies-to-popover");
	}

	[Test, DisplayName("opens details from a native details element with a top-layer hook")]
	public async Task Details()
	{
		await Docs.ConvertsToContainingRawHtml("<details class=\"applies-popover\" data-menu data-popover>");
		await Docs.ConvertsToContainingRawHtml("data-popover-panel");
	}

	[Test, DisplayName("shows the lifecycle text when the version is not released yet")]
	public async Task Status()
	{
		// The documented versions are ahead of the test version config, so the badge shows its lifecycle text instead of a version.
		await Docs.ConvertsToContainingRawHtml("Planned</span>");
		await Docs.ConvertsToContainingRawHtml("applies-badge--preview");
	}
}
