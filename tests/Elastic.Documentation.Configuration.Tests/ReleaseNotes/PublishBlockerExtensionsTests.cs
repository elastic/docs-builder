// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for PublishBlockerExtensions.ShouldBlock.
/// These tests verify the blocking logic for changelog entries across all mode/match combinations.
/// </summary>
public class PublishBlockerExtensionsTests
{
	// --- No rules ---

	[Fact]
	public void ShouldBlock_ReturnsFalse_WhenNoBlockingRules()
	{
		var blocker = new PublishBlocker();
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	// --- Exclude types (default mode) ---

	[Fact]
	public void ShouldBlock_ExcludeType_Blocks_WhenTypeMatches()
	{
		var blocker = new PublishBlocker { Types = ["regression", "known-issue"], TypesMode = FieldMode.Exclude };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Regression };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_ExcludeType_Allows_WhenTypeDoesNotMatch()
	{
		var blocker = new PublishBlocker { Types = ["regression", "known-issue"], TypesMode = FieldMode.Exclude };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_ExcludeType_IsCaseInsensitive()
	{
		var blocker = new PublishBlocker { Types = ["REGRESSION"], TypesMode = FieldMode.Exclude };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Regression };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	// --- Include types ---

	[Fact]
	public void ShouldBlock_IncludeType_Allows_WhenTypeMatches()
	{
		var blocker = new PublishBlocker { Types = ["feature", "bug-fix"], TypesMode = FieldMode.Include };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_IncludeType_Blocks_WhenTypeDoesNotMatch()
	{
		var blocker = new PublishBlocker { Types = ["feature", "bug-fix"], TypesMode = FieldMode.Include };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Regression };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_IncludeType_IsCaseInsensitive()
	{
		var blocker = new PublishBlocker { Types = ["FEATURE"], TypesMode = FieldMode.Include };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	// --- Exclude areas + match any (default) ---

	[Fact]
	public void ShouldBlock_ExcludeArea_MatchAny_Blocks_WhenAnyAreaMatches()
	{
		var blocker = new PublishBlocker { Areas = ["Internal"], AreasMode = FieldMode.Exclude, MatchAreas = MatchMode.Any };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_ExcludeArea_MatchAny_Allows_WhenNoAreaMatches()
	{
		var blocker = new PublishBlocker { Areas = ["Internal"], AreasMode = FieldMode.Exclude, MatchAreas = MatchMode.Any };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Monitoring"] };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_ExcludeArea_IsCaseInsensitive()
	{
		var blocker = new PublishBlocker { Areas = ["INTERNAL"], AreasMode = FieldMode.Exclude };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["internal"] };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	// --- Exclude areas + match all ---

	[Fact]
	public void ShouldBlock_ExcludeArea_MatchAll_Blocks_WhenAllAreasMatch()
	{
		var blocker = new PublishBlocker
		{
			Areas = ["Internal", "Search"],
			AreasMode = FieldMode.Exclude,
			MatchAreas = MatchMode.All
		};
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Internal", "Search"] };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_ExcludeArea_MatchAll_Allows_WhenNotAllAreasMatch()
	{
		var blocker = new PublishBlocker
		{
			Areas = ["Internal"],
			AreasMode = FieldMode.Exclude,
			MatchAreas = MatchMode.All
		};
		// Entry has ["Search", "Internal"]. MatchAll means ALL entry areas must be in the exclude list.
		// "Search" is NOT in ["Internal"], so not all match → allowed.
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	// --- Include areas + match any ---

	[Fact]
	public void ShouldBlock_IncludeArea_MatchAny_Allows_WhenAnyAreaMatches()
	{
		var blocker = new PublishBlocker { Areas = ["Search", "Monitoring"], AreasMode = FieldMode.Include, MatchAreas = MatchMode.Any };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_IncludeArea_MatchAny_Blocks_WhenNoAreaMatches()
	{
		var blocker = new PublishBlocker { Areas = ["Search", "Monitoring"], AreasMode = FieldMode.Include, MatchAreas = MatchMode.Any };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Internal", "Experimental"] };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	// --- Include areas + match all ---

	[Fact]
	public void ShouldBlock_IncludeArea_MatchAll_Allows_WhenAllAreasInIncludeList()
	{
		var blocker = new PublishBlocker
		{
			Areas = ["Search", "Monitoring", "Internal"],
			AreasMode = FieldMode.Include,
			MatchAreas = MatchMode.All
		};
		// All entry areas ("Search", "Internal") are in the include list → matches → allowed
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_IncludeArea_MatchAll_Blocks_WhenNotAllAreasInIncludeList()
	{
		var blocker = new PublishBlocker
		{
			Areas = ["Search"],
			AreasMode = FieldMode.Include,
			MatchAreas = MatchMode.All
		};
		// Entry has ["Search", "Internal"]. "Internal" is NOT in include list → not all match → blocked
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	// --- Entry with no areas ---

	[Fact]
	public void ShouldBlock_ExcludeArea_ReturnsFalse_WhenEntryHasNoAreas()
	{
		var blocker = new PublishBlocker { Areas = ["Internal"], AreasMode = FieldMode.Exclude };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_IncludeArea_ReturnsTrue_WhenEntryHasNoAreas()
	{
		// Include mode with no entry areas → entry doesn't match the include list → blocked
		var blocker = new PublishBlocker { Areas = ["Search"], AreasMode = FieldMode.Include };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	// --- Mixed modes (exclude_types + include_areas) ---

	[Fact]
	public void ShouldBlock_MixedModes_BlockedByExcludeType()
	{
		var blocker = new PublishBlocker
		{
			Types = ["deprecation"],
			TypesMode = FieldMode.Exclude,
			Areas = ["Search"],
			AreasMode = FieldMode.Include
		};
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Deprecation, Areas = ["Search"] };

		// Type "deprecation" is excluded → blocked (regardless of areas)
		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_MixedModes_BlockedByIncludeArea()
	{
		var blocker = new PublishBlocker
		{
			Types = ["deprecation"],
			TypesMode = FieldMode.Exclude,
			Areas = ["Search"],
			AreasMode = FieldMode.Include
		};
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Internal"] };

		// Type "feature" is NOT excluded, but area "Internal" is NOT in include list → blocked
		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_MixedModes_Allowed_WhenTypeNotExcludedAndAreaIncluded()
	{
		var blocker = new PublishBlocker
		{
			Types = ["deprecation"],
			TypesMode = FieldMode.Exclude,
			Areas = ["Search"],
			AreasMode = FieldMode.Include
		};
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search"] };

		// Type "feature" is NOT excluded, area "Search" IS in include list → allowed
		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	// --- MatchesType ---

	[Fact]
	public void MatchesType_ReturnsTrue_WhenTypeInList()
	{
		var blocker = new PublishBlocker { Types = ["feature", "bug-fix"] };

		blocker.MatchesType("feature").Should().BeTrue();
	}

	[Fact]
	public void MatchesType_ReturnsFalse_WhenTypeNotInList()
	{
		var blocker = new PublishBlocker { Types = ["feature", "bug-fix"] };

		blocker.MatchesType("regression").Should().BeFalse();
	}

	[Fact]
	public void MatchesType_ReturnsFalse_WhenNoTypes()
	{
		var blocker = new PublishBlocker();

		blocker.MatchesType("feature").Should().BeFalse();
	}

	// --- MatchesArea ---

	[Fact]
	public void MatchesArea_MatchAny_ReturnsTrue_WhenAnyAreaInList()
	{
		var blocker = new PublishBlocker { Areas = ["Search", "Internal"], MatchAreas = MatchMode.Any };

		blocker.MatchesArea(["Search", "Other"]).Should().BeTrue();
	}

	[Fact]
	public void MatchesArea_MatchAny_ReturnsFalse_WhenNoAreaInList()
	{
		var blocker = new PublishBlocker { Areas = ["Search", "Internal"], MatchAreas = MatchMode.Any };

		blocker.MatchesArea(["Other", "External"]).Should().BeFalse();
	}

	[Fact]
	public void MatchesArea_MatchAll_ReturnsTrue_WhenAllEntryAreasInList()
	{
		var blocker = new PublishBlocker { Areas = ["Search", "Internal", "Monitoring"], MatchAreas = MatchMode.All };

		blocker.MatchesArea(["Search", "Internal"]).Should().BeTrue();
	}

	[Fact]
	public void MatchesArea_MatchAll_ReturnsFalse_WhenNotAllEntryAreasInList()
	{
		var blocker = new PublishBlocker { Areas = ["Search"], MatchAreas = MatchMode.All };

		blocker.MatchesArea(["Search", "Internal"]).Should().BeFalse();
	}

	[Fact]
	public void MatchesArea_ReturnsFalse_WhenNoAreas()
	{
		var blocker = new PublishBlocker();

		blocker.MatchesArea(["Search"]).Should().BeFalse();
	}

	[Fact]
	public void MatchesArea_ReturnsFalse_WhenEntryAreasNull()
	{
		var blocker = new PublishBlocker { Areas = ["Search"] };

		blocker.MatchesArea(null).Should().BeFalse();
	}

	[Fact]
	public void MatchesArea_ReturnsFalse_WhenEntryAreasEmpty()
	{
		var blocker = new PublishBlocker { Areas = ["Search"] };

		blocker.MatchesArea([]).Should().BeFalse();
	}

	// --- HasBlockingRules ---

	[Fact]
	public void HasBlockingRules_ReturnsFalse_WhenEmpty()
	{
		var blocker = new PublishBlocker();

		blocker.HasBlockingRules.Should().BeFalse();
	}

	[Fact]
	public void HasBlockingRules_ReturnsTrue_WhenTypesSet()
	{
		var blocker = new PublishBlocker { Types = ["feature"] };

		blocker.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void HasBlockingRules_ReturnsTrue_WhenAreasSet()
	{
		var blocker = new PublishBlocker { Areas = ["Search"] };

		blocker.HasBlockingRules.Should().BeTrue();
	}

	// --- Plan examples from area matching table ---

	[Fact]
	public void PlanExample_ExcludeAreas_MatchAny_EntryWithMatchingArea_Blocked()
	{
		// exclude_areas: [Internal], match: any, entry areas: ["Search", "Internal"] → Blocked
		var blocker = new PublishBlocker { Areas = ["Internal"], AreasMode = FieldMode.Exclude, MatchAreas = MatchMode.Any };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}

	[Fact]
	public void PlanExample_ExcludeAreas_MatchAll_NotAllMatch_Allowed()
	{
		// exclude_areas: [Internal], match: all, entry areas: ["Search", "Internal"] → Allowed
		var blocker = new PublishBlocker { Areas = ["Internal"], AreasMode = FieldMode.Exclude, MatchAreas = MatchMode.All };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void PlanExample_IncludeAreas_MatchAny_SearchMatches_Allowed()
	{
		// include_areas: [Search], match: any, entry areas: ["Search", "Internal"] → Allowed
		var blocker = new PublishBlocker { Areas = ["Search"], AreasMode = FieldMode.Include, MatchAreas = MatchMode.Any };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeFalse();
	}

	[Fact]
	public void PlanExample_IncludeAreas_MatchAll_InternalNotInList_Blocked()
	{
		// include_areas: [Search], match: all, entry areas: ["Search", "Internal"] → Blocked
		var blocker = new PublishBlocker { Areas = ["Search"], AreasMode = FieldMode.Include, MatchAreas = MatchMode.All };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		blocker.ShouldBlock(entry).Should().BeTrue();
	}
}
