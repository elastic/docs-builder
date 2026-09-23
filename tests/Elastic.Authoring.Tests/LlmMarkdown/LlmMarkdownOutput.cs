// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.LlmMarkdown.LlmMarkdownOutput;

public class BasicTextFormatting : DocumentTest
{
	protected override string Document => """
		This is **bold** and *italic* text.
		""";

	[Fact(DisplayName = "converts to standard markdown")]
	public async Task ConvertsToStandardMarkdown() => await Docs.ConvertsToNewLlm("This is **bold** and *italic* text.\n");
}

public class Headings : DocumentTest
{
	protected override string Document => """
		## Heading 2
		### Heading 3
		""";

	[Fact(DisplayName = "converts to standard markdown headings")]
	public async Task ConvertsToStandardMarkdownHeadings() =>
		await Docs.ConvertsToNewLlm("""

		## Heading 2


		### Heading 3
		""");
}

public class CodeBlocks : DocumentTest
{
	protected override string Document =>
		"""
		```python
		def hello():
		    print("Hello, world!")


		```
		""";

	[Fact(DisplayName = "renders code blocks")]
	public async Task RendersCodeBlocks() =>
		await Docs.ConvertsToNewLlm("""

		```python
		def hello():
		    print("Hello, world!")
		```
		""");
}

public class EnhancedCodeBlocks : DocumentTest
{
	protected override string Document =>
		"""
		```python title="Hello World Example"
		def hello():
		    print("Hello, world!") # <1>
		```

		1. This is a callout
		""";

	[Fact(DisplayName = "converts to code block with optional caption comment")]
	public async Task ConvertsToCodeBlockWithOptionalCaptionComment() =>
		await Docs.ConvertsToNewLlm("""

		```python
		def hello():
		    print("Hello, world!")
		```
		""");
}

public class Lists : DocumentTest
{
	protected override string Document =>
		"""
		- Item 1
		- Item 2
		  - Nested item 1
		  - Nested item 2
		    - Nested Nested item 1
		    - Nested Nested item 2
		- Item 3

		1. Ordered item 1
		2. Ordered item 2
		   1. Nested ordered item 1
		   2. Nested ordered item 2
		      - Nested unordered item 1
		      - Nested unordered item 2
		3. Ordered item 3
		""";

	[Fact(DisplayName = "converts to standard markdown lists")]
	public async Task ConvertsToStandardMarkdownLists() =>
		await Docs.ConvertsToNewLlm(
			"""

		- Item 1
		- Item 2
		  - Nested item 1
		  - Nested item 2
		    - Nested Nested item 1
		    - Nested Nested item 2
		- Item 3

		1. Ordered item 1
		2. Ordered item 2
		   1. Nested ordered item 1
		   2. Nested ordered item 2
		      - Nested unordered item 1
		      - Nested unordered item 2
		3. Ordered item 3
		"""
		);
}

public class Tables : DocumentTest
{
	protected override string Document =>
		"""
		| Header 1 | Header 2 |
		|---|---|
		| Cell 1 | Cell 2 |
		| Cell 3 | Cell 4 |
		""";

