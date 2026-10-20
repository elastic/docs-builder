// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using SharpYaml;

namespace Elastic.Changelog.Tests.Serialization;

/// <summary>
/// elastic/elasticsearch and elastic/elasticsearch-serverless write changelog entries with <c>summary</c>, <c>area</c>
/// and their own type names. docs-builder reads them as entries in its own format.
/// </summary>
public class ElasticsearchEntryFormatTests
{
	[Test]
	public void DeserializeEntry_MapsSummaryAreaAndType()
	{
		// language=yaml
		const string yaml =
			"""
			pr: 144630
			summary: Limit size of sql query
			area: SQL
			type: bug
			issues: []
			""";

		var entry = ReleaseNotesSerialization.DeserializeEntry(yaml, "docs/changelog/144630.yaml");

		entry.Title.Should().Be("Limit size of sql query");
		entry.Areas.Should().Equal("SQL");
		entry.Type.Should().Be(ChangelogEntryType.BugFix);
		entry.Prs.Should().Equal("144630");
		entry.Issues.Should().BeNull();
		entry.Products.Should().BeNull("the entry has no products; they come from products.default where it is used");
	}

	[Test]
	public void DeserializeEntry_SplitsACommaSeparatedArea()
	{
		const string yaml = "pr: 8003\nsummary: Add shard defaults\narea: \"Data streams, TSDB\"\ntype: enhancement\nissues: [123]\n";

		var entry = ReleaseNotesSerialization.DeserializeEntry(yaml);

		entry.Areas.Should().Equal("Data streams", "TSDB");
		entry.Issues.Should().Equal("123");
	}

	[Test]
	[Arguments("bug", ChangelogEntryType.BugFix)]
	[Arguments("breaking", ChangelogEntryType.BreakingChange)]
	[Arguments("breaking-java", ChangelogEntryType.BreakingChange)]
	[Arguments("new-aggregation", ChangelogEntryType.Feature)]
	[Arguments("upgrade", ChangelogEntryType.Other)]
	[Arguments("enhancement", ChangelogEntryType.Enhancement)]
	[Arguments("known-issue", ChangelogEntryType.KnownIssue)]
	public void DeserializeEntry_MapsElasticsearchTypes(string type, ChangelogEntryType expected)
	{
		var entry = ReleaseNotesSerialization.DeserializeEntry($"pr: 1\nsummary: A change\narea: Search\ntype: {type}\n");

		entry.Type.Should().Be(expected);
	}

	[Test]
	public void DeserializeEntry_BreakingBlockFeedsDescriptionAndImpact()
	{
		// language=yaml
		const string yaml =
			"""
			pr: 159603
			summary: Add `array` and `binary` fields API formats to `dense_vector` field
			area: Search
			type: breaking
			issues: []
			breaking:
			  area: Search
			  title: Add `array` and `binary` fields API formats to `dense_vector` field
			  details: Changes the `dense_vector` default fields API format to `array`
			  impact: Users of string-encoded `dense_vector` values see arrays of numbers instead.
			  notable: false
			""";

		var entry = ReleaseNotesSerialization.DeserializeEntry(yaml);

		entry.Type.Should().Be(ChangelogEntryType.BreakingChange);
		entry.Description.Should().Be("Changes the `dense_vector` default fields API format to `array`");
		entry.Impact.Should().Be("Users of string-encoded `dense_vector` values see arrays of numbers instead.");
		entry.Highlight.Should().BeNull();
	}

	[Test]
	public void DeserializeEntry_HighlightBlockMarksTheEntryAsAHighlight()
	{
		// language=yaml
		const string yaml =
			"""
			pr: 154765
			summary: Support match_phrase on runtime expressions
			area: ES|QL
			type: enhancement
			issues: []
			highlight:
			  title: match_phrase can now operate on runtime expressions
			  body: |-
			    The `match_phrase` function can now work with runtime expressions.
			  notable: true
			""";

		var entry = ReleaseNotesSerialization.DeserializeEntry(yaml);

		entry.Highlight.Should().BeTrue();
		entry.Description.Should().Be("The `match_phrase` function can now work with runtime expressions.");
	}

	[Test]
	public void DeserializeEntry_SourceRepoQualifiesThePr()
	{
		var entry = ReleaseNotesSerialization.DeserializeEntry(
			"pr: 42\nsummary: A change\narea: Search\ntype: bug\nsource_repo: elasticsearch-serverless\n"
		);

		entry.Prs.Should().Equal("elastic/elasticsearch-serverless#42");
	}

	[Test]
	public void DeserializeEntry_UnknownKeyInElasticsearchFormat_StillThrows()
	{
		var act = () => ReleaseNotesSerialization.DeserializeEntry("pr: 1\nsummary: A change\narea: Search\ntype: bug\nsumary: typo\n");

		act.Should().Throw<YamlException>().Which.Message.Should().Contain("Unknown key 'sumary'");
	}

	[Test]
	public void NormalizeYaml_LeavesTheDocsBuilderFormatAlone()
	{
		const string yaml = "title: A change\ntype: bug-fix\nareas:\n  - Search\nproducts:\n  - product: elasticsearch\n";

		ReleaseNotesSerialization.IsElasticsearchEntryFormat(yaml).Should().BeFalse();
		ReleaseNotesSerialization.NormalizeYaml(yaml).Should().Be(yaml);
	}
}
