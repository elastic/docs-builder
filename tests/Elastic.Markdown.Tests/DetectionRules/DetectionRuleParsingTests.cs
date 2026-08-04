// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Extensions.DetectionRules;

namespace Elastic.Markdown.Tests.DetectionRules;

public class DetectionRuleParsingTests
{
	private const string MinimalRule = """
		[metadata]
		creation_date = "2024/08/01"
		maturity = "production"

		[rule]
		author = ["Elastic"]
		description = "Test rule"
		name = "Test Rule"
		rule_id = "abc-123"
		risk_score = 47
		severity = "medium"
		type = "query"
		license = "Elastic License v2"
		language = "kuery"
		query = "process.name : evil.exe"
		""";

	[Fact]
	public void FromToml_MinimalRule_ParsesCorrectly()
	{
		var rule = DetectionRule.FromToml(MinimalRule);

		rule.Name.Should().Be("Test Rule");
		rule.RuleId.Should().Be("abc-123");
		rule.Type.Should().Be("query");
		rule.RiskScore.Should().Be(47);
		rule.Severity.Should().Be("medium");
		rule.Authors.Should().ContainSingle().Which.Should().Be("Elastic");
	}

	[Fact]
	public void FromToml_ImplicitIntermediateTable_ParsesTransformInvestigate()
	{
		var toml = MinimalRule + """

			[[transform.investigate]]
			label = "Alerts associated with the user"
			description = ""
			providers = []

			[[transform.investigate]]
			label = "Alerts associated with the host"
			description = ""
			providers = []

			[[rule.threat]]
			framework = "MITRE ATT&CK"
			[rule.threat.tactic]
			id = "TA0011"
			name = "Command and Control"
			reference = "https://attack.mitre.org/tactics/TA0011/"
			[[rule.threat.technique]]
			id = "T1071"
			name = "Application Layer Protocol"
			reference = "https://attack.mitre.org/techniques/T1071/"
			""";

		var rule = DetectionRule.FromToml(toml);

		rule.Threats.Should().HaveCount(1);
		rule.Threats[0].Tactic.Id.Should().Be("TA0011");
		rule.Threats[0].Techniques.Should().HaveCount(1);
		rule.Threats[0].Techniques[0].Id.Should().Be("T1071");
	}