	[Fact(DisplayName = "converts to standard markdown tables")]
	public async Task ConvertsToStandardMarkdownTables() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Header 1 | Header 2 |
		|----------|----------|
		| Cell 1   | Cell 2   |
		| Cell 3   | Cell 4   |
		"""
		);
}

public class AppliesToRole : DocumentTest
{
	protected override string Document => """
		This is an inline {applies_to}`stack: preview 9.1` element.
		""";

	[Fact(DisplayName = "converts to human readable format")]
	public async Task ConvertsToHumanReadableFormat() =>
		await Docs.ConvertsToNewLlm("This is an inline <applies-to>Elastic Stack: Planned</applies-to> element.\n");
}

public class AppliesToRoleWithGaFutureVersion : DocumentTest
{
	protected override string Document => """
		This is an inline {applies_to}`stack: ga 9.0` element.
		""";

	[Fact(DisplayName = "shows planned text for unreleased version")]
	public async Task ShowsPlannedTextForUnreleasedVersion() =>
		await Docs.ConvertsToNewLlm("This is an inline <applies-to>Elastic Stack: Planned</applies-to> element.\n");
}

public class AppliesToRoleWithGaReleasedVersion : DocumentTest
{
	protected override string Document => """
		This is an inline {applies_to}`stack: ga 7.3` element.
		""";

	[Fact(DisplayName = "shows ga text for released version")]
	public async Task ShowsGaTextForReleasedVersion() =>
		await Docs.ConvertsToNewLlm("This is an inline <applies-to>Elastic Stack: Generally available since 7.3</applies-to> element.\n");
}

public class AppliesToRoleInSentenceWithServerless : DocumentTest
{
	protected override string Document => """
		This feature is available on {applies_to}`serverless: ga` for all users.
		""";

	[Fact(DisplayName = "shows serverless availability in sentence")]
	public async Task ShowsServerlessAvailabilityInSentence() =>
		await Docs.ConvertsToNewLlm(
			"This feature is available on <applies-to>Elastic Cloud Serverless: Generally available</applies-to> for all users.\n"
		);
}

public class AppliesToRoleInSentenceWithPreview : DocumentTest
{
	protected override string Document =>
		"""
		The new API {applies_to}`stack: preview 7.5` provides enhanced functionality.
		""";

	[Fact(DisplayName = "shows preview availability in sentence")]
	public async Task ShowsPreviewAvailabilityInSentence() =>
		await Docs.ConvertsToNewLlm(
			"The new API <applies-to>Elastic Stack: Preview since 7.5</applies-to> provides enhanced functionality.\n"
		);
}

public class AppliesToRoleInSentenceWithDeprecated : DocumentTest
{
	protected override string Document =>
		"""
		This method {applies_to}`stack: deprecated 7.0` should not be used in new code.
		""";

	[Fact(DisplayName = "shows deprecated availability in sentence")]
	public async Task ShowsDeprecatedAvailabilityInSentence() =>
		await Docs.ConvertsToNewLlm(
			"This method <applies-to>Elastic Stack: Deprecated since 7.0</applies-to> should not be used in new code.\n"
		);
}

public class AppliesToInlineRoleFormats : DocumentTest
{
	protected override string Document => "placeholder";

	[Theory]
	[InlineData("stack: ga 7.3", "Elastic Stack: Generally available since 7.3")]
	[InlineData("stack: ga 8.0", "Elastic Stack: Generally available since 8.0")]
	[InlineData("stack: ga 8.0+", "Elastic Stack: Generally available since 8.0")]
	[InlineData("stack: ga 9.0", "Elastic Stack: Planned")]
	[InlineData("stack: preview 7.5", "Elastic Stack: Preview since 7.5")]
	[InlineData("stack: preview =7.0", "Elastic Stack: Preview in 7.0")]
	[InlineData("stack: ga 7.0-8.0", "Elastic Stack: Generally available from 7.0 to 8.0")]
	[InlineData("stack: beta 7.0-7.5", "Elastic Stack: Beta from 7.0 to 7.5")]
	[InlineData("stack: preview 9.1", "Elastic Stack: Planned")]
	[InlineData("stack: beta 7.0", "Elastic Stack: Beta since 7.0")]
	[InlineData("stack: deprecated 7.0", "Elastic Stack: Deprecated since 7.0")]
	[InlineData("serverless: ga", "Elastic Cloud Serverless: Generally available")]
	[InlineData("elasticsearch: preview", "Serverless Elasticsearch projects: Preview")]
	[InlineData("vectordb: ga", "Serverless Elasticsearch Vector Database projects: Generally available")]
	public async Task RendersAllLifecycleTypesCorrectly(string input, string expected)
	{
		var scenario = Setup.Document($"Test {{applies_to}}`{input}` here.");
		var expectedOutput = $"Test <applies-to>{expected}</applies-to> here.\n";
		await scenario.ConvertsToNewLlm(expectedOutput);
	}
}

public class AppliesToInlineRoleWithMultipleLifecycles : DocumentTest
{
	protected override string Document =>
		"""
		This feature {applies_to}`stack: beta 7.0-7.1, ga 7.2` has multiple lifecycles.
		""";

	[Fact(DisplayName = "renders multiple lifecycles with product name for each")]
	public async Task RendersMultipleLifecyclesWithProductNameForEach() =>
		await Docs.ConvertsToNewLlm(
			"This feature <applies-to>Elastic Stack: Generally available since 7.2, Elastic Stack: Beta from 7.0 to 7.1</applies-to> has multiple lifecycles.\n"
		);
}

public class FrontmatterAppliesToInMetadata : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(
				"""
			---
			applies_to:
			  stack: ga 7.0
			---
			# Test Page

