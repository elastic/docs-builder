// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Table;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class TableDirectiveWithWidthsTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 30 70

| Name  | Description                              |
| ----- | ---------------------------------------- |
| Alpha | A short description.                     |
| Beta  | A much longer description that goes on.  |
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesWidths() => Block!.Widths.Should().BeEquivalentTo([30, 70]);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void HtmlContainsColgroup() => Html.Should().Contain("<colgroup>");

	[Fact]
	public void HtmlContainsColumnWidths()
	{
		Html.Should().Contain("width:30%");
		Html.Should().Contain("width:70%");
	}

	[Fact]
	public void HtmlContainsFixedWidthsClass() => Html.Should().Contain("fixed-widths");
}

public class TableDirectiveWithThreeColumnsTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 1 2 3

| A   | B   | C   |
| --- | --- | --- |
| 1   | 2   | 3   |
:::
"""
)
{
	[Fact]
	public void ParsesWidths() => Block!.Widths.Should().BeEquivalentTo([1, 2, 3]);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void NormalizesWidthsToPercentages()
	{
		// 1 + 2 + 3 = 6, so widths should be ~16.67%, ~33.33%, ~50%
		Html.Should().Contain("width:16.67%");
		Html.Should().Contain("width:33.33%");
		Html.Should().Contain("width:50%");
	}
}

public class TableDirectiveWithoutWidthsTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}

| Name  | Description |
| ----- | ----------- |
| Alpha | A short one |
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void WidthsAreNull() => Block!.Widths.Should().BeNull();

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void HtmlDoesNotContainColgroup() => Html.Should().NotContain("<colgroup>");

	[Fact]
	public void HtmlDoesNotContainFixedWidthsClass() => Html.Should().NotContain("fixed-widths");
}

public class TableDirectiveWithAutoWidthsTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: auto

| Name  | Description |
| ----- | ----------- |
| Alpha | A short one |
:::
"""
)
{
	[Fact]
	public void WidthsAreNull() => Block!.Widths.Should().BeNull();

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void HtmlDoesNotContainColgroup() => Html.Should().NotContain("<colgroup>");
}

public class TableDirectiveWithCaptionTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table} Frozen delights
:widths: 30 20 50

| Treat    | Qty | Description      |
| -------- | --- | ---------------- |
| Lollipop | 10  | A sweet treat.   |
| Ice cream| 5   | Cold and creamy. |
:::
"""
)
{
	[Fact]
	public void ParsesCaption() => Block!.Caption.Should().Be("Frozen delights");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void HtmlContainsCaption() => Html.Should().Contain("Frozen delights");

	[Fact]
	public void HtmlContainsCaptionClass() => Html.Should().Contain("md-table-caption");
}

public class TableDirectiveMismatchedWidthsTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 30 40 30

| Name  | Description |
| ----- | ----------- |
| Alpha | A short one |
:::
"""
)
{
	[Fact]
	public void EmitsErrorForMismatchedWidths()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("specifies 3 values but the table has 2 columns")
		);
	}
}

public class TableDirectiveInvalidWidthTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 30 abc

| Name  | Description |
| ----- | ----------- |
| Alpha | A short one |
:::
"""
)
{
	[Fact]
	public void EmitsErrorForInvalidWidth()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Invalid column width 'abc'")
		);
	}
}

public class TableDirectiveNegativeWidthTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 30 -10

| Name  | Description |
| ----- | ----------- |
| Alpha | A short one |
:::
"""
)
{
	[Fact]
	public void EmitsErrorForNegativeWidth()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Invalid column width '-10'")
		);
	}
}

public class TableDirectiveZeroWidthTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 0 50

| Name  | Description |
| ----- | ----------- |
| Alpha | A short one |
:::
"""
)
{
	[Fact]
	public void EmitsErrorForZeroWidth()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Invalid column width '0'")
		);
	}
}

public class TableDirectiveDecimalWidthTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 30.5 69.5

| Name  | Description |
| ----- | ----------- |
| Alpha | A short one |
:::
"""
)
{
	[Fact]
	public void EmitsErrorForDecimalWidth()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Invalid column width '30.5'")
		);
	}
}

public class TableDirectiveWithNonTableContentTests(ITestOutputHelper output) : DirectiveTest<TableBlock>(output,
"""
:::{table}
:widths: 30 70

This is just a paragraph, not a table.
:::
"""
)
{
	[Fact]
	public void EmitsWarningForMissingTable()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("does not contain a pipe table")
		);
	}

	[Fact]
	public void HtmlDoesNotContainColgroup() => Html.Should().NotContain("<colgroup>");
}
