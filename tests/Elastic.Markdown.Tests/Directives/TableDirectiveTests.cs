// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Table;
using Nullean.Snapshots;
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
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
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
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="table-wrapper">
			<table>
				<thead>
					<tr>
						<th>a</th>
						<th>b</th>
						<th>c</th>
					</tr>
				</thead>
				<tbody>
					<tr>
						<td>1</td>
						<td>2</td>
						<td>3</td>
					</tr>
				</tbody>
			</table>
		</div>
		"""
		);
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
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="table-wrapper table-matrix">
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
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 Column width count (3) does not match table column count (2).");
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
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 Column widths must sum to 12 (Bootstrap grid). Got sum of 8.");
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
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:3 Table directive must contain a pipe table. Add a table using Markdown pipe syntax."
			);
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
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:3 Invalid widths value 'foo'. Use preset (auto, description) or dash-separated integers 1-12 that sum to 12 (e.g., 4-8, 4-4-4)."
			);
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
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:3 Invalid widths value '0-12'. Use preset (auto, description) or dash-separated integers 1-12 that sum to 12 (e.g., 4-8, 4-4-4)."
			);
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
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 Table directive must contain exactly one pipe table.");
}
