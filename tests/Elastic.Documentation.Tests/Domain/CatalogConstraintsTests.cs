// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Documentation.Versions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The rules of the catalog records. Each guards a catalog built by hand: the loaders fail on the same conditions before they ever
/// run a rule, so what a file reports today does not change.
/// </summary>
public class CatalogConstraintsTests
{
	private static readonly VersioningSystem Stack = new()
	{
		Id = VersioningSystemId.Stack,
		Base = new SemVersion(8, 0, 0),
		Current = new SemVersion(9, 0, 0)
	};

	private static ProductCatalog Products(params Product[] products) => new() { Products = products.ToFrozenDictionary(p => p.Id) };

	private static Product Widgets(VersioningSystem? versioningSystem, bool publicReference = true) =>
		new()
		{
			Id = "widgets",
			DisplayName = "Widgets",
			VersioningSystem = versioningSystem,
			Features = new ProductFeatures { PublicReference = publicReference, ReleaseNotes = ReleaseNotesPath.OnRelease }
		};

	private static RelatedLearningCatalog Links(string title, string url) =>
		new()
		{
			Links = new Dictionary<string, RelatedLearningLink>
			{
				["demo"] = new() { Id = "demo", Title = title, Url = url }
			}.ToFrozenDictionary()
		};

	[Test]
	public void ProductCatalog_PublicReferenceProductWithVersioning_HasNoViolation() =>
		ProductCatalog.Constraints.Report(Products(Widgets(Stack))).Violations.Should().BeEmpty();

	[Test]
	public void ProductCatalog_PublicReferenceProductWithoutVersioning_IsAnError() =>
		ProductCatalog
			.Constraints
			.Report(Products(Widgets(versioningSystem: null)))
			.ToDiagnostics("products.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error products.yml Product 'widgets' has invalid or missing versioning while 'public-reference' is enabled."
			);

	[Test]
	public void ProductCatalog_PublicReferenceProductWithNoneVersioning_IsAnError() =>
		ProductCatalog.Constraints.Report(Products(Widgets(VersioningSystem.None))).Violations.Should().ContainSingle();

	/// <summary>
	/// A public-reference product that names a <c>none</c> system defined in versions.yml resolves to it, and the parser accepts that. Only the
	/// fallback the parser gives a product that is not public-reference (the <c>VersioningSystem.None</c> sentinel) is "no versioning".
	/// </summary>
	[Test]
	public void ProductCatalog_PublicReferenceProductWithADefinedNoneSystem_HasNoViolation()
	{
		var defined = new VersioningSystem
		{
			Id = VersioningSystemId.None,
			Base = new SemVersion(1, 0, 0),
			Current = new SemVersion(2, 0, 0)
		};

		ProductCatalog.Constraints.Report(Products(Widgets(defined))).Violations.Should().BeEmpty();
	}

	[Test]
	public void ProductCatalog_Projections_FollowTheComparerOfTheProducts()
	{
		var catalog = new ProductCatalog
		{
			Products = new Dictionary<string, Product> { ["widgets"] = Widgets(Stack) }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)
		};

