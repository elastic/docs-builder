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
	public void Load_LegacySynonymsFile_RecordsTheLegacyNameNotTheCopy()
	{
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.Search, "synonyms: []\n", replaceStoredAs: "synonyms.yml");

		var tuning = SearchTuningLoader.Load(provider);

		tuning.SourceFileName.Should().Be("synonyms.yml");
		tuning.Check("search.yml").Findings.Should().ContainSingle().Which.Message.Should().Contain("synonyms.yml");
	}

	[Test]
	public void Load_FileThatParsesButFailsToMap_ThrowsTheMapExceptionAsMainDoes()
	{
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.Search, "rules:\n  - rule_id: r1\n    actions:\n");

		var act = () => SearchTuningLoader.Load(provider);

		act.Should().Throw<NullReferenceException>();
	}

	[Test]
	public void Load_FileThatDoesNotParse_ThrowsTheParsersException()
	{
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.Search, "synonyms: [a, b\n");

		var act = () => SearchTuningLoader.Load(provider);

		var thrown = act.Should().Throw<Exception>().Which;
		thrown.GetType().FullName.Should().Be("SharpYaml.YamlException");
		thrown.Message.Should().Contain("search.yml");
	}

	[Test]
	[Arguments("rules:\n  - rule_id: r1\n    type: pinned\n    actions:\n      ids: [x]\n")]
	[Arguments("rules:\n  - rule_id: r1\n    type: pinned\n    criteria: []\n    actions:\n      ids: [x]\n")]
	public void Check_RuleWithoutCriteria_IsAWarning(string yaml) =>
		Load(yaml).Check("search.yml").Findings.Should().ContainSingle().Which.Message.Should().Be("Query rule 'r1' has no criteria.");

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
