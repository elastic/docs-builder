// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What <c>legacy-url-mappings.yml</c> loads into today, against the baseline <c>products.yml</c>.</summary>
public class LegacyUrlMappingsGoldenTests
{
	private static string Load(string yaml) =>
		GoldenConfig.Outcome(
			GoldenConfig.LegacyUrlMappings,
			yaml,
			p => p.CreateLegacyUrlMappings(GoldenConfig.LoadProducts()),
			value => value is Product product ? $"<Product {product.Id}>" : null
		);

	[Test]
	public void Baseline_Loaded_MatchesSnapshot() =>
		Load(GoldenConfig.BaselineText(GoldenConfig.LegacyUrlMappings)).Should().MatchSnapshot();

	[Test]
	public void EmptyFile_Loaded_Outcome() =>
		Load("")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.LegacyUrlMappings.LegacyUrlMappingConfigDto'.
		"""
			);

	[Test]
	public void EmptyMappings_Loaded_Outcome() =>
		Load("mappings: {}\n").Should().MatchInlineSnapshot("""
		model <LegacyUrlMappingConfiguration>:
		  Mappings: []
		""");

	[Test]
	public void OnlyStackList_Loaded_Outcome() =>
		Load("stack: ['8.19', '8.18']\n")
			.Should()
			.MatchInlineSnapshot("""
		model <LegacyUrlMappingConfiguration>:
		  Mappings: []
		""");

	[Test]
	public void UnknownTopLevelKey_Loaded_Outcome() =>
		Load("bogus: 1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/legacy-url-mappings.yml: (Lin: 0, Col: 0, Chr: 0) - (Lin: 0, Col: 7, Chr: 7): (1,1) Unknown key 'bogus'. Valid keys: mappings, stack.
				"""
			);

	[Test]
	public void UnknownMappingKey_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: apm\n    legacy_versions: []\n    note: x\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/legacy-url-mappings.yml: (Lin: 4, Col: 4, Chr: 66) - (Lin: 4, Col: 10, Chr: 72): (5,5) Unknown key 'note' in $.mappings['en/apm/']. Valid keys: legacy_versions, product.
				"""
			);

	[Test]
	public void UnknownProduct_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: not-a-product\n    legacy_versions: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			KeyNotFoundException: The given key 'not-a-product' was not present in the dictionary.
			"""
			);

	[Test]
	public void MissingProduct_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    legacy_versions: ['1.x']\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			KeyNotFoundException: The given key '' was not present in the dictionary.
			"""
			);

	[Test]
	public void BlankProduct_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: '  '\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		KeyNotFoundException: The given key '  ' was not present in the dictionary.
		"""
			);

	[Test]
	public void MissingLegacyVersions_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: apm\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <LegacyUrlMappingConfiguration>:
		  Mappings:
		    - <LegacyUrlMapping>:
		      BaseUrl: "en/apm/"
		      Product: <Product apm>
		      LegacyVersions: []
		"""
			);

	[Test]
	public void LegacyVersionsAsScalar_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: apm\n    legacy_versions: '1.x'\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/legacy-url-mappings.yml: (Lin: 3, Col: 21, Chr: 59) - (Lin: 3, Col: 26, Chr: 64): Expected a StartSequence token but found 'Scalar'.
				"""
			);

	[Test]
	public void LegacyVersionsAsNumbers_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: apm\n    legacy_versions: [1.5, 2, 3.0]\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <LegacyUrlMappingConfiguration>:
			  Mappings:
			    - <LegacyUrlMapping>:
			      BaseUrl: "en/apm/"
			      Product: <Product apm>
			      LegacyVersions:
			        - "1.5"
			        - "2"
			        - "3.0"
			"""
			);

	[Test]
	public void LegacyVersionsWithDuplicatesAndBlanks_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: apm\n    legacy_versions: ['1.x', '1.x', '', ' ']\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <LegacyUrlMappingConfiguration>:
			  Mappings:
			    - <LegacyUrlMapping>:
			      BaseUrl: "en/apm/"
			      Product: <Product apm>
			      LegacyVersions:
			        - "1.x"
			        - "1.x"
			        - ""
			        - " "
			"""
			);

	[Test]
	public void MappingsAsList_Loaded_Outcome() =>
		Load("mappings:\n  - en/apm/\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/legacy-url-mappings.yml: (Lin: 1, Col: 2, Chr: 12) - (Lin: 1, Col: 2, Chr: 12): Expected a StartMapping token but found 'StartSequence'.
				"""
			);

	[Test]
	public void MappingBodyAsScalar_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/: apm\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/legacy-url-mappings.yml: (Lin: 1, Col: 11, Chr: 21) - (Lin: 1, Col: 14, Chr: 24): Expected a StartMapping token but found 'Scalar'.
				"""
			);

	[Test]
	public void DuplicateBaseUrl_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: apm\n  en/apm/:\n    product: kibana\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/legacy-url-mappings.yml: (Lin: 5, Col: 0, Chr: 69) - (Lin: 5, Col: 0, Chr: 69): Duplicate mapping key 'en/apm/'.
				"""
			);

	[Test]
	public void BaseUrlsDifferingByTrailingSlashAndCase_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: apm\n  en/apm:\n    product: apm\n  EN/APM/:\n    product: apm\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <LegacyUrlMappingConfiguration>:
				  Mappings:
				    - <LegacyUrlMapping>:
				      BaseUrl: "en/apm/"
				      Product: <Product apm>
				      LegacyVersions: []
				    - <LegacyUrlMapping>:
				      BaseUrl: "en/apm"
				      Product: <Product apm>
				      LegacyVersions: []
				    - <LegacyUrlMapping>:
				      BaseUrl: "EN/APM/"
				      Product: <Product apm>
				      LegacyVersions: []
				"""
			);

	[Test]
	public void BlankBaseUrl_Loaded_Outcome() =>
		Load("mappings:\n  '':\n    product: apm\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <LegacyUrlMappingConfiguration>:
		  Mappings:
		    - <LegacyUrlMapping>:
		      BaseUrl: ""
		      Product: <Product apm>
		      LegacyVersions: []
		"""
			);

	[Test]
	public void BaseUrlThatIsAnAbsoluteUri_Loaded_Outcome() =>
		Load("mappings:\n  https://example.com/en/apm/:\n    product: apm\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <LegacyUrlMappingConfiguration>:
			  Mappings:
			    - <LegacyUrlMapping>:
			      BaseUrl: "https://example.com/en/apm/"
			      Product: <Product apm>
			      LegacyVersions: []
			"""
			);

	[Test]
	public void ProductThatIsNotPublicReference_Loaded_Outcome() =>
		Load("mappings:\n  en/x/:\n    product: agent-skills\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <LegacyUrlMappingConfiguration>:
			  Mappings:
			    - <LegacyUrlMapping>:
			      BaseUrl: "en/x/"
			      Product: <Product agent-skills>
			      LegacyVersions: []
			"""
			);

	[Test]
	public void ProductIdInDifferentCase_Loaded_Outcome() =>
		Load("mappings:\n  en/apm/:\n    product: APM\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			KeyNotFoundException: The given key 'APM' was not present in the dictionary.
			"""
			);

	[Test]
	public void AnchorAndAliasInVersions_Loaded_Outcome() =>
		Load(
			"mappings:\n  en/apm/:\n    product: apm\n    legacy_versions: &v ['1.x', '2.x']\n  en/kibana/:\n    product: kibana\n    legacy_versions: *v\n"
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				model <LegacyUrlMappingConfiguration>:
				  Mappings:
				    - <LegacyUrlMapping>:
				      BaseUrl: "en/apm/"
				      Product: <Product apm>
				      LegacyVersions:
				        - "1.x"
				        - "2.x"
				    - <LegacyUrlMapping>:
				      BaseUrl: "en/kibana/"
				      Product: <Product kibana>
				      LegacyVersions:
				        - "1.x"
				        - "2.x"
				"""
			);
}