			This is test content.
			"""
			),
		];

	[Fact(DisplayName = "includes applies_to in LLM metadata output")]
	public async Task IncludesAppliesToInLlmMetadataOutput() =>
		await Docs.ConvertsToLlmWithMetadata(
			"""
			---
			title: Test Page
			applies_to:
			  - Elastic Stack: Generally available since 7.0
			---

			# Test Page
			This is test content.
			"""
		);
}

public class AppliesToCodeBlockDirective : DocumentTest
{
	protected override string Document => """
		```{applies_to}
		stack: ga 7.0
		serverless: ga
		```
		""";

	[Fact(DisplayName = "renders applies_to block with human-readable text")]
	public async Task RendersAppliesToBlockWithHumanReadableText() =>
		await Docs.ConvertsToNewLlm(
			"""
		<applies-to>
		  - Elastic Cloud Serverless: Generally available
		  - Elastic Stack: Generally available since 7.0
		</applies-to>
		"""
		);
}

public class AppliesToCodeBlockWithMultipleLifecycles : DocumentTest
{
	protected override string Document => """
		```{applies_to}
		stack: beta 7.0-7.1, ga 7.2
		```
		""";

	[Fact(DisplayName = "renders multiple lifecycles with product name for each")]
	public async Task RendersMultipleLifecyclesWithProductNameForEach() =>
		await Docs.ConvertsToNewLlm(
			"""
		<applies-to>
		  - Elastic Stack: Generally available since 7.2
		  - Elastic Stack: Beta from 7.0 to 7.1
		</applies-to>
		"""
		);
}

public class AppliesSwitchDirective : DocumentTest
{
	protected override string Document =>
		"""
		::::{applies-switch}
		:::{applies-item} stack: ga 7.0
		Content for Elastic Stack users.
		:::
		:::{applies-item} serverless: ga
		Content for Serverless users.
		:::
		::::
		""";

	[Fact(DisplayName = "renders applies-switch with human-readable applies-to")]
	public async Task RendersAppliesSwitchWithHumanReadableAppliesTo() =>
		await Docs.ConvertsToNewLlm(
			"""

		<applies-switch>
		  <applies-item title="stack: ga 7.0" applies-to="Elastic Stack: Generally available since 7.0">
		    Content for Elastic Stack users.
		  </applies-item>

		  <applies-item title="serverless: ga" applies-to="Elastic Cloud Serverless: Generally available">
		    Content for Serverless users.
		  </applies-item>
		</applies-switch>
		"""
		);
}

public class AdmonitionDirective : DocumentTest
{
	protected override string Document =>
		"""
		:::{note}
		This is a note admonition.
		:::
		:::{warning}
		This is a warning admonition.
		:::
		:::{tip}
		This is a tip admonition.
		:::
		:::{important}
		This is a tip admonition.
		:::
		:::{admonition} This is my callout
		It can *span* multiple lines and supports inline formatting.

