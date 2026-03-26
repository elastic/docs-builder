// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Changelog;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleRulesExtensionsTests
{
	[Fact]
	public void DetermineFilterMode_NoProductListsOrBlocker_IsNoFiltering()
	{
		var rules = new BundleRules();
		rules.DetermineFilterMode().Should().Be(BundleFilterMode.NoFiltering);
	}

	[Fact]
	public void DetermineFilterMode_IncludeProducts_IsGlobalContent()
	{
		var rules = new BundleRules { IncludeProducts = ["kibana"] };
		rules.DetermineFilterMode().Should().Be(BundleFilterMode.GlobalContent);
	}

	[Fact]
	public void DetermineFilterMode_ExcludeProducts_IsGlobalContent()
	{
		var rules = new BundleRules { ExcludeProducts = ["kibana"] };
		rules.DetermineFilterMode().Should().Be(BundleFilterMode.GlobalContent);
	}

	[Fact]
	public void DetermineFilterMode_NonEmptyByProduct_IsPerProductContext()
	{
		var rules = new BundleRules
		{
			IncludeProducts = ["elasticsearch"],
			ByProduct = new Dictionary<string, BundlePerProductRule>(StringComparer.OrdinalIgnoreCase)
			{
				["kibana"] = new BundlePerProductRule()
			}
		};
		rules.DetermineFilterMode().Should().Be(BundleFilterMode.PerProductContext);
	}
}
