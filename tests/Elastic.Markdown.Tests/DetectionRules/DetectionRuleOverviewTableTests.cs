// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Extensions.DetectionRules;

namespace Elastic.Markdown.Tests.DetectionRules;

public class DetectionRuleOverviewTableTests
{
	private static DetectionRule Rule(string name, string type, string severity, string tags) =>
		DetectionRule.FromToml(
			$"""
			[metadata]
			creation_date = "2024/08/01"
			maturity = "production"

			[rule]
			author = ["Elastic"]
			description = "Test rule"
			name = "{name}"
			rule_id = "{name}-id"
			risk_score = 47
			severity = "{severity}"
			type = "{type}"
			license = "Elastic License v2"
			language = "kuery"
			query = "process.name : evil.exe"
			tags = [{tags}]
			"""
		);

	private static readonly DetectionRuleOverviewEntry[] Entries =
	[
		new(
			"Zebra Rule",
			"/rules/zebra",
			Rule(
				"Zebra Rule",
				"esql",
				"high",
				"\"Domain: Endpoint\", \"OS: Windows\", \"OS: Linux\", \"Tactic: Execution\", \"Tactic: Persistence\""
			)
		),
		new("Alpha Rule", "/rules/alpha", Rule("Alpha Rule", "threat_match", "low", "\"Use Case: Threat Detection\""))
	];

	// AppendLine writes the platform newline, so split on both forms for the Windows build.
	private static string[] Lines() =>
		DetectionRuleOverviewTable.ToMarkdown(Entries).Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

	[Test]
	public void WritesAFilterableTableWithChosenColumns()
	{
		var lines = Lines();
		lines[0].Should().Be(":::{table}");
		lines[1].Should().Be(":filterable:");
		lines[2].Should().Be($":filter-columns: {DetectionRuleOverviewTable.FilterColumns}");
		lines[3].Should().Be("| Rule | Domain | Severity | Rule type | OS | Tactic |");
		lines[^1].Should().Be(":::");
	}

	[Test]
	public void ListsRulesByNameWithLabelsAndJoinedTags()
	{
		var lines = Lines();
		// Alpha sorts first. It has no domain, OS, or tactic tags.
		lines[5].Should().Be("| [Alpha Rule](!/rules/alpha) | Unspecified | Low | Indicator match |  |  |");
		// The ES|QL label escapes its pipe so it stays in one cell.
		lines[6].Should().Be("| [Zebra Rule](!/rules/zebra) | Endpoint | High | ES\\|QL | Windows, Linux | Execution, Persistence |");
	}

	[Test]
	public void ReadsTagValuesByPrefix() => DetectionRuleOverviewTable.TagValues(Entries[0].Rule, "OS").Should().Equal("Windows", "Linux");
}
