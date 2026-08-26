// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Supplemental;

namespace Elastic.ApiExplorer.Tests.Supplemental;

public class ApiSupplementalDocTests
{
	[Fact]
	public void Parse_Null_ReturnsNull()
	{
		ApiSupplementalDoc.Parse(null).Should().BeNull();
	}

	[Fact]
	public void Parse_FrontMatterOnly_PreservesFrontMatterWithNullDescription()
	{
		const string raw = """
			---
			applies_to:
			  stack: preview
			---
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.FrontMatter.Should().Contain("applies_to:");
		doc.FrontMatter.Should().Contain("stack: preview");
		doc.Description.Should().BeNull();
		doc.ParameterOverrides.Should().BeEmpty();
		doc.RequestBodyOverrides.Should().BeEmpty();
		doc.PostSections.Should().BeEmpty();
	}

	[Fact]
	public void Parse_NoHeadings_EntireBodyIsDescription()
	{
		const string raw = """
			The search API returns hits that match the query.

			It supports aggregations.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.FrontMatter.Should().BeNull();
		doc.Description.Should().Be("The search API returns hits that match the query.\n\nIt supports aggregations.");
		doc.PostSections.Should().BeEmpty();
	}

	[Fact]
	public void Parse_CrlfInput_NormalizesToLf()
	{
		const string raw = "The search API returns hits that match the query.\r\n\r\nIt supports aggregations.";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("The search API returns hits that match the query.\n\nIt supports aggregations.");
	}

	[Fact]
	public void Parse_NoHeadingsWithFrontMatter_StripsFrontMatterFromDescription()
	{
		const string raw = """
			---
			description: Metadata description.
			---

			User-facing supplemental description.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.FrontMatter.Should().Contain("description: Metadata description.");
		doc.Description.Should().Be("User-facing supplemental description.");
		doc.Description.Should().NotContain("Metadata description.");
	}

	[Fact]
	public void Parse_DescriptionHeading_IsolatesDescriptionFromOtherSections()
	{
		const string raw = """
			## Description

			The search API returns hits that match the query.

			## Usage examples

			Use `search_after` for deep pagination.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("The search API returns hits that match the query.");
		doc.PostSections.Should().ContainSingle()
			.Which.Should().Be(new ApiSupplementalSection("Usage examples", "Use `search_after` for deep pagination."));
	}

	[Fact]
	public void Parse_ParametersHeading_MapsDefinitionList()
	{
		const string raw = """
			## Parameters

			: `allow_no_indices`
			  If `false`, missing indices return an error.

			: `expand_wildcards`
			  Type of index that wildcard patterns can match.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.Description.Should().BeNull();
		doc.ParameterOverrides.Should().HaveCount(2);
		doc.ParameterOverrides["allow_no_indices"].Should().Be("If `false`, missing indices return an error.");
		doc.ParameterOverrides["expand_wildcards"].Should().Be("Type of index that wildcard patterns can match.");
	}

	[Fact]
	public void Parse_QueryAndPathParameterHeadings_ShareOneMap()
	{
		const string raw = """
			## Query parameters

			: `q`
			  Query string.

			## Path parameters

			: `index`
			  Comma-separated list of data streams.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.ParameterOverrides.Should().HaveCount(2);
		doc.ParameterOverrides["q"].Should().Be("Query string.");
		doc.ParameterOverrides["index"].Should().Be("Comma-separated list of data streams.");
	}

	[Fact]
	public void Parse_RequestBodyHeading_MapsDefinitionList()
	{
		const string raw = """
			## Request body

			: `query`
			  Defines the search query using Query DSL.

			: `aggs`
			  Aggregations to compute over the result set.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.RequestBodyOverrides.Should().HaveCount(2);
		doc.RequestBodyOverrides["query"].Should().Be("Defines the search query using Query DSL.");
		doc.RequestBodyOverrides["aggs"].Should().Be("Aggregations to compute over the result set.");
		doc.ParameterOverrides.Should().BeEmpty();
	}

	[Fact]
	public void Parse_BacktickAndBareName_ProduceSameKey()
	{
		const string raw = """
			## Parameters

			: `allow_no_indices`
			  Backtick form.

			: allow_no_indices
			  Bare form.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.ParameterOverrides.Should().ContainSingle()
			.Which.Key.Should().Be("allow_no_indices");
		doc.ParameterOverrides["allow_no_indices"].Should().Be("Bare form.");
	}

	[Fact]
	public void Parse_UnrecognizedHeadings_CollectInDocumentOrder()
	{
		const string raw = """
			## Description

			Operation description.

			## Best practices

			Avoid deep pagination.

			## Common patterns

			Use filters with aggregations.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("Operation description.");
		doc.PostSections.Should().Equal(
			new ApiSupplementalSection("Best practices", "Avoid deep pagination."),
			new ApiSupplementalSection("Common patterns", "Use filters with aggregations."));
	}

	[Fact]
	public void Parse_TagStyleFile_LeavesOverrideMapsEmpty()
	{
		const string raw = """
			## Description

			Machine learning anomaly detection APIs.

			## Getting started

			Create a job, then open it.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("Machine learning anomaly detection APIs.");
		doc.ParameterOverrides.Should().BeEmpty();
		doc.RequestBodyOverrides.Should().BeEmpty();
		doc.PostSections.Should().ContainSingle()
			.Which.Should().Be(new ApiSupplementalSection("Getting started", "Create a job, then open it."));
	}

	[Fact]
	public void Parse_NestedH3_DoesNotStartNewSection()
	{
		const string raw = """
			## Usage examples

			Intro.

			### Full-text search

			Details stay in this section.
			""";

		var doc = ApiSupplementalDoc.Parse(raw);

		doc.Should().NotBeNull();
		doc.PostSections.Should().ContainSingle();
		doc.PostSections[0].Heading.Should().Be("Usage examples");
		doc.PostSections[0].Body.Should().Contain("### Full-text search");
		doc.PostSections[0].Body.Should().Contain("Details stay in this section.");
	}
}
