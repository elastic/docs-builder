// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;
using Elastic.Markdown.Myst.Directives.CsvInclude;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class CsvIncludeTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeTests() : base("""
:::{csv-include} test-data.csv
:::
""") =>
		// Add a test CSV file to the mock file system
		FileSystem.AddFile(
			"docs/test-data.csv",
			new MockFileData(@"Name,Age,City
John Doe,30,New York
Jane Smith,25,Los Angeles
Bob Johnson,35,Chicago")
		);

	[Test]
	public void ParsesCsvFileBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("csv-include");

	[Test]
	public void FindsCsvFile() => Block!.Found.Should().BeTrue();

	[Test]
	public void SetsCorrectCsvFilePath() => Block!.CsvFilePath.Should().EndWith("test-data.csv");

	[Test]
	public void ParsesCsvDataCorrectly()
	{
		var csvData = CsvReader.ReadCsvFile(
			Block!.CsvFilePath!,
			Block.Separator,
			CheckoutsFileSystem.FromWorkingDirectory(FileSystem)
		).ToList();
		csvData.Should().HaveCount(4);
		csvData[0].Should().BeEquivalentTo(["Name", "Age", "City"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "30", "New York"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "25", "Los Angeles"]);
		csvData[3].Should().BeEquivalentTo(["Bob Johnson", "35", "Chicago"]);
	}

	[Test]
	public void UsesCommaAsDefaultSeparator() => Block!.Separator.Should().Be(",");
}

[InheritsTests]
public class CsvIncludeWithOptionsTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeWithOptionsTests() : base("""
:::{csv-include} test-data.csv
:caption: Sample User Data
:separator: ;
:::
""") =>
		FileSystem.AddFile("docs/test-data.csv", new MockFileData(@"Name;Age;City
John Doe;30;New York
Jane Smith;25;Los Angeles"));

	[Test]
	public void SetsCaption() => Block!.Caption.Should().Be("Sample User Data");

	[Test]
	public void UsesCustomSeparator() => Block!.Separator.Should().Be(";");

	[Test]
	public void ParsesWithCustomSeparator()
	{
		var csvData = CsvReader.ReadCsvFile(
			Block!.CsvFilePath!,
			Block.Separator,
			CheckoutsFileSystem.FromWorkingDirectory(FileSystem)
		).ToList();
		csvData.Should().HaveCount(3);
		csvData[0].Should().BeEquivalentTo(["Name", "Age", "City"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "30", "New York"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "25", "Los Angeles"]);
	}
}

[InheritsTests]
public class CsvIncludeWithQuotesTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeWithQuotesTests() : base("""
:::{csv-include} test-data.csv
:::
""") =>
		FileSystem.AddFile(
			"docs/test-data.csv",
			new MockFileData(
				@"Name,Description,Location
John Doe,""Software Engineer, Senior"",New York
Jane Smith,""Product Manager, Lead"",Los Angeles"
			)
		);

	[Test]
	public void HandlesQuotedFieldsWithCommas()
	{
		var csvData = CsvReader.ReadCsvFile(
			Block!.CsvFilePath!,
			Block.Separator,
			CheckoutsFileSystem.FromWorkingDirectory(FileSystem)
		).ToList();
		csvData.Should().HaveCount(3);
		csvData[0].Should().BeEquivalentTo(["Name", "Description", "Location"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "Software Engineer, Senior", "New York"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "Product Manager, Lead", "Los Angeles"]);
	}
}

[InheritsTests]
public class CsvIncludeWithEscapedQuotesTests : DirectiveTest<CsvIncludeBlock>
{
	public CsvIncludeWithEscapedQuotesTests() : base("""
:::{csv-include} test-data.csv
:::
""") =>
		FileSystem.AddFile(
			"docs/test-data.csv",
			new MockFileData(@"Name,Description
John Doe,""He said """"Hello World"""" today""
Jane Smith,""She replied """"Goodbye""""")
		);

	[Test]
	public void HandlesEscapedQuotes()
	{
		var csvData = CsvReader.ReadCsvFile(
			Block!.CsvFilePath!,
			Block.Separator,
			CheckoutsFileSystem.FromWorkingDirectory(FileSystem)
		).ToList();
		csvData.Should().HaveCount(3);
		csvData[0].Should().BeEquivalentTo(["Name", "Description"]);
		csvData[1].Should().BeEquivalentTo(["John Doe", "He said \"Hello World\" today"]);
		csvData[2].Should().BeEquivalentTo(["Jane Smith", "She replied \"Goodbye\""]);
	}
}

public class CsvIncludeRenderLinksTests() : DirectiveTest("""
::::{csv-include} test-data.csv
::::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/test-data.csv", new MockFileData(@"Name,Link
Search,[Text](https://www.google.com)"));

	[Test]
	public void RendersMarkdownLinkAsLink() => Html.Should().Contain(">Text</a>");
}

public class CsvIncludeWithHtmlBreaksTests() : DirectiveTest("""
::::{csv-include} test-data.csv
::::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/test-data.csv",
			new MockFileData(
				"""
			Name,Terms
			"OpenAI","[Terms A](https://example.com/a)<br>[Terms B](https://example.com/b)"
			"""
			)
		);

	[Test]
	public void RendersHtmlBreaksInCsvCells() => Html.Should().Contain("<br");

	[Test]
	public void RendersLinksWithBreaks()
	{
		Html.Should().Contain(">Terms A</a>");
		Html.Should().Contain(">Terms B</a>");
	}
}

[InheritsTests]
public class CsvIncludeNotFoundTests() : DirectiveTest<CsvIncludeBlock>("""
:::{csv-include} missing-file.csv
:::
""")
{
	[Test]
	public void ReportsFileNotFound() => Block!.Found.Should().BeFalse();

	[Test]
	public void EmitsErrorForMissingFile()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

[InheritsTests]
public class CsvIncludeNoArgumentTests() : DirectiveTest<CsvIncludeBlock>("""
:::{csv-include}
:::
""")
{
	[Test]
	public void EmitsErrorForMissingArgument()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("requires an argument"));
	}
}
