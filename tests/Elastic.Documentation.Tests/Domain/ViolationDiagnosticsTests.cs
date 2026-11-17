// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Nullean.Entities;
using Severity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Tests.Domain;

public class ViolationDiagnosticsTests
{
	private static readonly Widget Broken = new("", 50);

	[Test]
	public void ToDiagnostics_MapsSeverityMessageAndRuleId_InReportedOrder()
	{
		var diagnostics = Widget.Constraints.Report(new Widget("", -1)).ToDiagnostics("w.yml");

		diagnostics
			.Select(d => (d.Severity, d.RuleId, d.Message))
			.Should()
			.Equal((Severity.Error, "name-required", "Widget has no name."), (Severity.Hint, "size-positive", "Widget has no size."));
	}

	[Test]
	public void ToDiagnostics_WarningRule_IsAWarning()
	{
		var diagnostics = Widget.Constraints.Report(new Widget("n", 50)).ToDiagnostics("w.yml");

		diagnostics.Should().ContainSingle().Which.Severity.Should().Be(Severity.Warning);
	}

	[Test]
	public void ToDiagnostic_WithoutOwnFile_UsesTheDeclaringFile()
	{
		var diagnostic = Widget.Constraints.Report(Broken).Violations[0].ToDiagnostic("declared.yml");

		diagnostic.File.Should().Be("declared.yml");
		diagnostic.Line.Should().BeNull();
	}

	[Test]
	public void ToDiagnostic_WithOwnLocation_PointsAtIt()
	{
		var located = Widget.Constraints.Report(Broken).Violations[0].At(new SourceLocation("own.yml", 3, 4, 5));

		var diagnostic = located.ToDiagnostic("declared.yml");

		(diagnostic.File, diagnostic.Line, diagnostic.Column, diagnostic.Length).Should().Be(("own.yml", 3, 4, 5));
	}

	[Test]
	public void WriteViolations_ReportsEveryViolation_AndSaysWhetherTheValueIsUsable()
	{
		var collector = new BufferedCollector();

		var usable = collector.WriteViolations(Widget.Constraints.Report(WidgetConstraints.Valid), "w.yml");
		var warningOnly = collector.WriteViolations(Widget.Constraints.Report(new Widget("n", 50)), "w.yml");
		var broken = collector.WriteViolations(Widget.Constraints.Report(Broken), "w.yml");

		(usable, warningOnly, broken).Should().Be((true, true, false));
		collector.Written.Select(d => d.RuleId).Should().Equal("size-small", "name-required", "size-small");
	}

	[Test]
	public void ApplyOutcome_Applied_CarriesTheNoticesItWasLetThroughWith()
	{
		var outcome = Widget.Constraints.ApplyDetailed(WidgetConstraints.Valid, w => w with { Size = 50 });

		outcome.ToDiagnostics("w.yml").Select(d => (d.Severity, d.RuleId)).Should().Equal((Severity.Warning, "size-small"));
	}

	[Test]
	public void ApplyOutcome_Failure_CarriesTheViolations()
	{
		var outcome = Widget.Constraints.ApplyDetailed(WidgetConstraints.Valid, w => w with { Name = "" });

		outcome
			.ToDiagnostics("w.yml")
			.Select(d => (d.Severity, d.RuleId, d.File))
			.Should()
			.Equal((Severity.Error, "name-required", "w.yml"));
	}

	[Test]
	public void ApplyOutcome_Noop_HasNoFindings() =>
		Widget.Constraints.ApplyDetailed(WidgetConstraints.Valid, w => w).ToDiagnostics("w.yml").Should().BeEmpty();
}
