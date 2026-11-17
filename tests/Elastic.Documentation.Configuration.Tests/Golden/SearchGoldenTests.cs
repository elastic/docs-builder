// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Search;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What <c>search.yml</c> loads into today.</summary>
public class SearchGoldenTests
{
	private static string Load(string? yaml) => GoldenConfig.Outcome(GoldenConfig.Search, yaml, p => p.CreateSearchConfiguration());

	[Test]
	public void Baseline_Loaded_MatchesSnapshot() => Load(GoldenConfig.BaselineText(GoldenConfig.Search)).Should().MatchSnapshot();

	[Test]
	public void FileMissing_Loaded_Outcome() =>
		Load(null)
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				Exception: Can not read search.yml in directory {config-dir}
				"""
			);

	[Test]
	public void EmptyFile_Loaded_Outcome() =>
		Load("")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.Search.SearchConfigDto'.
		"""
			);

	[Test]
	public void UnknownTopLevelKey_Loaded_Outcome() =>
		Load("bogus: 1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/search.yml: (Lin: 0, Col: 0, Chr: 0) - (Lin: 0, Col: 7, Chr: 7): (1,1) Unknown key 'bogus'. Valid keys: diminish_terms, rules, synonyms.
				"""
			);

	[Test]
	public void EmptyLists_Loaded_Outcome() =>
		Load("synonyms: []\nrules: []\ndiminish_terms: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <SearchConfiguration>:
		  Synonyms: []
		  SynonymBiDirectional: {}
		  Rules: []
		  DiminishTerms: []
		"""
			);

	[Test]
	public void SynonymGroupsOfOneAreDropped_Loaded_Outcome() =>
		Load("synonyms:\n  - [alone]\n  - []\n  - [a, b]\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <SearchConfiguration>:
			  Synonyms:
			    -:
			      - "a"
			      - "b"
			  SynonymBiDirectional:
			    a:
			      - "b"
			    b:
			      - "a"
			  Rules: []
			  DiminishTerms: []
			"""
			);

	[Test]
	public void SynonymGroupsWithTheSameFirstTerm_Loaded_KeepsTheFirst() =>
		Load("synonyms:\n  - [Run, jog]\n  - [run, sprint]\n  - [walk, stroll]\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <SearchConfiguration>:
			  Synonyms:
			    -:
			      - "Run"
			      - "jog"
			    -:
			      - "walk"
			      - "stroll"
			  SynonymBiDirectional:
			    Run:
			      - "jog"
			    jog:
			      - "Run"
			    walk:
			      - "stroll"
			    stroll:
			      - "walk"
			  Rules: []
			  DiminishTerms: []
			"""
			);

	[Test]
	public void SynonymsAsFlatList_Loaded_Outcome() =>
		Load("synonyms: [a, b]\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/search.yml: (Lin: 0, Col: 11, Chr: 11) - (Lin: 0, Col: 12, Chr: 12): Expected a StartSequence token but found 'Scalar'.
				"""
			);

	[Test]
	public void DiminishTermsKeepOrderAndDuplicates_Loaded_Outcome() =>
		Load("diminish_terms: [b, a, b, '']\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <SearchConfiguration>:
			  Synonyms: []
			  SynonymBiDirectional: {}
			  Rules: []
			  DiminishTerms:
			    - "b"
			    - "a"
			    - "b"
			    - ""
			"""
			);

	[Test]
	public void RuleWithAllFields_Loaded_Outcome() =>
		Load(
			"""
			rules:
			  - rule_id: r1
			    type: exclude
			    criteria:
			      - type: fuzzy
			        metadata: query_string
			        values: [a, b]
			    actions:
			      ids: [/docs/a, /docs/b]
			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			model <SearchConfiguration>:
			  Synonyms: []
			  SynonymBiDirectional: {}
			  Rules:
			    - <QueryRule>:
			      RuleId: "r1"
			      Type: Exclude
			      Criteria:
			        - <QueryRuleCriteria>:
			          Type: Fuzzy
			          Metadata: "query_string"
			          Values:
			            - "a"
			            - "b"
			      Actions <QueryRuleActions>:
			        Ids:
			          - "/docs/a"
			          - "/docs/b"
			  DiminishTerms: []
			"""
			);

	[Test]
	public void RuleWithUnknownTypes_Loaded_FallsBackToDefaults() =>
		Load(
			"""
			rules:
			  - rule_id: r1
			    type: banana
			    criteria:
			      - type: banana
			        metadata: m
			        values: [a]
			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			model <SearchConfiguration>:
			  Synonyms: []
			  SynonymBiDirectional: {}
			  Rules:
			    - <QueryRule>:
			      RuleId: "r1"
			      Type: Pinned
			      Criteria:
			        - <QueryRuleCriteria>:
			          Type: Exact
			          Metadata: "m"
			          Values:
			            - "a"
			      Actions <QueryRuleActions>:
			        Ids: []
			  DiminishTerms: []
			"""
			);

	[Test]
	public void RuleTypesInOtherCase_Loaded_Outcome() =>
		Load(
			"""
			rules:
			  - rule_id: r1
			    type: PINNED
			    criteria:
			      - type: PREFIX
			        metadata: m
			        values: []
			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			model <SearchConfiguration>:
			  Synonyms: []
			  SynonymBiDirectional: {}
			  Rules:
			    - <QueryRule>:
			      RuleId: "r1"
			      Type: Pinned
			      Criteria:
			        - <QueryRuleCriteria>:
			          Type: Prefix
			          Metadata: "m"
			          Values: []
			      Actions <QueryRuleActions>:
			        Ids: []
			  DiminishTerms: []
			"""
			);

	[Test]
	public void RuleWithoutAnyField_Loaded_Outcome() =>
		Load("rules:\n  - {}\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <SearchConfiguration>:
		  Synonyms: []
		  SynonymBiDirectional: {}
		  Rules:
		    - <QueryRule>:
		      RuleId: ""
		      Type: Pinned
		      Criteria: []
		      Actions <QueryRuleActions>:
		        Ids: []
		  DiminishTerms: []
		"""
			);

	[Test]
	public void RuleWithBlankIdAndMetadata_Loaded_Outcome() =>
		Load("rules:\n  - rule_id: ''\n    criteria:\n      - metadata: ' '\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <SearchConfiguration>:
			  Synonyms: []
			  SynonymBiDirectional: {}
			  Rules:
			    - <QueryRule>:
			      RuleId: ""
			      Type: Pinned
			      Criteria:
			        - <QueryRuleCriteria>:
			          Type: Exact
			          Metadata: " "
			          Values: []
			      Actions <QueryRuleActions>:
			        Ids: []
			  DiminishTerms: []
			"""
			);

	[Test]
	public void DuplicateRuleIds_Loaded_Outcome() =>
		Load("rules:\n  - rule_id: r\n  - rule_id: r\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <SearchConfiguration>:
		  Synonyms: []
		  SynonymBiDirectional: {}
		  Rules:
		    - <QueryRule>:
		      RuleId: "r"
		      Type: Pinned
		      Criteria: []
		      Actions <QueryRuleActions>:
		        Ids: []
		    - <QueryRule>:
		      RuleId: "r"
		      Type: Pinned
		      Criteria: []
		      Actions <QueryRuleActions>:
		        Ids: []
		  DiminishTerms: []
		"""
			);

	[Test]
	public void UnknownRuleKey_Loaded_Outcome() =>
		Load("rules:\n  - rule_id: r\n    priority: 1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/search.yml: (Lin: 2, Col: 4, Chr: 26) - (Lin: 2, Col: 14, Chr: 36): (3,5) Unknown key 'priority' in $.rules[0]. Valid keys: actions, criteria, rule_id, type.
				"""
			);

	[Test]
	public void UnknownCriteriaKey_Loaded_Outcome() =>
		Load("rules:\n  - rule_id: r\n    criteria:\n      - type: exact\n        operator: eq\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/search.yml: (Lin: 4, Col: 8, Chr: 64) - (Lin: 4, Col: 18, Chr: 74): (5,9) Unknown key 'operator' in $.rules[0].criteria[0]. Valid keys: metadata, type, values.
				"""
			);

	[Test]
	public void RulesAsMap_Loaded_Outcome() =>
		Load("rules:\n  r:\n    type: pinned\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/search.yml: (Lin: 1, Col: 2, Chr: 9) - (Lin: 1, Col: 2, Chr: 9): Expected a StartSequence token but found 'StartMapping'.
				"""
			);

	[Test]
	public void ValuesAsScalar_Loaded_Outcome() =>
		Load("rules:\n  - rule_id: r\n    criteria:\n      - type: exact\n        metadata: m\n        values: single\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/search.yml: (Lin: 5, Col: 16, Chr: 92) - (Lin: 5, Col: 22, Chr: 98): Expected a StartSequence token but found 'Scalar'.
				"""
			);
}
