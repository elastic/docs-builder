// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Domain;

public class ResultTests
{
	private static Diagnostic Of(Severity severity, string message) => new() { Severity = severity, File = "f.yml", Message = message };

	private static readonly Widget Value = WidgetRules.Valid;

	[Test]
	public void Success_HasValueAndNoErrors()
	{
		var result = Result.Success(Value);

		result.Value.Should().BeSameAs(Value);
		result.Diagnostics.Should().BeEmpty();
		result.HasErrors.Should().BeFalse();
	}

	[Test]
	public void Success_WithWarning_IsNotAnError() => Result.Success(Value, [Of(Severity.Warning, "w")]).HasErrors.Should().BeFalse();

	[Test]
	public void Failure_HasNoValueAndAnError()
	{
		var result = Result.Failure<Widget>([Of(Severity.Error, "e")]);

		result.Value.Should().BeNull();
		result.HasErrors.Should().BeTrue();
	}

	[Test]
	public void Report_WritesEveryDiagnosticInOrder_AndReturnsItself()
	{
		var collector = new BufferedCollector();
		var result = Result.Success(Value, [Of(Severity.Warning, "a"), Of(Severity.Error, "b"), Of(Severity.Hint, "c")]);

		result.Report(collector).Should().BeSameAs(result);

		collector.Written.Select(d => d.Message).Should().Equal("a", "b", "c");
	}

	[Test]
	public void EmitTo_ReportsAndReturnsTheValue_EvenWithErrors()
	{
		var collector = new BufferedCollector();
		var result = new Result<Widget>(Value, [Of(Severity.Error, "e")]);

		result.EmitTo(collector).Should().BeSameAs(Value);

		collector.Written.Should().HaveCount(1);
	}

	[Test]
	public void EmitErrorsTo_WritesOnlyErrors_AndReturnsNullWhenThereAreSome()
	{
		var collector = new BufferedCollector();
		var result = new Result<Widget>(Value, [Of(Severity.Warning, "w"), Of(Severity.Error, "e")]);

		result.EmitErrorsTo(collector).Should().BeNull();

		collector.Written.Select(d => d.Message).Should().Equal("e");
	}

	[Test]
	public void EmitErrorsTo_WithOnlyWarnings_WritesNothingAndReturnsTheValue()
	{
		var collector = new BufferedCollector();
		var result = Result.Success(Value, [Of(Severity.Warning, "w")]);

		result.EmitErrorsTo(collector).Should().BeSameAs(Value);

		collector.Written.Should().BeEmpty();
	}

	[Test]
	public void OrThrowIfMissing_ReturnsTheValueDespiteErrors() =>
		new Result<Widget>(Value, [Of(Severity.Error, "e")]).OrThrowIfMissing().Should().BeSameAs(Value);

	[Test]
	public void OrThrowIfMissing_WithoutValue_ThrowsTheLastErrorMessage()
	{
		var result = Result.Failure<Widget>([Of(Severity.Error, "first"), Of(Severity.Warning, "w"), Of(Severity.Error, "last")]);

		var act = () => result.OrThrowIfMissing();

		act.Should().Throw<InvalidOperationException>().WithMessage("last");
	}

	[Test]
	public void OrThrowIfMissing_WithoutValueOrErrors_NamesTheType()
	{
		var act = () => Result.Failure<Widget>([]).OrThrowIfMissing();

		act.Should().Throw<InvalidOperationException>().WithMessage("Could not load Widget");
	}

	[Test]
	public void OrThrow_WithoutErrors_ReturnsTheValue() =>
		Result.Success(Value, [Of(Severity.Warning, "w")]).OrThrow().Should().BeSameAs(Value);

	[Test]
	public void OrThrow_WithErrors_ThrowsWithEveryErrorAndEveryDiagnostic()
	{
		var result = new Result<Widget>(Value, [Of(Severity.Error, "one"), Of(Severity.Warning, "w"), Of(Severity.Error, "two")]);

		var act = () => result.OrThrow();

		var thrown = act.Should().Throw<ConfigurationException>().Which;
		thrown.Message.Should().Be("one" + Environment.NewLine + "two");
		thrown.Diagnostics.Should().HaveCount(3);
	}

	[Test]
	public void OrThrow_WithoutValue_ThrowsEvenWithoutErrors()
	{
		var act = () => Result.Failure<Widget>([]).OrThrow();

		act.Should().Throw<ConfigurationException>().WithMessage("Could not load Widget");
	}

	[Test]
	public void From_CarriesTheValue_AndPutsExtraBeforeTheViolations()
	{
		var report = WidgetRules.All.Report(new Widget("", 50));

		var result = Result.From(report, "w.yml", [Of(Severity.Hint, "extra")]);

		result.Value.Should().NotBeNull();
		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Select(d => d.Message).Should().Equal("extra", "Widget has no name.", "Widget is large.");
	}

	[Test]
	public void From_ValidValue_HasNoDiagnostics() => Result.From(WidgetRules.All.Report(Value), "w.yml").Diagnostics.Should().BeEmpty();

	[Test]
	public void ConfigurationException_Lines_ShowPositionAndRuleId()
	{
		var diagnostics = new[]
		{
			new Diagnostic { Severity = Severity.Error, File = "a.yml", Message = "m1" },
			new Diagnostic { Severity = Severity.Warning, File = "a.yml", Message = "m2", Line = 3, RuleId = "r" },
			new Diagnostic { Severity = Severity.Hint, File = "a.yml", Message = "m3", Line = 4, Column = 5 }
		};

		new ConfigurationException(diagnostics)
			.Lines
			.Should()
			.Equal("error: a.yml: m1", "warning: a.yml(3): m2 [r]", "hint: a.yml(4,5): m3");
	}
}
