// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using SharpYaml;

namespace Elastic.Changelog.Tests.Serialization;

/// <summary>
/// Entry formats are data in config/changelog-entry-formats.yml. These tests read entries through the formats that
/// ship with docs-builder, and through formats defined in the test, the way a repository's own files would be read.
/// </summary>
public class ChangelogEntryFormatTests
{
	private static ChangelogEntry Read(string format, string yaml) =>
		ReleaseNotesSerialization.DeserializeEntry(ChangelogEntryFormat.All[format].ToEntryYaml(yaml, "test.yaml"));

	[Test]
	public void BuiltInFormats_Load() => ChangelogEntryFormat.All.Keys.Should().Contain(["elasticsearch", "elastic-agent-changelog-tool"]);

	[Test]
	public void Elasticsearch_MapsSummaryAreaAndType()
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

		var entry = Read("elasticsearch", yaml);

		entry.Title.Should().Be("Limit size of sql query");
		entry.Areas.Should().Equal("SQL");
		entry.Type.Should().Be(ChangelogEntryType.BugFix);
		entry.Prs.Should().Equal("144630");
		entry.Issues.Should().BeNull();
		entry.Products.Should().BeNull("formats have no products; they come from the configuration where the entry is used");
	}

	[Test]
	public void Elasticsearch_SplitsACommaSeparatedArea()
	{
		var entry = Read(
			"elasticsearch",
			"pr: 8003\nsummary: Add shard defaults\narea: \"Data streams, TSDB\"\ntype: enhancement\nissues: [123]\n"
		);

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
	public void Elasticsearch_MapsTypes(string type, ChangelogEntryType expected) =>
		Read("elasticsearch", $"pr: 1\nsummary: A change\narea: Search\ntype: {type}\n").Type.Should().Be(expected);

	[Test]
	public void Elasticsearch_BreakingBlockFeedsDescriptionAndImpact()
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

		var entry = Read("elasticsearch", yaml);

		entry.Type.Should().Be(ChangelogEntryType.BreakingChange);
		entry.Description.Should().Be("Changes the `dense_vector` default fields API format to `array`");
		entry.Impact.Should().Be("Users of string-encoded `dense_vector` values see arrays of numbers instead.");
		entry.Highlight.Should().BeNull();
	}

	[Test]
	public void Elasticsearch_HighlightBlockMarksTheEntryAsAHighlight()
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

		var entry = Read("elasticsearch", yaml);

		entry.Highlight.Should().BeTrue();
		entry.Description.Should().Be("The `match_phrase` function can now work with runtime expressions.");
	}

	[Test]
	public void Elasticsearch_SourceRepoGoesToPrsAndKeepsPrOut()
	{
		var entry = Read("elasticsearch", "pr: 42\nsummary: A change\narea: Search\ntype: bug\nsource_repo: elasticsearch-serverless\n");

		entry.Prs.Should().Equal("elastic/elasticsearch-serverless#42");
	}

	[Test]
	public void Elasticsearch_UnknownKey_Throws()
	{
		var act =
			() => ChangelogEntryFormat.All["elasticsearch"].ToEntryYaml(
				"pr: 1\nsummary: A change\narea: Search\ntype: bug\nsumary: typo\n",
				"1.yaml"
			);

		act
			.Should()
			.Throw<YamlException>()
			.Which
			.Message
			.Should()
			.Contain("Unknown key 'sumary'")
			.And
			.Contain("'elasticsearch' entry format");
	}

	[Test]
	public void Elasticsearch_UnknownNestedKey_Throws()
	{
		const string yaml = "pr: 1\nsummary: A change\narea: Search\ntype: breaking\nbreaking:\n  area: Search\n  detials: typo\n";

		var act = () => ChangelogEntryFormat.All["elasticsearch"].ToEntryYaml(yaml, "1.yaml");

		act.Should().Throw<YamlException>().Which.Message.Should().Contain("Unknown key 'breaking.detials'");
	}

	[Test]
	public void ElasticAgentChangelogTool_MapsKindComponentAndUrls()
	{
		// language=yaml
		const string yaml =
			"""
			kind: bug-fix
			summary: Fix the Filebeat registry on restart
			description: The registry is no longer lost when Filebeat restarts.
			component: filebeat
			pr: https://github.com/elastic/beats/pull/47256
			issue: https://github.com/elastic/beats/issues/47000
			""";

		var entry = Read("elastic-agent-changelog-tool", yaml);

		entry.Title.Should().Be("Fix the Filebeat registry on restart");
		entry.Type.Should().Be(ChangelogEntryType.BugFix);
		entry.Areas.Should().Equal("filebeat");
		entry.Prs.Should().Equal("https://github.com/elastic/beats/pull/47256");
		entry.Issues.Should().Equal("https://github.com/elastic/beats/issues/47000");
		entry.Description.Should().Be("The registry is no longer lost when Filebeat restarts.");
	}

	[Test]
	public void ElasticAgentChangelogTool_PrList() =>
		Read(
			"elastic-agent-changelog-tool",
			"kind: feature\nsummary: A feature\ncomponent: all\npr:\n  - https://github.com/elastic/beats/pull/1\n  - https://github.com/elastic/beats/pull/2\n"
		).Prs.Should().Equal("https://github.com/elastic/beats/pull/1", "https://github.com/elastic/beats/pull/2");

	[Test]
	public void CustomFormat_IsDefinedAsData()
	{
		// language=yaml
		const string formats =
			"""
			formats:
			  acme:
			    fields:
			      title: { from: [headline, summary] }
			      type: { from: category }
			      areas: { from: tags.area }
			    values:
			      type: { fix: bug-fix }
			    ignore: [tags.owner]
			""";
		var acme = ChangelogEntryFormat.Parse(formats)["acme"];

		var entry = ReleaseNotesSerialization.DeserializeEntry(
			acme.ToEntryYaml("summary: Faster widgets\ncategory: fix\ntags:\n  area: [Widgets, Speed]\n  owner: team-a\n")
		);

		entry.Title.Should().Be("Faster widgets");
		entry.Type.Should().Be(ChangelogEntryType.BugFix);
		entry.Areas.Should().Equal("Widgets", "Speed");
	}

	[Test]
	public void CustomFormat_UnknownTargetField_IsRejected()
	{
		var act = () => ChangelogEntryFormat.Parse("formats:\n  acme:\n    fields:\n      headline: { from: summary }\n");

		act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("unknown field 'headline'");
	}
}
