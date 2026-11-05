// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Table;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class TableDirectiveBasicTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void ParsesTableDirectiveBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("table");

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="table-wrapper">
			<table>
				<thead>
					<tr>
						<th>head a</th>
						<th>head b</th>
					</tr>
				</thead>
				<tbody>
					<tr>
						<td>a</td>
						<td>b</td>
					</tr>
				</tbody>
			</table>
		</div>
		"""
		);
}

[InheritsTests]
public class TableDirectiveWithWidthsTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: 4-8

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void ParsesWidthsOption()
	{
		Block!.ColumnWidths.Should().HaveCount(2);
		Block.ColumnWidths[0].Should().BeApproximately(33.33, 0.1);
		Block.ColumnWidths[1].Should().BeApproximately(66.67, 0.1);
	}

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class TableDirectiveDescriptionPresetTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: description

| Term | Description |
| --- | --- |
| foo | A thing |
:::
"""
)
{
	[Test]
	public void MapsDescriptionTo4_8()
	{
		Block!.ColumnWidths.Should().HaveCount(2);
		Block.ColumnWidths[0].Should().BeApproximately(33.33, 0.1);
		Block.ColumnWidths[1].Should().BeApproximately(66.67, 0.1);
	}

	[Test]
	public void RendersColgroup() => Html.Should().Contain("colgroup");
}

[InheritsTests]
public class TableDirectiveAutoPresetTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: auto

| a | b | c |
| --- | --- | --- |
| 1 | 2 | 3 |
:::
"""
)
{
	[Test]
	public void HasNoColumnWidths() => Block!.ColumnWidths.Should().BeEmpty();

	[Test]
	public void DoesNotInjectColgroup() => Html.Should().NotContain("colgroup");
}

[InheritsTests]
public class TableDirectiveMatrixTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:matrix:

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void ParsesMatrixOption() => Block!.Matrix.Should().BeTrue();

	[Test]
	public void RendersMatrixClass() => Html.Should().Contain("table-wrapper table-matrix");
}

[InheritsTests]
public class TableDirectiveWithoutMatrixTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void DoesNotRenderMatrixClass() => Html.Should().NotContain("table-matrix");
}

[InheritsTests]
public class TableDirectiveFilterableTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:filterable:

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void ParsesFilterableOption() => Block!.Filterable.Should().BeTrue();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<filterable-table>
			<div class="table-wrapper">
				<table>
					<thead>
						<tr>
							<th>head a</th>
							<th>head b</th>
						</tr>
					</thead>
					<tbody>
						<tr>
							<td>a</td>
							<td>b</td>
						</tr>
					</tbody>
				</table>
			</div>
		</filterable-table>
		"""
		);

	[Test]
	public void KeepsServerRenderedTableInsideHost() => Html.Should().Contain("<filterable-table><div class=\"table-wrapper\"");
}

[InheritsTests]
public class TableDirectiveWithoutFilterableTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void DoesNotParseFilterableOption() => Block!.Filterable.Should().BeFalse();

	[Test]
	public void DoesNotWrapTable() => Html.Should().NotContain("filterable-table");
}

[InheritsTests]
public class TableDirectiveFilterableWithMatrixAndWidthsTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:filterable:
:matrix:
:widths: 4-8

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class TableDirectiveWidthCountMismatchTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: 4-4-4

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("does not match"));
}

[InheritsTests]
public class TableDirectiveWidthsSumErrorTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: 4-4

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("sum to 12"));
}

[InheritsTests]
public class TableDirectiveNoTableTests() : DirectiveTest<TableDirectiveBlock>("""
:::{table}
:widths: 4-8

Some text, no table.
:::
""")
{
	[Test]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("pipe table"));
}

[InheritsTests]
public class TableDirectiveInvalidWidthsTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: foo

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void EmitsErrorForInvalidPreset() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("Invalid widths value"));
}

[InheritsTests]
public class TableDirectiveOutOfRangeWidthsTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: 0-12

| head a | head b |
| --- | --- |
| a | b |
:::
"""
)
{
	[Test]
	public void EmitsErrorForOutOfRangeUnit() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("Invalid widths value"));
}

[InheritsTests]
public class TableDirectiveMultipleTablesTests() : DirectiveTest<TableDirectiveBlock>(
	"""
:::{table}
:widths: 4-8

| a | b |
| --- | --- |
| 1 | 2 |

| c | d |
| --- | --- |
| 3 | 4 |
:::
"""
)
{
	[Test]
	public void EmitsErrorForMultipleTables() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("exactly one pipe table"));
}
