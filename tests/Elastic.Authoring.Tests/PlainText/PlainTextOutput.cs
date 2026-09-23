// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.PlainText.PlainTextOutput;

public class BasicTextFormatting : DocumentTest
{
	protected override string Document => "This is **bold** and *italic* text.\n";

	[Fact(DisplayName = "strips bold and italic markers")]
	public async Task StripsBoldAndItalicMarkers() => await Docs.ConvertsToPlainText("This is bold and italic text.");
}

public class Headings : DocumentTest
{
	protected override string Document => "## Heading 2\n### Heading 3\n";

	[Fact(DisplayName = "converts headings to plain text without hash symbols")]
	public async Task ConvertsHeadingsToPlainTextWithoutHashSymbols() => await Docs.ConvertsToPlainText("Heading 2\n\nHeading 3");
}

public class InlineCode : DocumentTest
{
	protected override string Document => "This is `inline code` in a sentence.\n";

	[Fact(DisplayName = "strips backticks from inline code")]
	public async Task StripsBackticksFromInlineCode() => await Docs.ConvertsToPlainText("This is inline code in a sentence.");
}

public class CodeBlocks : DocumentTest
{
	protected override string Document => """
		```python
		def hello():
		    print("Hello, world!")
		```
		""";

	[Fact(DisplayName = "renders code content without fences")]
	public async Task RendersCodeContentWithoutFences() => await Docs.ConvertsToPlainText("def hello():\n    print(\"Hello, world!\")");
}

public class Links : DocumentTest
{
	protected override string Document => "This is a [link to docs](https://www.elastic.co/docs) in a sentence.\n";

	[Fact(DisplayName = "outputs link text only without URL")]
	public async Task OutputsLinkTextOnlyWithoutUrl() => await Docs.ConvertsToPlainText("This is a link to docs in a sentence.");
}

public class Images : DocumentTest
{
	protected override string Document => "![Alt text for image](https://example.com/image.png)\n";

	[Fact(DisplayName = "outputs image alt text only")]
	public async Task OutputsImageAltTextOnly() => await Docs.ConvertsToPlainText("Alt text for image");
}

public class UnorderedLists : DocumentTest
{
	protected override string Document => "- Item 1\n- Item 2\n- Item 3\n";

	[Fact(DisplayName = "renders list items without bullets")]
	public async Task RendersListItemsWithoutBullets() => await Docs.ConvertsToPlainText("Item 1\nItem 2\nItem 3");
}

public class OrderedLists : DocumentTest
{
	protected override string Document => "1. First item\n2. Second item\n3. Third item\n";

	[Fact(DisplayName = "renders list items without numbers")]
	public async Task RendersListItemsWithoutNumbers() => await Docs.ConvertsToPlainText("First item\nSecond item\nThird item");
}

public class NestedLists : DocumentTest
{
	protected override string Document => "- Item 1\n  - Nested item 1\n  - Nested item 2\n- Item 2\n";

	[Fact(DisplayName = "renders nested list items as plain text")]
	public async Task RendersNestedListItemsAsPlainText() => await Docs.ConvertsToPlainText("Item 1\nNested item 1\nNested item 2\nItem 2");
}

public class Tables : DocumentTest
{
	protected override string Document =>
		"""
		| Header 1 | Header 2 |
		|----------|----------|
		| Cell 1   | Cell 2   |
		| Cell 3   | Cell 4   |
		""";

	[Fact(DisplayName = "renders tables as header-value pairs")]
	public async Task RendersTablesAsHeaderValuePairs() =>
		await Docs.ConvertsToPlainText("Header 1: Cell 1\nHeader 2: Cell 2\nHeader 1: Cell 3\nHeader 2: Cell 4");
}

public class Blockquotes : DocumentTest
{
	protected override string Document => "> This is a quoted text\n> that spans multiple lines.\n";

	[Fact(DisplayName = "strips blockquote markers")]
	public async Task StripsBlockquoteMarkers() =>
		// Soft line breaks become spaces in plain text output
		await Docs.ConvertsToPlainText("This is a quoted text that spans multiple lines.");
}

public class AdmonitionDirectives : DocumentTest
{
	protected override string Document => ":::{note}\nThis is a note admonition.\n:::\n";

	[Fact(DisplayName = "renders admonition content without XML tags")]
	public async Task RendersAdmonitionContentWithoutXmlTags() => await Docs.ConvertsToPlainText("This is a note admonition.");
}

public class AdmonitionWithTitle : DocumentTest
{
	protected override string Document => ":::{admonition} Custom Title\nThis is the admonition content.\n:::\n";

	[Fact(DisplayName = "includes title and content")]
	public async Task IncludesTitleAndContent() => await Docs.ConvertsToPlainText("Custom Title\n\nThis is the admonition content.");
}

