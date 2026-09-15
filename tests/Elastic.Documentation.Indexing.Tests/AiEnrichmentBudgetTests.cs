// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Indexing.Tests;

public class AiEnrichmentBudgetTests
{
	[Fact]
	public void TryValidateMaxTime_NullValue_ReturnsTrue()
	{
		var result = AiEnrichmentBudget.TryValidateMaxTime(null, out var error);

		result.Should().BeTrue();
		error.Should().BeNull();
	}

	[Fact]
	public void TryValidateMaxTime_ExactlyMinimum_ReturnsTrue()
	{
		var result = AiEnrichmentBudget.TryValidateMaxTime(AiEnrichmentDefaults.MinWallClock, out var error);

		result.Should().BeTrue();
		error.Should().BeNull();
	}

	[Fact]
	public void TryValidateMaxTime_BelowMinimum_ReturnsFalse()
	{
		var result = AiEnrichmentBudget.TryValidateMaxTime(TimeSpan.FromSeconds(30), out var error);

		result.Should().BeFalse();
		error.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void TryValidateMaxTime_AboveMinimum_ReturnsTrue()
	{
		var result = AiEnrichmentBudget.TryValidateMaxTime(TimeSpan.FromHours(2), out var error);

		result.Should().BeTrue();
		error.Should().BeNull();
	}

	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	[InlineData(-5)]
	public void EffectiveMaxDocs_NonPositiveOrUnset_FallsBackToDefault(int? maxDocs)
	{
		var budget = new AiEnrichmentBudget(maxDocs, null);

		budget.EffectiveMaxDocs.Should().Be(AiEnrichmentDefaults.MaxEnrichmentsPerRun);
	}

	[Fact]
	public void EffectiveMaxDocs_PositiveValue_ReturnsThatValue()
	{
		var budget = new AiEnrichmentBudget(42, null);

		budget.EffectiveMaxDocs.Should().Be(42);
	}

	[Fact]
	public void Default_HasSharedDefaultDocCountAndNoTimeLimit()
	{
		AiEnrichmentBudget.Default.EffectiveMaxDocs.Should().Be(AiEnrichmentDefaults.MaxEnrichmentsPerRun);
		AiEnrichmentBudget.Default.MaxTime.Should().BeNull();
	}
}
