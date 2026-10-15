// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Changelog.Tests.Changelogs;

public class ProductResolutionTests
{
	private static readonly Dictionary<string, IReadOnlyList<string>> KibanaImplies = new(StringComparer.OrdinalIgnoreCase)
	{
		["observability"] = ["kibana", "cloud-serverless"],
		["security"] = ["kibana", "cloud-serverless"]
	};

	private static ChangelogConfiguration Config(
		Dictionary<string, IReadOnlyList<string>>? implies = null,
		IReadOnlyList<DefaultProduct>? defaults = null
	) =>
		ChangelogConfiguration.Default with
		{
			LabelToProducts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Team:obs-ux-logs"] = "observability",
				["Team:SecuritySolution"] = "security",
				["serverless"] = "cloud-serverless 2026-10 ga"
			},
			ProductsConfiguration = new ProductsConfig { Implies = implies, Default = defaults }
		};

	[Test]
	public void FromLabels_AddsTheImpliedProductsAfterTheLabelProducts()
	{
		var products = ProductResolution.FromLabels(["Team:obs-ux-logs", ">feature"], Config(KibanaImplies));

		products.Select(p => p.Product).Should().Equal("observability", "kibana", "cloud-serverless");
		products.Should().OnlyContain(p => p.Target == null && p.Lifecycle == null);
	}

	[Test]
	public void FromLabels_DoesNotAddAProductThatALabelAlreadyMapsTo()
	{
		var products = ProductResolution.FromLabels(["Team:obs-ux-logs", "serverless"], Config(KibanaImplies));

		products.Select(p => p.Product).Should().Equal("observability", "cloud-serverless", "kibana");
		// The label's spec wins over the implied product ID.
		products.Single(p => p.Product == "cloud-serverless").Lifecycle.Should().Be("ga");
	}

	[Test]
	public void FromLabels_ListsAnImpliedProductOnceAcrossLabels()
	{
		var products = ProductResolution.FromLabels(["Team:obs-ux-logs", "Team:SecuritySolution"], Config(KibanaImplies));

		products.Select(p => p.Product).Should().Equal("observability", "security", "kibana", "cloud-serverless");
	}

	[Test]
	public void FromLabels_AppliesImpliesOneLevelOnly()
	{
		var implies = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["observability"] = ["kibana"],
			["kibana"] = ["cloud-serverless"]
		};

		var products = ProductResolution.FromLabels(["Team:obs-ux-logs"], Config(implies));

		products.Select(p => p.Product).Should().Equal("observability", "kibana");
	}

	[Test]
	public void FromLabels_WithoutImplies_IsTheLabelMapping() =>
		ProductResolution.FromLabels(["Team:obs-ux-logs"], Config()).Select(p => p.Product).Should().Equal("observability");

	[Test]
	public void FromLabels_ReturnsEmptyWhenNoLabelMapsToAProduct()
	{
		ProductResolution.FromLabels([">feature"], Config(KibanaImplies)).Should().BeEmpty();
		ProductResolution.FromLabels([], Config(KibanaImplies)).Should().BeEmpty();
		ProductResolution.FromLabels(["Team:obs-ux-logs"], ChangelogConfiguration.Default).Should().BeEmpty();
	}

	[Test]
	public void Defaults_CarryTheConfiguredLifecycle()
	{
		var config = Config(defaults: [
			new DefaultProduct { Product = "kibana" },
			new DefaultProduct { Product = "cloud-serverless", Lifecycle = "beta" }
		]);

		ProductResolution.Defaults(config).Select(p => $"{p.Product} {p.Lifecycle}").Should().Equal("kibana ga", "cloud-serverless beta");
		ProductResolution.Defaults(Config()).Should().BeEmpty();
	}
}
