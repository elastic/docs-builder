// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Severity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Tests.Domain;

public class CheckTests
{
	[Test]
	public void Check_ValidRecord_ReturnsItWithNoFindings()
	{
		var loaded = WidgetConstraints.Valid.Check("w.yml");

		loaded.Value.Should().Be(WidgetConstraints.Valid);
		loaded.Findings.Should().BeEmpty();
	}

	[Test]
	public void Check_ViolatedRules_ReportsEveryFindingInSetOrder()
	{
		var loaded = new Widget("", -1).Check("w.yml");

		loaded
			.Findings
			.Select(d => (d.Severity, d.RuleId))
			.Should()
			.Equal((Severity.Error, "name-required"), (Severity.Hint, "size-positive"));
	}

	[Test]
	public void Check_AttributesFindingsToTheFile() =>
		new Widget("n", 50).Check("declared.yml").Findings.Should().ContainSingle().Which.File.Should().Be("declared.yml");

	[Test]
	public void Check_ErrorSeverityRule_StillReturnsLoaded_TheCallerDecidesFatality()
	{
		var loaded = new Widget("", 5).Check("w.yml");

		loaded.Should().BeOfType<Loaded<Widget>>();
		loaded.Value.Name.Should().BeEmpty();
		loaded.Findings.Should().ContainSingle().Which.Severity.Should().Be(Severity.Error);
	}

	[Test]
	public void Check_MatchesTheSetsOwnReport()
	{
		var widget = new Widget("", 50);

		widget.Check("w.yml").Findings.Should().Equal(Widget.Constraints.Report(widget).ToDiagnostics("w.yml"));
	}

	[Test]
	public void Check_StageType_UsesTheStagesOwnRules()
	{
		new DraftWidget("").Check("d.yml").Findings.Select(d => d.Message).Should().Equal("A draft needs a name.");
		new DraftWidget("x").Check("d.yml").Findings.Should().BeEmpty();
	}
}
