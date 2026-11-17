// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Severity = Elastic.Documentation.Diagnostics.Severity;
using ConstraintSet = Nullean.Entities.ConstraintSet;

namespace Elastic.Documentation.Configuration.Tests.Loading;

public class LoaderPipelineTests
{
	private sealed record Dto(string Name, int Size);

	private sealed record Widget(string Name, int Size) : Nullean.Entities.IHaveConstraints<Widget>
	{
		public static Nullean.Entities.ConstraintSet<Widget> Constraints => Rules;
	}

	private static readonly Nullean.Entities.Constraint<Widget> NameRequired = Nullean
		.Entities
		.Constraints<Widget>
		.Always(w => !string.IsNullOrEmpty(w.Name), "Widget has no name.")
		.Named("name-required");

	private static readonly Nullean.Entities.Constraint<Widget> SizeSmall = Nullean
		.Entities
		.Constraints<Widget>
		.Always(w => w.Size <= 10, "Widget is large.")
		.WithSeverity(Nullean.Entities.Severity.Warning)
		.Named("size-small");

	private static readonly Nullean.Entities.ConstraintSet<Widget> Rules = ConstraintSet.From(
		("name-required", NameRequired),
		("size-small", SizeSmall)
	);

	private static Result<Widget> Load(Dto dto, LoadOptions<Widget>? options = null) =>
		LoaderPipeline.Run("w.yml", () => dto, d => new Widget(d.Name, d.Size), options);

	private static Diagnostic Hint(string message) => new() { Severity = Severity.Hint, File = "w.yml", Message = message };

