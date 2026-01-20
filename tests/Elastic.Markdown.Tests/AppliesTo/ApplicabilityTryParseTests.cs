// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Markdown.Tests.AppliesTo;

public class ApplicabilityTryParseTests
{
	[Theory]
	[InlineData("ga", ProductLifecycle.GenerallyAvailable)]
	[InlineData("GA", ProductLifecycle.GenerallyAvailable)]
	[InlineData("preview", ProductLifecycle.TechnicalPreview)]
	[InlineData("tech-preview", ProductLifecycle.TechnicalPreview)]
	[InlineData("beta", ProductLifecycle.Beta)]
	[InlineData("deprecated", ProductLifecycle.Deprecated)]
	[InlineData("removed", ProductLifecycle.Removed)]
	public void ValidLifecycleReturnsTrueAndParsesCorrectly(string input, ProductLifecycle expectedLifecycle)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(input, diagnostics, out var applicability);

		result.Should().BeTrue();
		applicability.Should().NotBeNull();
		applicability!.Lifecycle.Should().Be(expectedLifecycle);
		diagnostics.Should().NotContain(d => d.Item1 == Severity.Error);
	}

	[Theory]
	[InlineData("ga 8.0", ProductLifecycle.GenerallyAvailable)]
	[InlineData("beta 9.1.0", ProductLifecycle.Beta)]
	[InlineData("preview 10.0+", ProductLifecycle.TechnicalPreview)]
	public void ValidLifecycleWithVersionReturnsTrueAndParsesCorrectly(string input, ProductLifecycle expectedLifecycle)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(input, diagnostics, out var applicability);

		result.Should().BeTrue();
		applicability.Should().NotBeNull();
		applicability!.Lifecycle.Should().Be(expectedLifecycle);
		applicability.Version.Should().NotBeNull();
		diagnostics.Should().NotContain(d => d.Item1 == Severity.Error);
	}

	[Theory]
	[InlineData("9.0")]
	[InlineData("8.5.0")]
	[InlineData("10")]
	[InlineData("v8.0")]
	[InlineData("invalid")]
	[InlineData("available")]
	[InlineData("released")]
	[InlineData("latest")]
	public void InvalidLifecycleReturnsFalseWithDiagnostic(string invalidLifecycle)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(invalidLifecycle, diagnostics, out var applicability);

		result.Should().BeFalse();
		applicability.Should().BeNull();
		diagnostics.Should().ContainSingle(d => d.Item1 == Severity.Error);
		diagnostics.First().Item2.Should().Contain("Unknown product lifecycle");
		diagnostics.First().Item2.Should().Contain(invalidLifecycle.Split(' ')[0]);
	}

	[Theory]
	[InlineData("9.0 8.5")]
	[InlineData("8.0.0 ga")]
	public void VersionAsFirstTokenReturnsFalseWithDiagnostic(string input)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(input, diagnostics, out var applicability);

		result.Should().BeFalse();
		applicability.Should().BeNull();
		diagnostics.Should().ContainSingle(d => d.Item1 == Severity.Error);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(null)]
	[InlineData("all")]
	[InlineData("ALL")]
	public void EmptyOrAllReturnsGenerallyAvailable(string? input)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(input, diagnostics, out var applicability);

		result.Should().BeTrue();
		applicability.Should().Be(Applicability.GenerallyAvailable);
		diagnostics.Should().BeEmpty();
	}
}

public class AppliesCollectionTryParseTests
{
	[Theory]
	[InlineData("9.0")]
	[InlineData("8.5.0")]
	[InlineData("invalid")]
	public void InvalidLifecycleReturnsFalseWithDiagnostic(string invalidLifecycle)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = AppliesCollection.TryParse(invalidLifecycle, diagnostics, out _);

		result.Should().BeFalse();
		diagnostics.Should().ContainSingle(d => d.Item1 == Severity.Error);
		diagnostics.First().Item2.Should().Contain("Unknown product lifecycle");
	}

	[Fact]
	public void MultipleItemsWithOneInvalidReturnsTrueButSkipsInvalid()
	{
		var diagnostics = new List<(Severity, string)>();

		var result = AppliesCollection.TryParse("ga 8.0, 9.0, beta 7.0", diagnostics, out var collection);

		// Should return true because some items were successfully parsed
		result.Should().BeTrue();
		collection.Should().NotBeNull();
		collection!.Count.Should().Be(2); // Only ga 8.0 and beta 7.0 should be parsed
		diagnostics.Should().ContainSingle(d => d.Item1 == Severity.Error);
	}

	[Fact]
	public void AllInvalidItemsReturnsFalse()
	{
		var diagnostics = new List<(Severity, string)>();

		var result = AppliesCollection.TryParse("9.0, 8.5, invalid", diagnostics, out _);

		result.Should().BeFalse();
		diagnostics.Count.Should().Be(3);
	}
}