public class WarningDirective : DocumentTest
{
	protected override string Document => ":::{warning}\nThis is a warning message.\n:::\n";

	[Fact(DisplayName = "renders warning content as plain text")]
	public async Task RendersWarningContentAsPlainText() => await Docs.ConvertsToPlainText("This is a warning message.");
}

public class TipDirective : DocumentTest
{
	protected override string Document => ":::{tip}\nThis is a helpful tip.\n:::\n";

	[Fact(DisplayName = "renders tip content as plain text")]
	public async Task RendersTipContentAsPlainText() => await Docs.ConvertsToPlainText("This is a helpful tip.");
}

public class ImageDirective : DocumentTest
{
	protected override string Document => "```{image} /path/to/image.png\n:alt: Descriptive alt text\n```\n";

	[Fact(DisplayName = "outputs alt text only")]
	public async Task OutputsAltTextOnly() => await Docs.ConvertsToPlainText("Descriptive alt text");
}

public class DropdownDirective : DocumentTest
{
	protected override string Document => ":::{dropdown} Dropdown Title\nThis is dropdown content.\n:::\n";

	[Fact(DisplayName = "renders dropdown title and content")]
	public async Task RendersDropdownTitleAndContent() => await Docs.ConvertsToPlainText("Dropdown Title\n\nThis is dropdown content.");
}

public class TabsDirective : DocumentTest
{
	protected override string Document =>
		"""
		::::{tab-set}

		:::{tab-item} Tab 1
		Content for tab 1.
		:::

		:::{tab-item} Tab 2
		Content for tab 2.
		:::

		::::
		""";

	[Fact(DisplayName = "renders all tab content")]
	public async Task RendersAllTabContent() =>
		await Docs.ConvertsToPlainText("Tab 1\n\nContent for tab 1.\n\nTab 2\n\nContent for tab 2.");
}

public class DefinitionList : DocumentTest
{
	protected override string Document => "`Term 1`\n:   Definition for term 1.\n\n`Term 2`\n:   Definition for term 2.\n";

	[Fact(DisplayName = "renders terms and definitions as plain text")]
	public async Task RendersTermsAndDefinitionsAsPlainText() =>
		await Docs.ConvertsToPlainText("Term 1\nDefinition for term 1.\n\nTerm 2\nDefinition for term 2.");
}

public class Substitutions : DocumentTest
{
	protected override string Document =>
		"""
		---
		sub:
		  product-name: "Elasticsearch"
		---

		Welcome to {{product-name}}!
		""";

	[Fact(DisplayName = "resolves substitutions")]
	public async Task ResolvesSubstitutions() => await Docs.ConvertsToPlainText("Welcome to Elasticsearch!");
}

public class KbdRole : DocumentTest
{
	protected override string Document => "Press {kbd}`Ctrl+C` to copy.\n";

	[Fact(DisplayName = "renders keyboard shortcuts as readable text")]
	public async Task RendersKeyboardShortcutsAsReadableText() =>
		// Character keys preserve their case from input
		await Docs.ConvertsToPlainText("Press Ctrl + c to copy.");
}

public class AppliesToRole : DocumentTest
{
	protected override string Document => "This feature {applies_to}`stack: ga 7.0` is available.\n";

	[Fact(DisplayName = "renders applies_to as readable text")]
	public async Task RendersAppliesToAsReadableText() =>
		await Docs.ConvertsToPlainText("This feature (Elastic Stack: Generally available since 7.0) is available.");
}

public class AppliesToBlockDirective : DocumentTest
{
	protected override string Document => "```{applies_to}\nstack: ga 7.0\n```\n";

	[Fact(DisplayName = "renders applies_to block as readable text")]
	public async Task RendersAppliesToBlockAsReadableText() =>
		await Docs.ConvertsToPlainText("(Elastic Stack: Generally available since 7.0)");
}

public class Comments : DocumentTest
{
	protected override string Document => "This text is visible.\n\n% This is a comment\n\nThis text is also visible.\n";

	[Fact(DisplayName = "excludes comments from output")]
	public async Task ExcludesCommentsFromOutput() => await Docs.ConvertsToPlainText("This text is visible.\n\nThis text is also visible.");
}

public class ThematicBreak : DocumentTest
{
	protected override string Document => "First section.\n\n---\n\nSecond section.\n";

	[Fact(DisplayName = "renders thematic break as blank line")]
	public async Task RendersThematicBreakAsBlankLine() => await Docs.ConvertsToPlainText("First section.\n\n\nSecond section.");
}

public class ComplexDocument : DocumentTest
{
	protected override string Document =>
		"""
		This is a paragraph with **bold** and *italic* text.

		```python
		def hello():
		    print("Hello!")
		```

		- List item 1
		- List item 2

		| Name | Value |
		|------|-------|
		| foo  | bar   |
		""";