		Here is a list:
		- Item 1
		- Item 2
		:::
		""";

	[Fact(DisplayName = "renders correctly")]
	public async Task RendersCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		<note>
		  This is a note admonition.
		</note>

		<warning>
		  This is a warning admonition.
		</warning>

		<tip>
		  This is a tip admonition.
		</tip>

		<important>
		  This is a tip admonition.
		</important>

		<admonition title="This is my callout">
		  It can *span* multiple lines and supports inline formatting.Here is a list:
		  - Item 1
		  - Item 2
		</admonition>
		"""
		);
}

public class AdmonitionDirectiveWithAppliesTo : DocumentTest
{
	protected override string Document =>
		"""
		:::{note}
		:applies_to: stack: ga
		This is a note admonition with applies_to information.
		:::
		:::{warning}
		:applies_to: serverless: ga
		This is a warning admonition with applies_to information.
		:::
		:::{tip}
		:applies_to: elasticsearch: preview
		This is a tip admonition with applies_to information.
		:::
		:::{important}
		:applies_to: { stack: ga, serverless: ga }
		This is an important admonition with applies_to information.
		:::
		:::{admonition} Custom Admonition
		:applies_to: { stack: ga, elasticsearch: preview }
		This is a custom admonition with applies_to information.
		:::
		""";

	[Fact(DisplayName = "renders correctly with applies_to information")]
	public async Task RendersCorrectlyWithAppliesToInformation() =>
		await Docs.ConvertsToNewLlm(
			"""

		<note applies-to="Elastic Stack: Generally available">
		  This is a note admonition with applies_to information.
		</note>

		<warning applies-to="Elastic Cloud Serverless: Generally available">
		  This is a warning admonition with applies_to information.
		</warning>

		<tip applies-to="Serverless Elasticsearch projects: Preview">
		  This is a tip admonition with applies_to information.
		</tip>

		<important applies-to="Elastic Cloud Serverless: Generally available, Elastic Stack: Generally available">
		  This is an important admonition with applies_to information.
		</important>

		<admonition title="Custom Admonition" applies-to="Serverless Elasticsearch projects: Preview, Elastic Stack: Generally available">
		  This is a custom admonition with applies_to information.
		</admonition>
		"""
		);
}

public class AdmonitionDirectiveWithMultipleLifecycles : DocumentTest
{
	protected override string Document =>
		"""
		:::{note}
		:applies_to: stack: beta 7.0-7.1, ga 7.2
		This note has multiple lifecycle states.
		:::
		""";

	[Fact(DisplayName = "renders multiple lifecycles with product name for each")]
	public async Task RendersMultipleLifecyclesWithProductNameForEach() =>
		await Docs.ConvertsToNewLlm(
			"""

		<note applies-to="Elastic Stack: Generally available since 7.2, Elastic Stack: Beta from 7.0 to 7.1">
		  This note has multiple lifecycle states.
		</note>
		"""
		);
}

public class ImageDirective : DocumentTest
{
	protected override string Document =>
		"""
		```{image} /path/to/image.png
		:alt: Alt text
		:width: 300px
		```
		""";

	[Fact(DisplayName = "converts to standard markdown image")]
	public async Task ConvertsToStandardMarkdownImage() =>
		await Docs.ConvertsToNewLlm("""

		![Alt text](https://www.elastic.co/path/to/image.png)
		""");
}

public class IncludeDirective : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index("""

			```{include} _snippets/my-include.md
			```
			"""),
			Page("_snippets/my-include.md", """

			- List item
			  - Nested list item
			"""),
		];

	[Fact(DisplayName = "handles include directives appropriately")]
	public async Task HandlesIncludeDirectivesAppropriately() =>
		await Docs.ConvertsToNewLlm("""

		- List item
		  - Nested list item
		""");
}

public class MultipleElements : DocumentTest
{
	protected override string Document =>
		"""
		This is a paragraph with **bold** and *italic* text.

		```python
		def hello():
		    print("Hello, world!")
		```

		- List item 1
		- List item 2

		| Header 1 | Header 2 |
		|---|---|
		| Cell 1 | Cell 2 |
		""";

