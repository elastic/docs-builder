// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Table;
using AwesomeAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class TableDirectiveBasicTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
| head a | head b |
| --- | --- |
| a | b |
:::
""")
{
	[Fact]
	public void ParsesTableDirectiveBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("table");

	[Fact]
	public void RendersTableInOutput()
	{
		Html.Should().Contain("table-wrapper");
		Html.Should().Contain("<table");
		Html.Should().Contain("head a");
		Html.Should().Contain("head b");
	}
}

public class TableDirectiveWithWidthsTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: 4-8

| head a | head b |
| --- | --- |
| a | b |
:::
""")
{
	[Fact]
	public void ParsesWidthsOption()
	{
		Block!.ColumnWidths.Should().HaveCount(2);
		Block.ColumnWidths[0].Should().BeApproximately(33.33, 0.1);
		Block.ColumnWidths[1].Should().BeApproximately(66.67, 0.1);
	}

	[Fact]
	public void RendersColgroupWithWidths()
	{
		Html.Should().Contain("colgroup");
		Html.Should().Contain("table-layout:fixed");
		Html.Should().Contain("width:33.33%");
		Html.Should().Contain("width:66.67%");
	}
}

public class TableDirectiveDescriptionPresetTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: description

| Term | Description |
| --- | --- |
| foo | A thing |
:::
""")
{
	[Fact]
	public void MapsDescriptionTo4_8()
	{
		Block!.ColumnWidths.Should().HaveCount(2);
		Block.ColumnWidths[0].Should().BeApproximately(33.33, 0.1);
		Block.ColumnWidths[1].Should().BeApproximately(66.67, 0.1);
	}

	[Fact]
	public void RendersColgroup() => Html.Should().Contain("colgroup");
}

public class TableDirectiveAutoPresetTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: auto

| a | b | c |
| --- | --- | --- |
| 1 | 2 | 3 |
:::
""")
{
	[Fact]
	public void HasNoColumnWidths() => Block!.ColumnWidths.Should().BeEmpty();

	[Fact]
	public void DoesNotInjectColgroup() => Html.Should().NotContain("colgroup");
}

public class TableDirectiveWidthCountMismatchTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: 4-4-4

| head a | head b |
| --- | --- |
| a | b |
:::
""")
{
	[Fact]
	public void EmitsError() => Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("does not match"));
}

public class TableDirectiveWidthsSumErrorTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: 4-4

| head a | head b |
| --- | --- |
| a | b |
:::
""")
{
	[Fact]
	public void EmitsError() => Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("sum to 12"));
}

public class TableDirectiveNoTableTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: 4-8

Some text, no table.
:::
""")
{
	[Fact]
	public void EmitsError() => Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("pipe table"));
}

public class TableDirectiveInvalidWidthsTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: foo

| head a | head b |
| --- | --- |
| a | b |
:::
""")
{
	[Fact]
	public void EmitsErrorForInvalidPreset() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("Invalid widths value"));
}

public class TableDirectiveOutOfRangeWidthsTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
"""
:::{table}
:widths: 0-12

| head a | head b |
| --- | --- |
| a | b |
:::
""")
{
	[Fact]
	public void EmitsErrorForOutOfRangeUnit() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("Invalid widths value"));
}

public class TableDirectiveMultipleTablesTests(ITestOutputHelper output) : DirectiveTest<TableDirectiveBlock>(output,
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
""")
{
	[Fact]
	public void EmitsErrorForMultipleTables() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("exactly one pipe table"));
}
