// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Severity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Configuration.Tests.Loading;

public class LoaderPipelineTests
{
	private sealed record Dto(string Name, int Size);

	private sealed record Widget(string Name, int Size);

	private static readonly Nullean.Entities.ConstraintSet<Widget> Rules = Nullean
		.Entities
		.ConstraintSet
		.From(
			("name-required", Nullean
				.Entities
				.Constraints<Widget>
				.Always(w => !string.IsNullOrEmpty(w.Name), "Widget has no name.")
				.Named("name-required")),
			("size-small", Nullean
				.Entities
				.Constraints<Widget>
				.Always(w => w.Size <= 10, "Widget is large.")
				.WithSeverity(Nullean.Entities.Severity.Warning)
				.Named("size-small"))
		);

	private static Result<Widget> Load(Dto dto, bool errorsAreFatal = true, IReadOnlyList<Diagnostic>? extra = null) =>
		LoaderPipeline.Run("w.yml", () => dto, d => new Widget(d.Name, d.Size), Rules, extra, errorsAreFatal: errorsAreFatal);

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
		var result = Load(new Dto("", 1), errorsAreFatal: false);

		result.Value.Should().NotBeNull();
		result.HasErrors.Should().BeTrue();
	}

	[Test]
	public void Run_ExtraFindings_ComeBeforeTheViolations()
	{
		var extra = new Diagnostic { Severity = Severity.Hint, File = "w.yml", Message = "extra" };

		var result = Load(new Dto("ok", 50), extra: [extra]);

		result.Diagnostics.Select(d => d.Message).Should().Equal("extra", "Widget is large.");
	}

	[Test]
	public void Run_ParseThrows_BecomesAnErrorDiagnosticWithTheMessage()
	{
		var result = LoaderPipeline.Run<Dto, Widget>(
			"w.yml",
			() => throw new InvalidOperationException("bad yaml"),
			d => new Widget(d.Name, d.Size),
			Rules
		);

		result.Value.Should().BeNull();
		var diagnostic = result.Diagnostics.Should().ContainSingle().Which;
		(diagnostic.Severity, diagnostic.File, diagnostic.Message).Should().Be((Severity.Error, "w.yml", "bad yaml"));
	}

	[Test]
	public void Run_MapThrows_BecomesAnErrorDiagnostic()
	{
		var result = LoaderPipeline.Run<Dto, Widget>(
			"w.yml",
			() => new Dto("a", 1),
			_ => throw new InvalidOperationException("cannot map"),
			Rules
		);

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("cannot map");
	}

	[Test]
	public void Run_FileOf_ReportsViolationsAgainstTheRecordsOwnFile()
	{
		var result = LoaderPipeline.Run("w.yml", () => new Widget("", 1), Rules, fileOf: _ => "legacy.yml", errorsAreFatal: false);

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
	public void Check_ListOfSets_ReportsEverySetAndIsFatalOnError()
	{
		var onlyName = Nullean.Entities.ConstraintSet.From(("name-required", Rules.AsConstraints().First()));

		var result = LoaderPipeline.Check(new Widget("", 50), "w.yml", [onlyName, Rules]);

		result.Value.Should().BeNull();
		result.Diagnostics.Select(d => d.RuleId).Should().Equal("name-required", "name-required", "size-small");
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
