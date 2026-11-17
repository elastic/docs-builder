// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Domain;

namespace Elastic.Documentation.Tests.Domain;

public class DomainOperationTests
{
	private static readonly Diagnostic Own = new() { Severity = Severity.Hint, File = "own", Message = "own" };

	[Test]
	public void Built_CarriesEveryFinding_AndTheValueEvenWithErrors()
	{
		var broken = new Widget("", 50);

		var result = DomainOperation.Built(WidgetRules.All, broken, [Own], "w.yml");

		result.Value.Should().BeSameAs(broken);
		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Select(d => d.Message).Should().Equal("own", "Widget has no name.", "Widget is large.");
		result.Diagnostics.Skip(1).Should().OnlyContain(d => d.File == "w.yml");
	}

	[Test]
	public void Derived_KeepsOnlyErrors_AfterTheOperationsOwn()
	{
		var result = DomainOperation.Derived(WidgetRules.All, new Widget("", 50), [Own]);

		result.Diagnostics.Select(d => d.Message).Should().Equal("own", "Widget has no name.");
	}

	[Test]
	public void Derived_ValidValue_HasNoDiagnostics()
	{
		var result = DomainOperation.Derived(WidgetRules.All, new Widget("n", 50));

		result.HasErrors.Should().BeFalse();
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("  ")]
	[Arguments("not-a-lifecycle")]
	public void ParseLifecycle_BlankOrUnknown_IsNull(string? text) => DomainOperation.ParseLifecycle(text).Should().BeNull();

	[Test]
	[Arguments("ga", Lifecycle.Ga)]
	[Arguments("GA", Lifecycle.Ga)]
	[Arguments("beta", Lifecycle.Beta)]
	[Arguments("Preview", Lifecycle.Preview)]
	public void ParseLifecycle_KnownText_IgnoresCase(string text, Lifecycle expected) =>
		DomainOperation.ParseLifecycle(text).Should().Be(expected);
}