	[Fact(DisplayName = "converts complex document to clean markdown")]
	public async Task ConvertsComplexDocumentToCleanMarkdown() =>
		await Docs.ConvertsToNewLlm(
			"""

		This is a paragraph with **bold** and *italic* text.
		```python
		def hello():
		    print("Hello, world!")
		```

		- List item 1
		- List item 2


		| Header 1 | Header 2 |
		|----------|----------|
		| Cell 1   | Cell 2   |
		"""
		);
}

public class DirectiveInListShouldBeIndentedCorrectly : DocumentTest
{
	protected override string Document =>
		"""
		* List item 1
		  ```python
		  def hello():
		      print("Hello, world!")
		  ```
		* List item 2
		  :::{tip}
		    - Nested list item 1
		    - Nested list item 2
		  :::

		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		- List item 1
		  ```python
		  def hello():
		      print("Hello, world!")
		  ```
		- List item 2
		  <tip>
		  - Nested list item 1
		  - Nested list item 2
		  </tip>
		"""
		);
}

public class Tabs : DocumentTest
{
	protected override string Document =>
		"""
		::::{tab-set}

		:::{tab-item} Tab #1 title
		This is where the content for tab #1 goes.
		:::

		:::{tab-item} Tab #2 title
		This is where the content for tab #2 goes.
		:::

		::::
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		<tab-set>
		  <tab-item title="Tab #1 title">
		    This is where the content for tab #1 goes.
		  </tab-item>

		  <tab-item title="Tab #2 title">
		    This is where the content for tab #2 goes.
		  </tab-item>
		</tab-set>
		"""
		);
}

public class Comments : DocumentTest
{
	protected override string Document =>
		"""
		This text is visible

		% This is a comment

		<!--
		This is also a comment
		-->

		This text is also visible
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() => await Docs.ConvertsToNewLlm("This text is visible\nThis text is also visible");
}

public class Dropdown : DocumentTest
{
	protected override string Document =>
		"""
		:::{dropdown} Dropdown title
		This is where the content for the dropdown goes.
		:::
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		<dropdown title="Dropdown title">
		  This is where the content for the dropdown goes.
		</dropdown>
		"""
		);
}

public class DropdownWithAppliesTo : DocumentTest
{
	protected override string Document =>
		"""
		:::{dropdown} Dropdown title
		:applies_to: stack: ga 8.0
		This is where the content for the dropdown goes.
		:::
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		<dropdown title="Dropdown title" applies-to="Elastic Stack: Generally available since 8.0">
		  This is where the content for the dropdown goes.
		</dropdown>
		"""
		);
}

public class DefinitionList : DocumentTest
{
	protected override string Document =>
		"""
		`First Term`
		:   This is the definition of the first term.
		    - This a list in a definition
		    - This a list in a definition

		`Second Term`
		:    This is one definition of the second term.
		     ```javascript
		     console.log("Hello, world!");
		     ```
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		<definitions>
		  <definition term="First Term">
		    This is the definition of the first term.
		    - This a list in a definition
		    - This a list in a definition
		  </definition>
		  <definition term="Second Term">
		    This is one definition of the second term.
		    ```javascript
		    console.log("Hello, world!");
		    ```
		  </definition>
		</definitions>
		"""
		);
}

public class ImageInline : DocumentTest
{
	protected override string Document =>
		"""
		![elasticsearch](images/64x64_Color_elasticsearch-logo-color-64px.png "elasticsearch =50%")
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		![elasticsearch](https://www.elastic.co/images/64x64_Color_elasticsearch-logo-color-64px.png "elasticsearch")
		"""
		);
}

public class KbdRole : DocumentTest
{
	protected override string Document => """
		{kbd}`cmd+enter`
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() => await Docs.ConvertsToNewLlm("""

		<kbd>Cmd</kbd> + <kbd>Enter</kbd>
		""");
}

public class CodeblockInList : DocumentTest
{
	protected override string Document =>
		"""
		- List item 1
		  ```python
		  def hello():
		      print("Hello, world!")
		  ```
		- List item 2
		  1. Nested list item
		     ```python
		     def hello():
		         print("Hello, world!")
		     ```
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		- List item 1
		  ```python
		  def hello():
		      print("Hello, world!")
		  ```
		- List item 2
		  1. Nested list item
		     ```python
		     def hello():
		         print("Hello, world!")
		     ```
		"""
		);
}

