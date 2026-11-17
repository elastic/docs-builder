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

		var result = DomainOperation.Built(WidgetRules.All, broken, "w.yml", [Own]);

		result.Value.Should().BeSameAs(broken);
		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Select(d => d.Message).Should().Equal("own", "Widget has no name.", "Widget is large.");
		result.Diagnostics.Skip(1).Should().OnlyContain(d => d.File == "w.yml");
	}

	[Test]
	public void Derived_KeepsOnlyErrors_AfterTheOperationsOwn()
	{
		var result = DomainOperation.Derived(WidgetRules.All, new Widget("", 50), "w.yml", [Own]);

		result.Diagnostics.Select(d => d.Message).Should().Equal("own", "Widget has no name.");
	}

	[Test]
	public void Derived_ValidValue_HasNoDiagnostics()
	{
		var result = DomainOperation.Derived(WidgetRules.All, new Widget("n", 50), "w.yml");

		result.HasErrors.Should().BeFalse();
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void Derived_ErrorsAreReportedAgainstTheGivenFile()
	{
		var result = DomainOperation.Derived(WidgetRules.All, new Widget("", 1), "derived.yml");

		result.Diagnostics.Should().ContainSingle().Which.File.Should().Be("derived.yml");
	}

	[Test]
	public void Derived_OwnWarning_IsInDiagnosticsAndEmitTo_ButNotEmitErrorsTo()
	{
		var warning = new Diagnostic { Severity = Severity.Warning, File = "own", Message = "own warning" };
		var result = DomainOperation.Derived(WidgetRules.All, WidgetRules.Valid, "w.yml", [warning]);

		var viaEmitTo = new BufferedCollector();
		var viaEmitErrorsTo = new BufferedCollector();
		_ = result.EmitTo(viaEmitTo);
		_ = result.EmitErrorsTo(viaEmitErrorsTo);

		result.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("own warning");
		viaEmitTo.Written.Should().ContainSingle();
		viaEmitErrorsTo.Written.Should().BeEmpty();
	}
}
