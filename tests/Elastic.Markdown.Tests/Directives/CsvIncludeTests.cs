// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.CsvInclude;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class CsvIncludeTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeTests(ITestOutputHelper output) : base(output,
"""
:::{csv-include} test-data.csv
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
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("csv-include");

	[Fact]
	public void FindsCsvFile() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectCsvFilePath() => Block!.CsvFilePath.Should().EndWith("test-data.csv");

	[Fact]
	public void ParsesCsvDataCorrectly()
	{
		var csvData = CsvReader.ReadCsvFile(Block!.CsvFilePath!, Block.Separator, FileSystem).ToList();
		csvData.Should().HaveCount(4);
		csvData[0].Should().BeEquivalentTo(["Name", "Age", "City"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "30", "New York"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "25", "Los Angeles"]);
		csvData[3].Should().BeEquivalentTo(["Bob Johnson", "35", "Chicago"]);
	}

	[Fact]
	public void UsesCommaAsDefaultSeparator() => Block!.Separator.Should().Be(",");
}

public class CsvIncludeWithOptionsTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeWithOptionsTests(ITestOutputHelper output) : base(output,
"""
:::{csv-include} test-data.csv
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
		var csvData = CsvReader.ReadCsvFile(Block!.CsvFilePath!, Block.Separator, FileSystem).ToList();
		csvData.Should().HaveCount(3);
		csvData[0].Should().BeEquivalentTo(["Name", "Age", "City"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "30", "New York"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "25", "Los Angeles"]);
	}
}

public class CsvIncludeWithQuotesTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeWithQuotesTests(ITestOutputHelper output) : base(output,
"""
:::{csv-include} test-data.csv
:::
""") => FileSystem.AddFile("docs/test-data.csv", new MockFileData(
@"Name,Description,Location
John Doe,""Software Engineer, Senior"",New York
Jane Smith,""Product Manager, Lead"",Los Angeles"));

	[Fact]
	public void HandlesQuotedFieldsWithCommas()
	{
		var csvData = CsvReader.ReadCsvFile(Block!.CsvFilePath!, Block.Separator, FileSystem).ToList();
		csvData.Should().HaveCount(3);
		csvData[0].Should().BeEquivalentTo(["Name", "Description", "Location"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "Software Engineer, Senior", "New York"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "Product Manager, Lead", "Los Angeles"]);
	}
}

public class CsvIncludeWithEscapedQuotesTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeWithEscapedQuotesTests(ITestOutputHelper output) : base(output,
"""
:::{csv-include} test-data.csv
:::
""") => FileSystem.AddFile("docs/test-data.csv", new MockFileData(
@"Name,Description
John Doe,""He said """"Hello World"""" today""
Jane Smith,""She replied """"Goodbye"""""));

	[Fact]
	public void HandlesEscapedQuotes()
	{
		var csvData = CsvReader.ReadCsvFile(Block!.CsvFilePath!, Block.Separator, FileSystem).ToList();
		csvData.Should().HaveCount(3);
		csvData[0].Should().BeEquivalentTo(["Name", "Description"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "He said \"Hello World\" today"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "She replied \"Goodbye\""]);
	}
}

public class CsvIncludeNotFoundTests(ITestOutputHelper output) : DirectiveTest<CsvIncludeBlock>(output,
"""
:::{csv-include} missing-file.csv
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

public class CsvIncludeNoArgumentTests(ITestOutputHelper output) : DirectiveTest<CsvIncludeBlock>(output,
"""
:::{csv-include}
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
