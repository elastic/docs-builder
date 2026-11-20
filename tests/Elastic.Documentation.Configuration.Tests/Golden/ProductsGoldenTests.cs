// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What <c>products.yml</c> loads into today, against the baseline <c>versions.yml</c>.</summary>
public class ProductsGoldenTests
{
	private static string Load(string yaml) =>
		GoldenConfig.Outcome(
			GoldenConfig.Products,
			yaml,
			p => p.CreateProducts(p.CreateVersionConfiguration()),
			"PublicReferenceProducts",
			"ProductDisplayNames",
			"DisplayNameLookup"
		);

	[Test]
	public void Baseline_Products_MatchSnapshot() => ModelDump.ToText(GoldenConfig.LoadProducts().Products).Should().MatchSnapshot();

	[Test]
	public void Baseline_DerivedLookups_MatchSnapshot()
	{
		var config = GoldenConfig.LoadProducts();
		var sb = new StringBuilder();
		_ = sb.AppendLine("public-reference products:");
		foreach (var id in config.PublicReferenceProducts.Keys.Order(StringComparer.Ordinal))
			_ = sb.Append("  ").AppendLine(id);
		_ = sb.AppendLine("not public-reference:");
		foreach (var id in config.Products.Keys.Except(config.PublicReferenceProducts.Keys).Order(StringComparer.Ordinal))
			_ = sb.Append("  ").AppendLine(id);
		_ = sb.AppendLine("display names:");
		foreach (var (id, name) in config.ProductDisplayNames.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
			_ = sb.Append("  ").Append(id).Append(": ").AppendLine(name);
		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void Baseline_RepositoryLookups_MatchSnapshot()
	{
		var config = GoldenConfig.LoadProducts();
		var sb = new StringBuilder();
		foreach (var repository in new[]
		{
			"elasticsearch",
			"elastic/kibana",
			"private-repo-2",
			"docs-content",
			"does-not-exist",
			"ELASTIC-TRANSPORT-GO"
		})
		{
			var products = config.GetProductsByRepositoryName(repository);
			_ = sb.Append(repository).Append(" -> ").Append(string.Join(", ", products.Select(p => p.Id)));
			_ = sb.Append(" | single: ").AppendLine(config.GetProductByRepositoryName(repository)?.Id ?? "null");
		}
		_ = sb.Append("display of unknown id: ").AppendLine(config.GetDisplayName("nope"));
		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void EmptyFile_Loaded_Outcome() =>
		Load("")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.Products.ProductConfigDto'.
		"""
			);

	[Test]
	public void EmptyProducts_Loaded_Outcome() =>
		Load("products: {}\n").Should().MatchInlineSnapshot("""
		model <ProductsConfiguration>:
		  Products: {}
		""");

	[Test]
	public void UnknownTopLevelKey_Loaded_Outcome() =>
		Load("bogus: 1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/products.yml: (Lin: 0, Col: 0, Chr: 0) - (Lin: 0, Col: 7, Chr: 7): (1,1) Unknown key 'bogus'. Valid keys: products.
				"""
			);

	[Test]
	public void UnknownProductKey_Loaded_Outcome() =>
		Load("products:\n  apm:\n    display: APM\n    versioning: stack\n    colour: red\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/products.yml: (Lin: 4, Col: 4, Chr: 60) - (Lin: 4, Col: 12, Chr: 68): (5,5) Unknown key 'colour' in $.products.apm. Valid keys: display, dra_artifact, features, repository, versioning.
				"""
			);

	[Test]
	public void ProductWithoutBody_Loaded_Outcome() =>
		Load("products:\n  apm:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		NullReferenceException: Object reference not set to an instance of an object.
		"""
			);

	[Test]
	public void ProductWithEmptyBody_Loaded_Outcome() =>
		Load("products:\n  apm: {}\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: Product 'apm' has invalid or missing versioning 'apm' while 'public-reference' is enabled.
		"""
			);

	[Test]
	public void ProductWithBlankDisplay_Loaded_Outcome() =>
		Load("products:\n  apm:\n    display: '  '\n    versioning: stack\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <ProductsConfiguration>:
			  Products:
			    apm <Product>:
			      Id: "apm"
			      DisplayName: "  "
			      VersioningSystem <VersioningSystem>:
			        Id: Stack
			        Base: 9.0.0
			        Current: 9.5.6
			        IsVersionless: False
			      Repository: "apm"
			      DraArtifact: null
			      Features <ProductFeatures>:
			        PublicReference: True
			        ReleaseNotes: OnRelease
			        ParticipatesInReleaseNotes: True
			"""
			);

	[Test]
	public void ProductsAsList_Loaded_Outcome() =>
		Load("products:\n  - apm\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/products.yml: (Lin: 1, Col: 2, Chr: 12) - (Lin: 1, Col: 2, Chr: 12): Expected a StartMapping token but found 'StartSequence'.
				"""
			);

	[Test]
	public void DuplicateProductId_Loaded_Outcome() =>
		Load("products:\n  apm:\n    display: A\n    versioning: stack\n  apm:\n    display: B\n    versioning: stack\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/products.yml: (Lin: 7, Col: 0, Chr: 98) - (Lin: 7, Col: 0, Chr: 98): Duplicate mapping key 'apm'.
				"""
			);

	[Test]
	public void ProductIdOnlyDiffersByCase_Loaded_Outcome() =>
		Load("products:\n  apm:\n    display: A\n    versioning: stack\n  APM:\n    display: B\n    versioning: stack\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    APM <Product>:
				      Id: "APM"
				      DisplayName: "B"
				      VersioningSystem <VersioningSystem>:
				        Id: Stack
				        Base: 9.0.0
				        Current: 9.5.6
				        IsVersionless: False
				      Repository: "APM"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: OnRelease
				        ParticipatesInReleaseNotes: True
				    apm <Product>:
				      Id: "apm"
				      DisplayName: "A"
				      VersioningSystem <VersioningSystem>:
				        Id: Stack
				        Base: 9.0.0
				        Current: 9.5.6
				        IsVersionless: False
				      Repository: "apm"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: OnRelease
				        ParticipatesInReleaseNotes: True
				"""
			);

	[Test]
	public void ProductIdWithoutVersioningSystemOfThatName_Loaded_Outcome() =>
		Load("products:\n  not-a-system:\n    display: X\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			InvalidOperationException: Product 'not-a-system' has invalid or missing versioning 'not-a-system' while 'public-reference' is enabled.
			"""
			);

	[Test]
	public void UnknownVersioningWithPublicReferenceOff_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: nope\n    features:\n      public-reference: false\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: None
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "x"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: False
				        ReleaseNotes: OnRelease
				        ParticipatesInReleaseNotes: True
				"""
			);

	[Test]
	public void VersioningNamedInDifferentCase_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: STACK\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <ProductsConfiguration>:
			  Products:
			    x <Product>:
			      Id: "x"
			      DisplayName: "X"
			      VersioningSystem <VersioningSystem>:
			        Id: Stack
			        Base: 9.0.0
			        Current: 9.5.6
			        IsVersionless: False
			      Repository: "x"
			      DraArtifact: null
			      Features <ProductFeatures>:
			        PublicReference: True
			        ReleaseNotes: OnRelease
			        ParticipatesInReleaseNotes: True
			"""
			);

	[Test]
	public void RepositoryAndDraArtifact_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    repository: some-repo\n    dra_artifact: some-artifact\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: All
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "some-repo"
				      DraArtifact: "some-artifact"
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: OnRelease
				        ParticipatesInReleaseNotes: True
				"""
			);

	[Test]
	public void DraArtifactSpelledWithHyphen_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    dra-artifact: some-artifact\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/products.yml: (Lin: 4, Col: 4, Chr: 54) - (Lin: 4, Col: 18, Chr: 68): (5,5) Unknown key 'dra-artifact' in $.products.x. Did you mean 'dra_artifact'?
				"""
			);

