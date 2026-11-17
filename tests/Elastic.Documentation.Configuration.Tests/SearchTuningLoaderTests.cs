// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Configuration.Tests;

public class SearchTuningLoaderTests
{
	private static SearchTuning Load(string yaml)
	{
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.Search, yaml);
		return SearchTuningLoader.Load(provider);
	}

	[Test]
	public void Load_RuleTypes_KeepTheTextTheFileWrote()
	{
		var tuning = Load(
			"rules:\n  - rule_id: r1\n    type: banana\n    criteria:\n      - type: PREFIX\n        metadata: m\n        values: [a]\n"
		);

		var rule = tuning.Rules.Single();
		rule.Type.Should().Be(QueryRuleType.Pinned);
		rule.TypeRaw.Should().Be("banana");
		rule.Criteria.Single().TypeRaw.Should().Be("PREFIX");
	}

	[Test]
	public void Load_SearchFile_RecordsItsName() => Load("synonyms: []\n").SourceFileName.Should().Be("search.yml");

	[Test]
	public void Check_UnknownRuleType_IsAWarningThatReadsTheFileAsPinned()
	{
		var tuning = Load(
			"rules:\n  - rule_id: r1\n    type: banana\n    criteria:\n      - type: exact\n        metadata: m\n        values: [a]\n    actions:\n      ids: [x]\n"
		);

		var findings = tuning.Check("search.yml").Findings;

		findings.Should().ContainSingle().Which.Severity.Should().Be(Severity.Warning);
		findings[0].Message.Should().Be("Query rule 'r1' has type 'banana', which is not pinned or exclude, so it is read as pinned.");
	}

	[Test]
	public void Check_CleanFile_HasNoFindings() =>
		Load(
			"synonyms:\n  - [a, b]\nrules:\n  - rule_id: r1\n    type: exclude\n    criteria:\n      - type: exact\n        metadata: m\n        values: [a]\n    actions:\n      ids: [x]\n"
		).Check("search.yml").Findings.Should().BeEmpty();
}
