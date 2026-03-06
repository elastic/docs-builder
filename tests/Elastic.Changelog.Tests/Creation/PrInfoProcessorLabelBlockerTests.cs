// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Creation;

public class PrInfoProcessorLabelBlockerTests
{
	[Fact]
	public void AreAllProductsBlocked_NullRules_ReturnsFalse() =>
		PrInfoProcessor.AreAllProductsBlocked(["some-label"], null).Should().BeFalse();

	[Fact]
	public void AreAllProductsBlocked_NoLabelsConfigured_ReturnsFalse()
	{
		var rules = new CreateRules { Labels = null, Mode = FieldMode.Exclude };

		PrInfoProcessor.AreAllProductsBlocked(["some-label"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_GlobalExclude_MatchingLabel_ReturnsTrue()
	{
		var rules = new CreateRules
		{
			Labels = ["changelog:skip"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any
		};

		PrInfoProcessor.AreAllProductsBlocked(["changelog:skip", "type:feature"], rules).Should().BeTrue();
	}

	[Fact]
	public void AreAllProductsBlocked_GlobalExclude_NoMatchingLabel_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = ["changelog:skip"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any
		};

		PrInfoProcessor.AreAllProductsBlocked(["type:feature"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_GlobalInclude_NoneMatch_ReturnsTrue()
	{
		var rules = new CreateRules
		{
			Labels = ["changelog:include"],
			Mode = FieldMode.Include,
			Match = MatchMode.Any
		};

		PrInfoProcessor.AreAllProductsBlocked(["type:feature"], rules).Should().BeTrue();
	}

	[Fact]
	public void AreAllProductsBlocked_GlobalInclude_HasMatch_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = ["changelog:include"],
			Mode = FieldMode.Include,
			Match = MatchMode.Any
		};

		PrInfoProcessor.AreAllProductsBlocked(["changelog:include", "type:feature"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_ProductOverride_OneNotBlocked_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = ["changelog:skip"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new()
				{
					Labels = ["changelog:skip"],
					Mode = FieldMode.Exclude,
					Match = MatchMode.Any
				},
				["kibana"] = new()
				{
					Labels = ["kibana:skip"],
					Mode = FieldMode.Exclude,
					Match = MatchMode.Any
				}
			}
		};

		// changelog:skip blocks elasticsearch but not kibana (kibana needs "kibana:skip")
		PrInfoProcessor.AreAllProductsBlocked(["changelog:skip", "type:feature"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_ProductOverride_AllBlocked_ReturnsTrue()
	{
		var rules = new CreateRules
		{
			Labels = ["changelog:skip"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new()
				{
					Labels = ["changelog:skip"],
					Mode = FieldMode.Exclude,
					Match = MatchMode.Any
				},
				["kibana"] = new()
				{
					Labels = ["changelog:skip"],
					Mode = FieldMode.Exclude,
					Match = MatchMode.Any
				}
			}
		};

		PrInfoProcessor.AreAllProductsBlocked(["changelog:skip", "type:feature"], rules).Should().BeTrue();
	}

	[Fact]
	public void AreAllProductsBlocked_ExcludeMatchAll_OnlyBlocksWhenAllLabelsMatch()
	{
		var rules = new CreateRules
		{
			Labels = ["skip-a", "skip-b"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.All
		};

		// Only one of the two exclude labels present — MatchMode.All means ALL pr labels must be in the exclude list
		PrInfoProcessor.AreAllProductsBlocked(["skip-a", "other"], rules).Should().BeFalse();
		PrInfoProcessor.AreAllProductsBlocked(["skip-a", "skip-b"], rules).Should().BeTrue();
	}

	[Fact]
	public void IsBlockedByRules_EmptyLabels_ReturnsFalse()
	{
		var rules = new CreateRules { Labels = [], Mode = FieldMode.Exclude };

		PrInfoProcessor.IsBlockedByRules(["some-label"], rules).Should().BeFalse();
	}
}