	[Test]
	public void Run_ValidInput_ReturnsTheMappedRecord()
	{
		var result = Load(new Dto("ok", 1));

		result.Value.Should().Be(new Widget("ok", 1));
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void Run_WarningOnly_KeepsTheValueAndTheWarning()
	{
		var result = Load(new Dto("ok", 50));

		result.Value.Should().NotBeNull();
		result.HasErrors.Should().BeFalse();
		result.Diagnostics.Should().ContainSingle().Which.RuleId.Should().Be("size-small");
	}

	[Test]
	public void Run_ErrorIsFatalByDefault_ReturnsNoValueButEveryDiagnostic()
	{
		var result = Load(new Dto("", 50));

		result.Value.Should().BeNull();
		result.Diagnostics.Select(d => d.RuleId).Should().Equal("name-required", "size-small");
	}

	[Test]
	public void Run_ErrorsNotFatal_ReturnsTheValueWithItsErrors()
	{
		var result = Load(new Dto("", 1), new LoadOptions<Widget> { ErrorsAreFatal = false });

		result.Value.Should().NotBeNull();
		result.HasErrors.Should().BeTrue();
	}

	[Test]
	public void Run_WithoutRules_ChecksAgainstTheTypesOwnConstraints() =>
		Load(new Dto("", 1)).Diagnostics.Should().ContainSingle().Which.RuleId.Should().Be("name-required");

	[Test]
	public void Run_ExplicitRules_WinOverTheTypesConstraints()
	{
		var onlySize = ConstraintSet.From(("size-small", SizeSmall));

		var result = Load(new Dto("", 50), new LoadOptions<Widget> { Rules = onlySize });

		result.Value.Should().NotBeNull("the name rule is not in the explicit set");
		result.Diagnostics.Select(d => d.RuleId).Should().Equal("size-small");
	}

	[Test]
	public void Run_MergedRules_ReportEverySet()
	{
		var merged = ConstraintSet.Merge(
			ConstraintSet.From(("name-required", NameRequired)),
			ConstraintSet.From(("size-small", SizeSmall))
		);

		var result = Load(new Dto("", 50), new LoadOptions<Widget> { Rules = merged, ErrorsAreFatal = false });

		result.Diagnostics.Select(d => d.RuleId).Should().Equal("name-required", "size-small");
	}

	[Test]
	public void Run_ExtraFindings_ComeBeforeTheViolations()
	{
		var result = Load(new Dto("ok", 50), new LoadOptions<Widget> { Extra = [Hint("extra")] });

		result.Diagnostics.Select(d => d.Message).Should().Equal("extra", "Widget is large.");
	}

	[Test]
	public void Run_ParseThrows_BecomesAnErrorDiagnosticWithTheMessage()
	{
		var result = LoaderPipeline.Run<Dto, Widget>(
			"w.yml",
			() => throw new InvalidOperationException("bad yaml"),
			d => new Widget(d.Name, d.Size)
		);

		result.Value.Should().BeNull();
		var diagnostic = result.Diagnostics.Should().ContainSingle().Which;
		(diagnostic.Severity, diagnostic.File, diagnostic.Message).Should().Be((Severity.Error, "w.yml", "bad yaml"));
	}

	[Test]
	public void Run_MapAddsAFindingThenThrows_ReturnsTheFindingBeforeTheError()
	{
		var extra = new List<Diagnostic>();

		var result = LoaderPipeline.Run<Dto, Widget>(
			"w.yml",
			() => new Dto("a", 1),
			_ =>
			{
				extra.Add(Hint("found while mapping"));
				throw new InvalidOperationException("cannot map");
			},
			new LoadOptions<Widget> { Extra = extra }
		);

		result.Value.Should().BeNull();
		result
			.Diagnostics
			.Select(d => (d.Severity, d.Message))
			.Should()
			.Equal((Severity.Hint, "found while mapping"), (Severity.Error, "cannot map"));
	}

	[Test]
	public void Run_Describe_ShapesTheTextOfTheErrorOnly()
	{
		var result = LoaderPipeline.Run<Dto, Widget>(
			"w.yml",
			() => throw new InvalidOperationException("outer", new FormatException("inner")),
			d => new Widget(d.Name, d.Size),
			new LoadOptions<Widget> { Describe = e => $"{e.Message} / {e.InnerException?.Message}" }
		);

		result.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("outer / inner");
	}

	[Test]
	public void Run_Cancellation_Propagates()
	{
		var act =
			() => LoaderPipeline.Run<Dto, Widget>("w.yml", () => throw new OperationCanceledException(), d => new Widget(d.Name, d.Size));

		act.Should().Throw<OperationCanceledException>();
	}

	[Test]
	public void Run_FileOf_ReportsViolationsAgainstTheRecordsOwnFile()
	{
		var result = LoaderPipeline.Run(
			"w.yml",
			() => new Widget("", 1),
			new LoadOptions<Widget> { FileOf = _ => "legacy.yml", ErrorsAreFatal = false }
		);

		result.Diagnostics.Should().ContainSingle().Which.File.Should().Be("legacy.yml");
	}

	[Test]
	public void Build_OnlyBuilds_AndDoesNotCheck()
	{
		var result = LoaderPipeline.Build("w.yml", () => new Widget("", 50));

		result.Value.Should().NotBeNull();
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void Build_Throws_ReturnsTheExtraFindingsThenTheDescribedError()
	{
		var result = LoaderPipeline.Build<Widget>(
			"w.yml",
			() => throw new InvalidOperationException("boom"),
			new LoadOptions<Widget> { Extra = [Hint("before")], Describe = e => $"described {e.Message}" }
		);

		result.Diagnostics.Select(d => d.Message).Should().Equal("before", "described boom");
	}

	[Test]
	public void Check_ExplicitRules_AreHonoured_AndErrorsAreFatalByDefault()
	{
		var result = LoaderPipeline.Check(new Widget("", 50), "w.yml");

		result.Value.Should().BeNull();
		result.Diagnostics.Select(d => d.RuleId).Should().Equal("name-required", "size-small");
	}

	[Test]
	public void Failed_IsAnErrorOnTheFile()
	{
		var result = LoaderPipeline.Failed<Widget>("w.yml", "nope");

		result.Value.Should().BeNull();
		result
			.Diagnostics
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Match<Diagnostic>(d => d.Severity == Severity.Error && d.File == "w.yml" && d.Message == "nope");
	}
}
