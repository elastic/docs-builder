// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;

namespace Elastic.ApiExplorer.Tests;

public class ApiOverviewSectionsTests
{
	private static ApiOverviewRow Row(OverviewRowKind kind, string title) => new() { Kind = kind, Title = title };

	[Test]
	public void Sections_ItemsBeforeAnyHeading_GoInASectionWithoutHeading()
	{
		var sections = ApiOverviewBuilder.Sections([Row(OverviewRowKind.MarkdownPage, "Intro"), Row(OverviewRowKind.TagHeading, "Search")]);

		sections.Should().HaveCount(2);
		sections[0].Heading.Should().BeNull();
		sections[0].Items.Select(i => i.Title).Should().Equal("Intro");
		sections[1].Heading!.Title.Should().Be("Search");
		sections[1].Items.Should().BeEmpty();
	}

	[Test]
	public void Sections_ClassificationFollowedByTag_KeepsTheEmptyClassification()
	{
		var sections = ApiOverviewBuilder.Sections([
			Row(OverviewRowKind.ClassificationHeading, "Search"),
			Row(OverviewRowKind.TagHeading, "Documents"),
			Row(OverviewRowKind.Operation, "Get a document")
		]);

		sections.Select(s => s.Heading!.Title).Should().Equal("Search", "Documents");
		sections[0].Items.Should().BeEmpty();
		sections[1].Items.Select(i => i.Title).Should().Equal("Get a document");
	}

	[Test]
	public void Sections_SchemasUnderACategory_AreGroupedUnderIt()
	{
		var sections = ApiOverviewBuilder.Sections([
			Row(OverviewRowKind.SchemaCategoryHeading, "Types"),
			Row(OverviewRowKind.Schema, "A"),
			Row(OverviewRowKind.Schema, "B")
		]);

		sections.Should().ContainSingle().Which.Items.Select(i => i.Title).Should().Equal("A", "B");
	}

	[Test]
	public void Sections_NoRows_ReturnsEmpty() => ApiOverviewBuilder.Sections([]).Should().BeEmpty();
}