		catalog.Products.ContainsKey("WIDGETS").Should().BeTrue();
		catalog.PublicReferenceProducts.ContainsKey("WIDGETS").Should().BeTrue();
		catalog.GetDisplayName("WIDGETS").Should().Be("Widgets");
		catalog.GetDisplayName("WIDGETS".AsSpan()).Should().Be("Widgets");
		catalog.TryGetProductName("WIDGETS", out _).Should().BeTrue();
	}

	[Test]
	public void ProductCatalog_ProductThatIsNotPublicReference_MayHaveNoVersioning() =>
		ProductCatalog.Constraints.Report(Products(Widgets(VersioningSystem.None, publicReference: false))).Violations.Should().BeEmpty();

	[Test]
	public void RelatedLearning_CompleteLink_HasNoViolation() =>
		RelatedLearningCatalog.Constraints.Report(Links("Demo", "https://www.elastic.co/training/demo")).Violations.Should().BeEmpty();

	[Test]
	[Arguments("", "https://www.elastic.co/training/demo", "link-has-title")]
	[Arguments("Demo", "", "link-has-url")]
	[Arguments("Demo", "/training/demo", "link-url-absolute-http")]
	[Arguments("Demo", "ftp://www.elastic.co/training/demo", "link-url-absolute-http")]
	public void RelatedLearning_IncompleteLink_ReportsTheRuleThatIsBroken(string title, string url, string ruleId) =>
		RelatedLearningCatalog
			.Constraints
			.Report(Links(title, url))
			.ToDiagnostics("related-learning.yml")
			.Select(d => d.RuleId)
			.Should()
			.Equal(ruleId);

	[Test]
	public void RelatedLearning_MessageNamesTheLinkAndTheUrl() =>
		RelatedLearningCatalog
			.Constraints
			.Report(Links("Demo", "/training/demo"))
			.ToDiagnostics("related-learning.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error related-learning.yml related-learning.yml link 'demo' has invalid url '/training/demo'. Every url must be an absolute http or https URL."
			);

	[Test]
	public void ProductInVersioningSystemCatalog_UndefinedVersioningSystem_IsAnError()
	{
		var versions = new VersioningSystemCatalog { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() };

		ProductCatalogInVersioningSystemCatalog
			.Constraints
			.Report(new ProductCatalogInVersioningSystemCatalog(Products(Widgets(Stack)), versions))
			.ToDiagnostics("products.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error products.yml Product 'widgets' uses versioning system 'stack', which is not defined in versions.yml."
			);
	}

	[Test]
	public void ProductInVersioningSystemCatalog_DefinedVersioningSystem_HasNoViolation()
	{
		var versions = new VersioningSystemCatalog
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem> { [Stack.Id] = Stack }
		};

		ProductCatalogInVersioningSystemCatalog
			.Constraints
			.Report(new ProductCatalogInVersioningSystemCatalog(Products(Widgets(Stack)), versions))
			.Violations
			.Should()
			.BeEmpty();
	}

	[Test]
	public void LegacyUrlMappingInProductCatalog_UndefinedProduct_IsAnError()
	{
		var legacy = new LegacyUrlMappingCatalog
		{
			Mappings = [new LegacyUrlMapping { BaseUrl = "en/widgets/", Product = Widgets(Stack), LegacyVersions = ["1.0"] }]
		};

		LegacyUrlMappingCatalogInProductCatalog
			.Constraints
			.Report(new LegacyUrlMappingCatalogInProductCatalog(legacy, Products()))
			.ToDiagnostics("legacy-url-mappings.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error legacy-url-mappings.yml Legacy url mapping 'en/widgets/' refers to product 'widgets', which is not defined in products.yml."
			);
	}

	/// <summary>
	/// A versioning system whose base is after its current version is read without a finding today. Reporting it is a behaviour change,
	/// deferred with the rest of the catalog's warnings.
	/// </summary>
	[Test]
	public void VersioningSystemCatalog_BaseAfterCurrent_IsNotReportedToday()
	{
		var backwards = Stack with { Base = new SemVersion(9, 0, 0), Current = new SemVersion(8, 0, 0) };
		var catalog = new VersioningSystemCatalog
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem> { [backwards.Id] = backwards }
		};

		VersioningSystemCatalog.Constraints.Report(catalog).Violations.Should().BeEmpty();
	}

	[Test]
	public void ProductCatalog_EqualContent_IsEqual_WhateverWasComputed()
	{
		var left = Products(Widgets(Stack));
		var right = Products(Widgets(Stack));

		_ = left.PublicReferenceProducts;
		_ = left.ProductDisplayNames;

		left.Should().Be(right);
		left.GetHashCode().Should().Be(right.GetHashCode());
	}

	[Test]
	public void ProductCatalog_Projections_FollowTheProducts()
	{
		var catalog = Products(
			Widgets(Stack),
			Widgets(Stack) with { Id = "hidden", DisplayName = "Hidden", Features = new ProductFeatures { PublicReference = false } }
		);

		catalog.PublicReferenceProducts.Keys.Should().Equal("widgets");
		catalog
			.ProductDisplayNames
			.Should()
			.BeEquivalentTo(new Dictionary<string, string> { ["widgets"] = "Widgets", ["hidden"] = "Hidden" });
	}

	[Test]
	public void ProductCatalog_CopiedWithOtherProducts_RecomputesItsProjections()
	{
		var catalog = Products(Widgets(Stack));
		_ = catalog.ProductDisplayNames;

		var copy = catalog with { Products = Products(Widgets(Stack) with { DisplayName = "Gadgets" }).Products };

		copy.GetDisplayName("widgets").Should().Be("Gadgets");
	}

	[Test]
	[Arguments("cloud_hosted", "cloud-hosted")]
	[Arguments("  apm  ", "apm")]
	[Arguments("", "")]
	[Arguments(null, "")]
	public void ProductNames_Normalize_TrimsAndUsesHyphens(string? written, string expected) =>
		ProductNames.Normalize(written).Should().Be(expected);

	[Test]
	[Arguments("cloud_hosted", "CLOUD-HOSTED", true)]
	[Arguments("apm", "apm-agent", false)]
	public void ProductNames_AreSame_ComparesNormalizedIdsIgnoringCase(string left, string right, bool same) =>
		ProductNames.AreSame(left, right).Should().Be(same);
}