	[Fact]
	public void FromToml_MultiLineStringWithMarkdownLinks_ParsesCorrectly()
	{
		// TOML uses """ for multi-line strings; use 4-quote C# raw literals to embed them
		var toml = """"
			[metadata]
			creation_date = "2024/08/01"
			maturity = "production"

			[rule]
			author = ["Elastic"]
			description = "Test rule"
			name = "Test Rule With Setup"
			rule_id = "abc-456"
			risk_score = 73
			severity = "high"
			type = "esql"
			license = "Elastic License v2"
			setup = """
			## Setup

			Follow the [helper guide](https://www.elastic.co/docs/some/path#anchor) to configure.

			Also see [another link](https://example.com).
			"""
			"""";

		var rule = DetectionRule.FromToml(toml);

		rule.Name.Should().Be("Test Rule With Setup");
		rule.Setup.Should().Contain("[helper guide]");
		rule.Setup.Should().Contain("elastic.co/docs");
	}

	[Fact]
	public void FromToml_MixedMultiLineDelimiters_ParsesCorrectly()
	{
		// Triple-quoted """ appears inside a '''-delimited multi-line string
		var toml = """"
			[metadata]
			creation_date = "2024/08/01"
			maturity = "production"

			[rule]
			author = ["Elastic"]
			description = "Test rule"
			name = "Mixed Delimiters"
			rule_id = "abc-789"
			risk_score = 50
			severity = "medium"
			type = "esql"
			license = "Elastic License v2"
			query = '''
			from logs-endpoint.events.process-*
			| grok process.command_line """e=Access&y=Guest&h=(?<Esql.server>[^&]+)&p"""
			| where Esql.server is not null
			'''
			note = """## Triage
			Check the process tree."""
			"""";

		var rule = DetectionRule.FromToml(toml);

		rule.Query.Should().Contain("grok process.command_line");
		rule.Query.Should().Contain("e=Access");
		rule.Note.Should().Contain("Triage");
	}

	[Fact]
	public void FromToml_DeprecatedRule_ParsesDeprecationDate()
	{
		var toml = """
			[metadata]
			creation_date = "2024/08/01"
			deprecation_date = "2025/03/15"
			maturity = "deprecated"

			[rule]
			author = ["Elastic"]
			description = "Deprecated rule"
			name = "Old Rule"
			rule_id = "dep-001"
			risk_score = 21
			severity = "low"
			type = "query"
			license = "Elastic License v2"
			""";

		var rule = DetectionRule.FromToml(toml);

		rule.DeprecationDate.Should().Be("2025/03/15");
		rule.Maturity.Should().Be("deprecated");
	}

	[Fact]
	public void FromToml_ThreatWithSubTechniques_ParsesFullHierarchy()
	{
		var toml = MinimalRule + """

			[[rule.threat]]
			framework = "MITRE ATT&CK"
			[rule.threat.tactic]
			id = "TA0001"
			name = "Initial Access"
			reference = "https://attack.mitre.org/tactics/TA0001/"
			[[rule.threat.technique]]
			id = "T1566"
			name = "Phishing"
			reference = "https://attack.mitre.org/techniques/T1566/"
			[[rule.threat.technique.subtechnique]]
			id = "T1566.001"
			name = "Spearphishing Attachment"
			reference = "https://attack.mitre.org/techniques/T1566/001/"
			[[rule.threat.technique.subtechnique]]
			id = "T1566.002"
			name = "Spearphishing Link"
			reference = "https://attack.mitre.org/techniques/T1566/002/"
			""";

		var rule = DetectionRule.FromToml(toml);

		rule.Threats.Should().HaveCount(1);
		var technique = rule.Threats[0].Techniques[0];
		technique.Id.Should().Be("T1566");
		technique.SubTechniques.Should().HaveCount(2);
		technique.SubTechniques[0].Id.Should().Be("T1566.001");
		technique.SubTechniques[1].Id.Should().Be("T1566.002");
	}

	[Fact]
	public void FromToml_MultipleThreats_ParsesAll()
	{
		var toml = MinimalRule + """

			[[rule.threat]]
			framework = "MITRE ATT&CK"
			[rule.threat.tactic]
			id = "TA0011"
			name = "Command and Control"
			reference = "https://attack.mitre.org/tactics/TA0011/"

			[[rule.threat]]
			framework = "MITRE ATT&CK"
			[rule.threat.tactic]
			id = "TA0005"
			name = "Defense Evasion"
			reference = "https://attack.mitre.org/tactics/TA0005/"
			""";

		var rule = DetectionRule.FromToml(toml);

		rule.Threats.Should().HaveCount(2);
		rule.Threats[0].Tactic.Id.Should().Be("TA0011");
		rule.Threats[1].Tactic.Id.Should().Be("TA0005");
	}

	[Fact]
	public void FromToml_OptionalFieldsMissing_DefaultsCorrectly()
	{
		var rule = DetectionRule.FromToml(MinimalRule);

		rule.Setup.Should().BeNull();
		rule.Note.Should().BeNull();
		rule.References.Should().BeNull();
		rule.Indices.Should().BeNull();
		rule.RunsEvery.Should().BeNull();
		rule.IndicesFromDateMath.Should().BeNull();
		rule.DeprecationDate.Should().BeNull();
		rule.MaximumAlertsPerExecution.Should().Be(100);
		rule.Threats.Should().BeEmpty();
	}

	[Fact]
	public void FromToml_DomainTag_ExtractedCorrectly()
	{
		var toml = """
			[metadata]
			creation_date = "2024/08/01"
			maturity = "production"

			[rule]
			author = ["Elastic"]
			description = "Test"
			name = "Domain Test"
			rule_id = "dom-001"
			risk_score = 50
			severity = "medium"
			type = "query"
			license = "Elastic License v2"
			tags = ["Domain: Endpoint", "Use Case: Threat Detection"]
			""";

		var rule = DetectionRule.FromToml(toml);

		rule.Domain.Should().Be("Endpoint");
	}
}
