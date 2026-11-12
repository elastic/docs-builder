// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.Myst.Directives.SchemaTree;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class SchemaTreeTests() : DirectiveTest<SchemaTreeBlock>(
	"""
:::{schema-tree} Nested response schema

* `value` — `List` — nullable

  Metadata: `elastic:type = date_range`

  * `$data$` — `Struct` — non-null
    * `from` — `Timestamp[ms]` — nullable
    * `to` — `Timestamp[ms]` — nullable

:::
"""
)
{
	[Test]
	public void Render_NestedFields_UsesNativeDisclosures() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());

	[Test]
	public void Export_Search_PreservesTheFieldList() =>
		PlainTextExporter.ConvertToPlainText(Document, Set.Context).Should().MatchSnapshot();

	[Test]
	public void Export_Markdown_PreservesTheFieldList() =>
		LlmMarkdownExporter.ConvertToLlmMarkdown(Document, Set.Context).Should().MatchSnapshot();

	[Test]
	public void Parse_NestedList_EmitsNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class SchemaTreeEscapingTests() : DirectiveTest<SchemaTreeBlock>(
	"""
:::{schema-tree} <script>label</script>

* `<img src=x onerror=alert(1)>` — `List<value>` — non-null

  Metadata: `<script>value</script>`

:::
"""
)
{
	[Test]
	public void Render_CodeAndTitle_EncodesHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class SchemaTreeValidationTests() : DirectiveTest<SchemaTreeBlock>(
	"""
:::{schema-tree}
A paragraph is not a field list.
:::

:::{schema-tree}
:::

:::{schema-tree}
1. Ordered root
:::

:::{schema-tree}
* Root
  1. Ordered child
:::
"""
)
{
	[Test]
	public void Parse_InvalidLists_EmitsErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchSnapshot();
}

[InheritsTests]
public class SchemaTreeMarkdownBodyTests() : DirectiveTest<SchemaTreeBlock>(
	"""
:::{schema-tree} Documented fields

* `first` — `Struct` — nullable

  A **formatted description**.

  * `child` — `Utf8` — non-null

* `second` — `Int64` — nullable

  Another root field.

:::
"""
)
{
	[Test]
	public void Render_MultipleRoots_PreservesDescriptions() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}
