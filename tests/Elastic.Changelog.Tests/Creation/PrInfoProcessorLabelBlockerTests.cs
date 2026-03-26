// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;

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
	public void AreAllProductsBlocked_ProductOverrideBlocks_GlobalDoesNot_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = [">non-issue"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new()
				{
					Labels = [">test"],
					Mode = FieldMode.Exclude,
					Match = MatchMode.Any
				}
			}
		};

		// >test blocks elasticsearch's override, but global rule (>non-issue) doesn't match
		// Non-overridden products (e.g. kibana) use global rules and are NOT blocked
		PrInfoProcessor.AreAllProductsBlocked([">test"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_GlobalBlocks_ProductOverrideDoesNot_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = [">non-issue"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new()
				{
					Labels = [">test"],
					Mode = FieldMode.Exclude,
					Match = MatchMode.Any
				}
			}
		};

		// >non-issue blocks global (non-overridden products), but elasticsearch's override (>test) doesn't match
		PrInfoProcessor.AreAllProductsBlocked([">non-issue"], rules).Should().BeFalse();
	}

	// --- Tests derived from the documented YAML example in the Rules reference ---
	// rules:
	//   create:
	//     exclude: ">non-issue"
	//     products:
	//       'elasticsearch, kibana':
	//         exclude: ">test"
	//       cloud-serverless:
	//         exclude: ">non-issue, ILM"

	private static CreateRules DocumentedExampleRules() => new()
	{
		Labels = [">non-issue"],
		Mode = FieldMode.Exclude,
		Match = MatchMode.Any,
		ByProduct = new Dictionary<string, CreateRules>
		{
			["elasticsearch"] = new() { Labels = [">test"], Mode = FieldMode.Exclude, Match = MatchMode.Any },
			["kibana"] = new() { Labels = [">test"], Mode = FieldMode.Exclude, Match = MatchMode.Any },
			["cloud-serverless"] = new() { Labels = [">non-issue", "ILM"], Mode = FieldMode.Exclude, Match = MatchMode.Any }
		}
	};

	[Fact]
	public void DocExample_NonIssueOnly_NotAllBlocked()
	{
		// >non-issue: global blocks, cloud-serverless blocks, but ES/kibana need >test
		PrInfoProcessor.AreAllProductsBlocked([">non-issue"], DocumentedExampleRules()).Should().BeFalse();
	}

	[Fact]
	public void DocExample_TestOnly_NotAllBlocked()
	{
		// >test: ES/kibana block, but global (>non-issue) doesn't match
		PrInfoProcessor.AreAllProductsBlocked([">test"], DocumentedExampleRules()).Should().BeFalse();
	}

	[Fact]
	public void DocExample_NonIssueAndTest_AllBlocked()
	{
		// >non-issue + >test: global blocks, ES/kibana block (>test), cloud-serverless blocks (>non-issue)
		PrInfoProcessor.AreAllProductsBlocked([">non-issue", ">test"], DocumentedExampleRules()).Should().BeTrue();
	}

	[Fact]
	public void DocExample_IlmOnly_NotAllBlocked()
	{
		// ILM: only cloud-serverless blocks, global doesn't match, ES/kibana don't match
		PrInfoProcessor.AreAllProductsBlocked(["ILM"], DocumentedExampleRules()).Should().BeFalse();
	}

	[Fact]
	public void DocExample_UnrelatedLabel_NotAllBlocked()
	{
		// No configured label matches anything
		PrInfoProcessor.AreAllProductsBlocked(["type:feature"], DocumentedExampleRules()).Should().BeFalse();
	}

	// --- Include mode: "all PRs are not notable unless a specific label is present" ---

	[Fact]
	public void AreAllProductsBlocked_IncludeWithProductOverride_AllBlocked()
	{
		var rules = new CreateRules
		{
			Labels = ["@Public"],
			Mode = FieldMode.Include,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = ["@Release"], Mode = FieldMode.Include, Match = MatchMode.Any }
			}
		};

		// Neither @Public nor @Release present → both global and ES override block
		PrInfoProcessor.AreAllProductsBlocked(["type:feature"], rules).Should().BeTrue();
	}

	[Fact]
	public void AreAllProductsBlocked_IncludeWithProductOverride_GlobalSatisfied_NotAllBlocked()
	{
		var rules = new CreateRules
		{
			Labels = ["@Public"],
			Mode = FieldMode.Include,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = ["@Release"], Mode = FieldMode.Include, Match = MatchMode.Any }
			}
		};

		// @Public satisfies global (non-overridden products pass), but ES needs @Release
		PrInfoProcessor.AreAllProductsBlocked(["@Public"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_IncludeWithProductOverride_OverrideSatisfied_NotAllBlocked()
	{
		var rules = new CreateRules
		{
			Labels = ["@Public"],
			Mode = FieldMode.Include,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = ["@Release"], Mode = FieldMode.Include, Match = MatchMode.Any }
			}
		};

		// @Release satisfies ES override, but global needs @Public
		PrInfoProcessor.AreAllProductsBlocked(["@Release"], rules).Should().BeFalse();
	}

	// --- Include mode with match: all ---

	[Fact]
	public void AreAllProductsBlocked_IncludeMatchAll_PartialMatch_ReturnsTrue()
	{
		var rules = new CreateRules
		{
			Labels = ["required-a", "required-b"],
			Mode = FieldMode.Include,
			Match = MatchMode.All
		};

		// Include + all: blocked if NOT ALL PR labels are in the include list
		// PR has "required-a" and "other" — "other" is not in the include list → blocked
		PrInfoProcessor.AreAllProductsBlocked(["required-a", "other"], rules).Should().BeTrue();
	}

	[Fact]
	public void AreAllProductsBlocked_IncludeMatchAll_AllMatch_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = ["required-a", "required-b"],
			Mode = FieldMode.Include,
			Match = MatchMode.All
		};

		// All PR labels are in the include list → not blocked
		PrInfoProcessor.AreAllProductsBlocked(["required-a", "required-b"], rules).Should().BeFalse();
	}

	// --- Mixed modes across products ---

	[Fact]
	public void AreAllProductsBlocked_MixedModesAcrossProducts()
	{
		var rules = new CreateRules
		{
			Labels = [">non-issue"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any,
			ByProduct = new Dictionary<string, CreateRules>
			{
				// ES uses include mode (opposite of global)
				["elasticsearch"] = new() { Labels = ["@Release"], Mode = FieldMode.Include, Match = MatchMode.Any }
			}
		};

		// >non-issue: global blocks, ES include-mode needs @Release (absent) → ES blocked too
		PrInfoProcessor.AreAllProductsBlocked([">non-issue"], rules).Should().BeTrue();

		// @Release: global (>non-issue) doesn't match → not all blocked
		PrInfoProcessor.AreAllProductsBlocked(["@Release"], rules).Should().BeFalse();
	}

	// --- Case insensitivity ---

	[Fact]
	public void AreAllProductsBlocked_CaseInsensitiveMatching()
	{
		var rules = new CreateRules
		{
			Labels = ["Changelog:Skip"],
			Mode = FieldMode.Exclude,
			Match = MatchMode.Any
		};

		PrInfoProcessor.AreAllProductsBlocked(["changelog:skip"], rules).Should().BeTrue();
		PrInfoProcessor.AreAllProductsBlocked(["CHANGELOG:SKIP"], rules).Should().BeTrue();
	}

	// --- ByProduct-only configs (no global labels) ---

	[Fact]
	public void AreAllProductsBlocked_ByProductOnly_AllBlocked_ReturnsTrue()
	{
		var rules = new CreateRules
		{
			Labels = null,
			Mode = FieldMode.Exclude,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = [">test"], Mode = FieldMode.Exclude, Match = MatchMode.Any },
				["kibana"] = new() { Labels = [">test"], Mode = FieldMode.Exclude, Match = MatchMode.Any }
			}
		};

		PrInfoProcessor.AreAllProductsBlocked([">test"], rules).Should().BeTrue();
	}

	[Fact]
	public void AreAllProductsBlocked_ByProductOnly_OneNotBlocked_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = null,
			Mode = FieldMode.Exclude,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = [">test"], Mode = FieldMode.Exclude, Match = MatchMode.Any },
				["kibana"] = new() { Labels = ["kibana:skip"], Mode = FieldMode.Exclude, Match = MatchMode.Any }
			}
		};

		// >test blocks elasticsearch but not kibana
		PrInfoProcessor.AreAllProductsBlocked([">test"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_ByProductOnly_NoMatchingLabels_ReturnsFalse()
	{
		var rules = new CreateRules
		{
			Labels = null,
			Mode = FieldMode.Exclude,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = [">test"], Mode = FieldMode.Exclude, Match = MatchMode.Any }
			}
		};

		PrInfoProcessor.AreAllProductsBlocked(["type:feature"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_ByProductOnly_IncludeMode_AllBlocked()
	{
		var rules = new CreateRules
		{
			Labels = null,
			Mode = FieldMode.Exclude,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = ["@Public"], Mode = FieldMode.Include, Match = MatchMode.Any },
				["kibana"] = new() { Labels = ["@Public"], Mode = FieldMode.Include, Match = MatchMode.Any }
			}
		};

		// Neither product's include label is present → both blocked
		PrInfoProcessor.AreAllProductsBlocked(["type:feature"], rules).Should().BeTrue();
	}

	[Fact]
	public void AreAllProductsBlocked_ByProductOnly_IncludeMode_OneSatisfied()
	{
		var rules = new CreateRules
		{
			Labels = null,
			Mode = FieldMode.Exclude,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = ["@Public"], Mode = FieldMode.Include, Match = MatchMode.Any },
				["kibana"] = new() { Labels = ["@Release"], Mode = FieldMode.Include, Match = MatchMode.Any }
			}
		};

		// @Public satisfies elasticsearch but kibana needs @Release
		PrInfoProcessor.AreAllProductsBlocked(["@Public"], rules).Should().BeFalse();
	}

	[Fact]
	public void AreAllProductsBlocked_ByProductOnly_MixedModes_AllBlocked()
	{
		var rules = new CreateRules
		{
			Labels = null,
			Mode = FieldMode.Exclude,
			ByProduct = new Dictionary<string, CreateRules>
			{
				["elasticsearch"] = new() { Labels = [">test"], Mode = FieldMode.Exclude, Match = MatchMode.Any },
				["kibana"] = new() { Labels = ["@Release"], Mode = FieldMode.Include, Match = MatchMode.Any }
			}
		};

		// >test blocks elasticsearch (exclude), @Release not present blocks kibana (include)
		PrInfoProcessor.AreAllProductsBlocked([">test"], rules).Should().BeTrue();
	}

	// --- IsBlockedByRules edge cases ---

	[Fact]
	public void IsBlockedByRules_EmptyLabels_ReturnsFalse()
	{
		var rules = new CreateRules { Labels = [], Mode = FieldMode.Exclude };

		PrInfoProcessor.IsBlockedByRules(["some-label"], rules).Should().BeFalse();
	}

	[Fact]
	public void IsBlockedByRules_EmptyPrLabels_ExcludeMode_ReturnsFalse()
	{
		var rules = new CreateRules { Labels = ["skip"], Mode = FieldMode.Exclude, Match = MatchMode.Any };

		PrInfoProcessor.IsBlockedByRules([], rules).Should().BeFalse();
	}

	[Fact]
	public void IsBlockedByRules_EmptyPrLabels_IncludeMode_ReturnsTrue()
	{
		var rules = new CreateRules { Labels = ["required"], Mode = FieldMode.Include, Match = MatchMode.Any };

		// No PR labels → none match the include list → blocked
		PrInfoProcessor.IsBlockedByRules([], rules).Should().BeTrue();
	}
}