public class Substitutions : DocumentTest
{
	protected override string Document =>
		"""
		---
		sub:
		  hello-world: "Hello World!"
		---

		Hello, this is a substitution: {{hello-world}}
		This is not a substitution: {{not-found}}
		""";

	[Fact(DisplayName = "rendered correctly")]
	public async Task RenderedCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		Hello, this is a substitution: Hello World!
		This is not a substitution: {{not-found}}
		"""
		);
}

public class SubstitutionInCodeblock : DocumentTest
{
	protected override string Document =>
		"""
		---
		sub:
		  hello-world: "Hello World!"
		---

		```plaintext
		Hello, this is a substitution: {{hello-world}}
		```

		```plaintext subs=true
		Hello, this is a substitution: {{hello-world}}
		```
		""";

	[Fact(DisplayName = "substitution in codeblock is only replaced when subs=true")]
	public async Task SubstitutionInCodeblockIsOnlyReplacedWhenSubsIsTrue() =>
		await Docs.ConvertsToNewLlm(
			"""

		```plaintext
		Hello, this is a substitution: {{hello-world}}
		```

		```plaintext
		Hello, this is a substitution: Hello World!
		```
		"""
		);
}

public class MermaidCodeBlock : DocumentTest
{
	protected override string Document =>
		"""
		```mermaid
		flowchart LR
		    A[Start] --> B{Decision}
		    B -->|Yes| C[Action 1]
		    B -->|No| D[Action 2]
		    C --> E[End]
		    D --> E
		```
		""";

	[Fact(DisplayName = "renders mermaid as code block")]
	public async Task RendersMermaidAsCodeBlock() =>
		await Docs.ConvertsToNewLlm(
			"""

		```mermaid
		flowchart LR
		A[Start] --> B{Decision}
		B -->|Yes| C[Action 1]
		B -->|No| D[Action 2]
		C --> E[End]
		D --> E
		```
		"""
		);
}

public class SubstitutionInHeading : DocumentTest
{
	protected override string Document =>
		"""
		---
		sub:
		  world: "World"
		---

		## Hello, {{world}}!
		""";

	[Fact(DisplayName = "renders correctly")]
	public async Task RendersCorrectly() => await Docs.ConvertsToNewLlm("""

		## Hello, World!
		""");
}

public class SettingsDirective : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index("""

