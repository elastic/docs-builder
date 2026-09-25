// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Markdown.Tests.AppliesTo;

public class ApplicabilityTryParseTests
{
	[Test]
	[Arguments("ga", ProductLifecycle.GenerallyAvailable)]
	[Arguments("GA", ProductLifecycle.GenerallyAvailable)]
	[Arguments("preview", ProductLifecycle.TechnicalPreview)]
	[Arguments("tech-preview", ProductLifecycle.TechnicalPreview)]
	[Arguments("experimental", ProductLifecycle.Experimental)]
	[Arguments("beta", ProductLifecycle.Beta)]
	[Arguments("deprecated", ProductLifecycle.Deprecated)]
	[Arguments("removed", ProductLifecycle.Removed)]
	public void ValidLifecycleReturnsTrueAndParsesCorrectly(string input, ProductLifecycle expectedLifecycle)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(input, diagnostics, out var applicability);

		result.Should().BeTrue();
		applicability.Should().NotBeNull();
		applicability.Lifecycle.Should().Be(expectedLifecycle);
		diagnostics.Should().NotContain(d => d.Item1 == Severity.Error);
	}

	[Test]
	[Arguments("ga 8.0", ProductLifecycle.GenerallyAvailable)]
	[Arguments("beta 9.1.0", ProductLifecycle.Beta)]
	[Arguments("experimental 9.1.0", ProductLifecycle.Experimental)]
	[Arguments("preview 10.0+", ProductLifecycle.TechnicalPreview)]
	public void ValidLifecycleWithVersionReturnsTrueAndParsesCorrectly(string input, ProductLifecycle expectedLifecycle)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(input, diagnostics, out var applicability);

		result.Should().BeTrue();
		applicability.Should().NotBeNull();
		applicability.Lifecycle.Should().Be(expectedLifecycle);
		applicability.Version.Should().NotBeNull();
		diagnostics.Should().NotContain(d => d.Item1 == Severity.Error);
	}

	[Test]
	[Arguments("9.0")]
	[Arguments("8.5.0")]
	[Arguments("10")]
	[Arguments("v8.0")]
	[Arguments("invalid")]
	[Arguments("available")]
	[Arguments("released")]
	[Arguments("latest")]
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

	[Test]
	[Arguments("9.0 8.5")]
	[Arguments("8.0.0 ga")]
	public void VersionAsFirstTokenReturnsFalseWithDiagnostic(string input)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = Applicability.TryParse(input, diagnostics, out var applicability);

		result.Should().BeFalse();
		applicability.Should().BeNull();
		diagnostics.Should().ContainSingle(d => d.Item1 == Severity.Error);
	}

	[Test]
	[Arguments("")]
	[Arguments("   ")]
	[Arguments(null)]
	[Arguments("all")]
	[Arguments("ALL")]
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
	[Test]
	[Arguments("9.0")]
	[Arguments("8.5.0")]
	[Arguments("invalid")]
	public void InvalidLifecycleReturnsFalseWithDiagnostic(string invalidLifecycle)
	{
		var diagnostics = new List<(Severity, string)>();

		var result = AppliesCollection.TryParse(invalidLifecycle, diagnostics, out _);

		result.Should().BeFalse();
		diagnostics.Should().ContainSingle(d => d.Item1 == Severity.Error);
		diagnostics.First().Item2.Should().Contain("Unknown product lifecycle");
	}

	[Test]
	public void MultipleItemsWithOneInvalidReturnsTrueButSkipsInvalid()
	{
		var diagnostics = new List<(Severity, string)>();

		var result = AppliesCollection.TryParse("ga 8.0, 9.0, beta 7.0", diagnostics, out var collection);

		// Should return true because some items were successfully parsed
		result.Should().BeTrue();
		collection.Should().NotBeNull();
		collection.Count.Should().Be(2); // Only ga 8.0 and beta 7.0 should be parsed
		diagnostics.Should().ContainSingle(d => d.Item1 == Severity.Error);
	}

	[Test]
	public void AllInvalidItemsReturnsFalse()
	{
		var diagnostics = new List<(Severity, string)>();

		var result = AppliesCollection.TryParse("9.0, 8.5, invalid", diagnostics, out _);

		result.Should().BeFalse();
		diagnostics.Count.Should().Be(3);
	}
}