	[Fact(DisplayName = "renders complex document as clean text")]
	public async Task RendersComplexDocumentAsCleanText() =>
		await Docs.ConvertsToPlainText(
			"This is a paragraph with bold and italic text.\n" + "\ndef hello():\n    print(\"Hello!\")\n" +
				"\nList item 1\nList item 2\n" + "\nName: foo\nValue: bar"
		);
}

public class IncludeDirective : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[Index("```{include} _snippets/included.md\n```\n"), Page("_snippets/included.md", "\nThis is included content.\n"),];

	[Fact(DisplayName = "inlines included content")]
	public async Task InlinesIncludedContent() => await Docs.ConvertsToPlainText("This is included content.");
}

public class MathBlock : DocumentTest
{
	protected override string Document => "```{math}\nE = mc^2\n```\n";

	[Fact(DisplayName = "renders math content as text")]
	public async Task RendersMathContentAsText() => await Docs.ConvertsToPlainText("E = mc^2");
}

public class MermaidCodeBlock : DocumentTest
{
	protected override string Document => "```mermaid\nflowchart LR\n    A --> B\n```\n";

	[Fact(DisplayName = "renders mermaid as code content")]
	public async Task RendersMermaidAsCodeContent() =>
		// Mermaid code blocks render like any code block
		await Docs.ConvertsToPlainText("flowchart LR\nA --> B");
}

public class CsvIncludeDirective : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(":::{csv-include} data/users.csv\n:::\n"),
			Page("data/users.csv", "Name,Age,City\nJohn Doe,30,New York\nJane Smith,25,Los Angeles"),
		];

	[Fact(DisplayName = "renders csv as header-value pairs")]
	public async Task RendersCsvAsHeaderValuePairs() =>
		await Docs.ConvertsToPlainText("Name: John Doe\nAge: 30\nCity: New York\nName: Jane Smith\nAge: 25\nCity: Los Angeles");
}

public class CsvIncludeDirectiveWithCaption : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(":::{csv-include} data/products.csv\n:caption: Product List\n:::\n"),
			Page("data/products.csv", "Product,Price\nWidget,9.99\nGadget,19.99"),
		];

	[Fact(DisplayName = "includes caption in output")]
	public async Task IncludesCaptionInOutput() =>
		await Docs.ConvertsToPlainText("Product List\nProduct: Widget\nPrice: 9.99\nProduct: Gadget\nPrice: 19.99");
}

public class RealisticDocumentationPage : DocumentTest
{
	protected override string Document =>
		"""
		## Getting Started with Elasticsearch

		Elasticsearch is a distributed search and analytics engine. This guide helps you get started quickly.

		:::{note}
		Make sure you have Java 17 or later installed before proceeding.
		:::

		### Installation

		You can install Elasticsearch using several methods:

		- **Download directly** from the [official website](https://elastic.co/downloads)
		- Use **package managers** like `apt` or `yum`
		- Run with **Docker** for containerized environments

		### Quick Start

		After installation, start the service:

		```bash
		./bin/elasticsearch
		```

		Verify it's running by visiting `http://localhost:9200` in your browser.

		### Basic Configuration

		| Setting | Default | Description |
		|---------|---------|-------------|
		| `cluster.name` | elasticsearch | Name of your cluster |
		| `node.name` | auto-generated | Unique node identifier |

		:::{tip}
		Press {kbd}`Ctrl+C` to stop the server gracefully.
		:::

		### Next Steps

		1. Create your first index
		2. Index some documents
		3. Run your first search query

		For more details, see the **Configuration Guide** and **API Reference**.
		""";

	[Fact(DisplayName = "renders realistic page with preserved newlines")]
	public async Task RendersRealisticPageWithPreservedNewlines() =>
		await Docs.ConvertsToPlainText(
			"Getting Started with Elasticsearch\n" +
				"\nElasticsearch is a distributed search and analytics engine. This guide helps you get started quickly.\n" +
				"\nMake sure you have Java 17 or later installed before proceeding.\n" + "\nInstallation\n" +
				"\nYou can install Elasticsearch using several methods:\n" +
				"\nDownload directly from the official website\nUse package managers like apt or yum\nRun with Docker for containerized environments\n" +
				"\nQuick Start\n" + "\nAfter installation, start the service:\n" + "\n./bin/elasticsearch\n" +
				"\nVerify it's running by visiting http://localhost:9200 in your browser.\n" + "\nBasic Configuration\n" +
				"\nSetting: cluster.name\nDefault: elasticsearch\nDescription: Name of your cluster\n" +
				"Setting: node.name\nDefault: auto-generated\nDescription: Unique node identifier\n" +
				"\nPress Ctrl + c to stop the server gracefully.\n" + "\nNext Steps\n" +
				"\nCreate your first index\nIndex some documents\nRun your first search query\n" +
				"\nFor more details, see the Configuration Guide and API Reference."
		);
}