			:::{settings} _settings/example-settings.yml
			:::
			"""),
			Page(
				"_settings/example-settings.yml",
				"""
			groups:
			  - group: General settings
			    settings:
			      - setting: xpack.example.setting
			        description: |
			          This is a test setting with **bold** text and a [link](https://example.com).
			        datatype: enum
			        default: strict
			        applies_to:
			          stack: ga 9.2
			        options:
			          - option: strict
			          - option: lenient
			        settings:
			          - setting: "[n].url"
			            description: Child setting description.
			            datatype: string
			      - setting: xpack.another.setting
			        description: Another setting description.
			  - group: Advanced settings
			    settings:
			      - setting: xpack.advanced.option
			        description: An advanced option.
			"""
			),
		];

	[Fact(DisplayName = "renders settings as markdown headings")]
	public async Task RendersSettingsAsMarkdownHeadings() =>
		await Docs.ConvertsToNewLlm(
			"""

		## General settings
		<definitions>
		  <definition term="xpack.example.setting">
		    <stack-availability>Elastic Stack: Planned</stack-availability>
		    <supported-on>Self-managed Elastic deployments: Planned</supported-on>

		This is a test setting with **bold** text and a [link](https://example.com).
		Datatype: `enum`
		Default: `strict`
		Options:
		- `strict`
		- `lenient`
		  </definition>
		  <definition term="xpack.example.setting[n].url">
		    <stack-availability>Elastic Stack: Planned</stack-availability>
		    <supported-on>Self-managed Elastic deployments: Planned</supported-on>

		Child setting description.
		Datatype: `string`
		  </definition>
		  <definition term="xpack.another.setting">

		Another setting description.
		  </definition>
		</definitions>

		## Advanced settings
		<definitions>
		  <definition term="xpack.advanced.option">

		An advanced option.
		  </definition>
		</definitions>
		"""
		);

	[Fact(DisplayName = "renders group headings one level deeper than preceding markdown heading")]
	public async Task RendersGroupHeadingsOneLevelDeeperThanPrecedingMarkdownHeading()
	{
		var scenario = Setup.Generate([
			Index(
				"""

				## Included settings file

				:::{settings} _settings/example-settings.yml
				:::
				"""
			),
			Page(
				"_settings/example-settings.yml",
				"""
				groups:
				  - group: General settings
				    settings:
				      - setting: xpack.example.setting
				        description: Test.
				  - group: Advanced settings
				    settings:
				      - setting: xpack.advanced.option
				        description: Advanced.
				"""
			),
		]);
		await scenario.ConvertsToNewLlm(
			"""

		## Included settings file


		### General settings
		<definitions>
		  <definition term="xpack.example.setting">

		Test.
		  </definition>
		</definitions>

		### Advanced settings
		<definitions>
		  <definition term="xpack.advanced.option">

		Advanced.
		  </definition>
		</definitions>
		"""
		);
	}
}

public class LinksInParagraphs : DocumentTest
{
	protected override string Document =>
		"""
		This is a paragraph with a [link to docs](https://www.elastic.co/docs/deploy-manage/security) in it.
		""";

	[Fact(DisplayName = "renders links without duplication")]
	public async Task RendersLinksWithoutDuplication() =>
		await Docs.ConvertsToNewLlm(
			"This is a paragraph with a [link to docs](https://www.elastic.co/docs/deploy-manage/security) in it.\n"
		);
}

public class LinksInTables : DocumentTest
{
	protected override string Document =>
		"""
		| Feature | Availability |
		|---------|--------------|
		| [Security configurations](https://www.elastic.co/docs/deploy-manage/security) | Full control |
		| [Authentication realms](https://www.elastic.co/docs/deploy-manage/users-roles) | Available |
		""";

	[Fact(DisplayName = "renders links in table cells without duplication")]
	public async Task RendersLinksInTableCellsWithoutDuplication() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Feature                                                                        | Availability |
		|--------------------------------------------------------------------------------|--------------|
		| [Security configurations](https://www.elastic.co/docs/deploy-manage/security)  | Full control |
		| [Authentication realms](https://www.elastic.co/docs/deploy-manage/users-roles) | Available    |
		"""
		);
}

public class MultipleLinksInTableCells : DocumentTest
{
	protected override string Document =>
		"""
		| Feature | Links |
		|---------|-------|
		| Security | [Config](https://example.com/config) and [Auth](https://example.com/auth) |
		""";

	[Fact(DisplayName = "renders multiple links in same cell without duplication")]
	public async Task RendersMultipleLinksInSameCellWithoutDuplication() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Feature  | Links                                                                     |
		|----------|---------------------------------------------------------------------------|
		| Security | [Config](https://example.com/config) and [Auth](https://example.com/auth) |
		"""
		);
}

public class LinksWithFormattingInTables : DocumentTest
{
	protected override string Document =>
		"""
		| Feature | Description |
		|---------|-------------|
		| [**Bold link**](https://example.com) | Description |
		| [*Italic link*](https://example.com/italic) | Another |
		""";

	[Fact(DisplayName = "renders formatted links in table cells correctly")]
	public async Task RendersFormattedLinksInTableCellsCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Feature                                     | Description |
		|---------------------------------------------|-------------|
		| [**Bold link**](https://example.com)        | Description |
		| [*Italic link*](https://example.com/italic) | Another     |
		"""
		);
}

public class BoldAndItalicInTables : DocumentTest
{
	protected override string Document =>
		"""
		| Format | Example |
		|--------|---------|
		| Bold | This is **bold text** here |
		| Italic | This is *italic text* here |
		| Both | This is **bold** and *italic* |
		""";

	[Fact(DisplayName = "renders bold and italic in table cells without duplication")]
	public async Task RendersBoldAndItalicInTableCellsWithoutDuplication() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Format | Example                       |
		|--------|-------------------------------|
		| Bold   | This is **bold text** here    |
		| Italic | This is *italic text* here    |
		| Both   | This is **bold** and *italic* |
		"""
		);
}

public class CodeInlineInTables : DocumentTest
{
	protected override string Document =>
		"""
		| Command | Description |
		|---------|-------------|
		| `git status` | Shows status |
		| `git commit` | Commits changes |
		""";

	[Fact(DisplayName = "renders code inline in table cells correctly")]
	public async Task RendersCodeInlineInTableCellsCorrectly() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Command      | Description     |
		|--------------|-----------------|
		| `git status` | Shows status    |
		| `git commit` | Commits changes |
		"""
		);
}

public class ImagesInTables : DocumentTest
{
	protected override string Document =>
		"""
		| Icon | Name |
		|------|------|
		| ![logo](https://example.com/logo.png) | Logo |
		""";

	[Fact(DisplayName = "renders images in table cells without duplication")]
	public async Task RendersImagesInTableCellsWithoutDuplication() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Icon                                  | Name |
		|---------------------------------------|------|
		| ![logo](https://example.com/logo.png) | Logo |
		"""
		);
}

public class CsvIncludeDirective : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index("""

			:::{csv-include} data/users.csv
			:::
			"""),
			Page(
				"data/users.csv",
				"""
			Name,Age,City
			John Doe,30,New York
			Jane Smith,25,Los Angeles
			Bob Johnson,35,Chicago
			"""
			),
		];

	[Fact(DisplayName = "renders csv as markdown table")]
	public async Task RendersCsvAsMarkdownTable() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Name        | Age | City        |
		|-------------|-----|-------------|
		| John Doe    | 30  | New York    |
		| Jane Smith  | 25  | Los Angeles |
		| Bob Johnson | 35  | Chicago     |
		"""
		);
}

public class CsvIncludeDirectiveWithCaption : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index("""

			:::{csv-include} data/products.csv
			:caption: Product List
			:::
			"""),
			Page(
				"data/products.csv",
				"""
			Product,Price,Stock
			Widget,9.99,100
			Gadget,19.99,50
			"""
			),
		];

	[Fact(DisplayName = "renders csv with caption as bold title")]
	public async Task RendersCsvWithCaptionAsBoldTitle() =>
		await Docs.ConvertsToNewLlm(
			"""

		**Product List**

		| Product | Price | Stock |
		|---------|-------|-------|
		| Widget  | 9.99  | 100   |
		| Gadget  | 19.99 | 50    |
		"""
		);
}

public class CsvIncludeDirectiveWithCustomSeparator : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index("""

			:::{csv-include} data/semicolon.csv
			:separator: ;
			:::
			"""),
			Page("data/semicolon.csv", """
			Name;Value
			Item1;100
			Item2;200
			"""),
		];

	[Fact(DisplayName = "parses csv with custom separator")]
	public async Task ParsesCsvWithCustomSeparator() =>
		await Docs.ConvertsToNewLlm(
			"""

		| Name  | Value |
		|-------|-------|
		| Item1 | 100   |
		| Item2 | 200   |
		"""
		);
}
