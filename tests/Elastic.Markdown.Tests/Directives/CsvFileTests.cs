// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.CsvFile;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class CsvFileTests : DirectiveTest<CsvFileBlock>
{
	public CsvFileTests(ITestOutputHelper output) : base(output,
"""
:::{csv-file} test-data.csv
:::
""") =>
		// Add a test CSV file to the mock file system
		FileSystem.AddFile("docs/test-data.csv", new MockFileData(
@"Name,Age,City
John Doe,30,New York
Jane Smith,25,Los Angeles
Bob Johnson,35,Chicago"));

	[Fact]
	public void ParsesCsvFileBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("csv-file");

	[Fact]
	public void FindsCsvFile() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectCsvFilePath() => Block!.CsvFilePath.Should().EndWith("test-data.csv");

	[Fact]
	public void ParsesCsvDataCorrectly()
	{
		Block!.CsvData.Should().HaveCount(4);
		Block.CsvData[0].Should().BeEquivalentTo(["Name", "Age", "City"]);
		Block.CsvData[1].Should().BeEquivalentTo(["John Doe", "30", "New York"]);
		Block.CsvData[2].Should().BeEquivalentTo(["Jane Smith", "25", "Los Angeles"]);
		Block.CsvData[3].Should().BeEquivalentTo(["Bob Johnson", "35", "Chicago"]);
	}

	[Fact]
	public void UsesCommaAsDefaultSeparator() => Block!.Separator.Should().Be(",");
}

public class CsvFileWithOptionsTests : DirectiveTest<CsvFileBlock>
{
	public CsvFileWithOptionsTests(ITestOutputHelper output) : base(output,
"""
:::{csv-file} test-data.csv
:caption: Sample User Data
:separator: ;
:::
""") => FileSystem.AddFile("docs/test-data.csv", new MockFileData(
@"Name;Age;City
John Doe;30;New York
Jane Smith;25;Los Angeles"));

	[Fact]
	public void SetsCaption() => Block!.Caption.Should().Be("Sample User Data");

	[Fact]
	public void UsesCustomSeparator() => Block!.Separator.Should().Be(";");

	[Fact]
	public void ParsesWithCustomSeparator()
	{
		Block!.CsvData.Should().HaveCount(3);
		Block.CsvData[0].Should().BeEquivalentTo(["Name", "Age", "City"]);
		Block.CsvData[1].Should().BeEquivalentTo(["John Doe", "30", "New York"]);
		Block.CsvData[2].Should().BeEquivalentTo(["Jane Smith", "25", "Los Angeles"]);
	}
}

public class CsvFileWithQuotesTests : DirectiveTest<CsvFileBlock>
{
	public CsvFileWithQuotesTests(ITestOutputHelper output) : base(output,
"""
:::{csv-file} test-data.csv
:::
""") => FileSystem.AddFile("docs/test-data.csv", new MockFileData(
@"Name,Description,Location
John Doe,""Software Engineer, Senior"",New York
Jane Smith,""Product Manager, Lead"",Los Angeles"));

	[Fact]
	public void HandlesQuotedFieldsWithCommas()
	{
		Block!.CsvData.Should().HaveCount(3);
		Block.CsvData[0].Should().BeEquivalentTo(["Name", "Description", "Location"]);
		Block.CsvData[1].Should().BeEquivalentTo(["John Doe", "Software Engineer, Senior", "New York"]);
		Block.CsvData[2].Should().BeEquivalentTo(["Jane Smith", "Product Manager, Lead", "Los Angeles"]);
	}
}

public class CsvFileWithEscapedQuotesTests : DirectiveTest<CsvFileBlock>
{
	public CsvFileWithEscapedQuotesTests(ITestOutputHelper output) : base(output,
"""
:::{csv-file} test-data.csv
:::
""") => FileSystem.AddFile("docs/test-data.csv", new MockFileData(
@"Name,Description
John Doe,""He said """"Hello World"""" today""
Jane Smith,""She replied """"Goodbye"""""));

	[Fact]
	public void HandlesEscapedQuotes()
	{
		Block!.CsvData.Should().HaveCount(3);
		Block.CsvData[0].Should().BeEquivalentTo(["Name", "Description"]);
		Block.CsvData[1].Should().BeEquivalentTo(["John Doe", "He said \"Hello World\" today"]);
		Block.CsvData[2].Should().BeEquivalentTo(["Jane Smith", "She replied \"Goodbye\""]);
	}
}

public class CsvFileNotFoundTests(ITestOutputHelper output) : DirectiveTest<CsvFileBlock>(output,
"""
:::{csv-file} missing-file.csv
:::
""")
{
	[Fact]
	public void ReportsFileNotFound() => Block!.Found.Should().BeFalse();

	[Fact]
	public void EmitsErrorForMissingFile()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

public class CsvFileNoArgumentTests(ITestOutputHelper output) : DirectiveTest<CsvFileBlock>(output,
"""
:::{csv-file}
:::
""")
{
	[Fact]
	public void EmitsErrorForMissingArgument()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("requires an argument"));
	}
}