	[Test]
	public void EmptyFeaturesMap_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features: {}\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <ProductsConfiguration>:
			  Products:
			    x <Product>:
			      Id: "x"
			      DisplayName: "X"
			      VersioningSystem <VersioningSystem>:
			        Id: All
			        Base: 99999.0.0
			        Current: 99999.0.0
			        IsVersionless: True
			      Repository: "x"
			      DraArtifact: null
			      Features <ProductFeatures>:
			        PublicReference: True
			        ReleaseNotes: OnRelease
			        ParticipatesInReleaseNotes: True
			"""
			);

	[Test]
	public void UnknownFeatureKey_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      public-ref: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			InvalidOperationException: Product 'x' has unknown feature key(s): public-ref. Known features: public-reference, release-notes.
			"""
			);

	[Test]
	public void FeaturesAsList_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features: [public-reference]\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/products.yml: (Lin: 4, Col: 14, Chr: 64) - (Lin: 4, Col: 14, Chr: 64): Expected a StartMapping token but found 'StartSequence'.
				"""
			);

	[Test]
	public void PublicReferenceBlank_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      public-reference: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				InvalidOperationException: Product 'x' has an empty 'public-reference' value. Allowed values: true, false.
				"""
			);

	[Test]
	public void PublicReferenceNotABoolean_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      public-reference: yes\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				InvalidOperationException: Product 'x' has invalid 'public-reference' value 'yes'. Allowed values: true, false.
				"""
			);

	[Test]
	public void PublicReferenceUppercaseBoolean_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      public-reference: FALSE\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: All
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "x"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: False
				        ReleaseNotes: OnRelease
				        ParticipatesInReleaseNotes: True
				"""
			);

	[Test]
	public void ReleaseNotesBlank_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      release-notes: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				InvalidOperationException: Product 'x' has an empty 'release-notes' value. Allowed values: true, false, dra, on-release.
				"""
			);

	[Test]
	public void ReleaseNotesPrestageAlias_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      release-notes: prestage\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: All
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "x"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: DailyReleasableArtifacts
				        ParticipatesInReleaseNotes: True
				"""
			);

	[Test]
	public void ReleaseNotesUppercaseDra_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      release-notes: DRA\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: All
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "x"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: DailyReleasableArtifacts
				        ParticipatesInReleaseNotes: True
				"""
			);

	[Test]
	public void ReleaseNotesFalse_Loaded_Outcome() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      release-notes: false\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: All
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "x"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: None
				        ParticipatesInReleaseNotes: False
				"""
			);

	[Test]
	public void ReleaseNotesUnknownValue_Loaded_TreatedAsOnRelease() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      release-notes: banana\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: All
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "x"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: OnRelease
				        ParticipatesInReleaseNotes: True
				"""
			);

	[Test]
	public void FeaturesWithOnlyReleaseNotes_Loaded_PublicReferenceStaysOn() =>
		Load("products:\n  x:\n    display: X\n    versioning: all\n    features:\n      release-notes: dra\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <ProductsConfiguration>:
				  Products:
				    x <Product>:
				      Id: "x"
				      DisplayName: "X"
				      VersioningSystem <VersioningSystem>:
				        Id: All
				        Base: 99999.0.0
				        Current: 99999.0.0
				        IsVersionless: True
				      Repository: "x"
				      DraArtifact: null
				      Features <ProductFeatures>:
				        PublicReference: True
				        ReleaseNotes: DailyReleasableArtifacts
				        ParticipatesInReleaseNotes: True
				"""
			);
}
