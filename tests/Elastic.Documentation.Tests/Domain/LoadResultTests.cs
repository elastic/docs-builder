// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Domain;

public class LoadResultTests
{
	private static Diagnostic Of(Severity severity, string message) => new() { Severity = severity, File = "f.yml", Message = message };

	private static readonly Widget Value = WidgetConstraints.Valid;

	private static LoadResult<Widget> Loaded(params Diagnostic[] findings) => new Loaded<Widget>(Value, findings);

	private static LoadResult<Widget> Unreadable(params Diagnostic[] findings) => new Unreadable<Widget>(findings);

	[Test]
	public void Loaded_WithoutErrors_HasTheValueAndNoErrors()
	{
		var result = Loaded();

		result.ValueOrNull().Should().BeSameAs(Value);
		result.Findings().Should().BeEmpty();
		result.HasErrors().Should().BeFalse();
	}

	[Test]
	public void Loaded_WithWarning_IsNotAnError() => Loaded(Of(Severity.Warning, "w")).HasErrors().Should().BeFalse();

	[Test]
	public void Loaded_MayStillHoldErrors() => Loaded(Of(Severity.Error, "e")).HasErrors().Should().BeTrue();

	[Test]
	public void Unreadable_HasNoValue()
	{
		var result = Unreadable(Of(Severity.Error, "e"));

		result.ValueOrNull().Should().BeNull();
		result.HasErrors().Should().BeTrue();
	}

	[Test]
	public void EveryCase_CanBeMatchedExhaustively()
	{
		static string Describe(LoadResult<Widget> r) => r switch
		{
			Loaded<Widget> l => $"loaded {l.Value.Name}",
			Unreadable<Widget> => "unreadable"
		};

		Describe(Loaded()).Should().Be("loaded ok");
		Describe(Unreadable()).Should().Be("unreadable");
	}

	[Test]
	public void Report_WritesEveryFindingInOrder_AndReturnsItself()
	{
		var collector = new BufferedCollector();
		var result = Loaded(Of(Severity.Warning, "a"), Of(Severity.Error, "b"), Of(Severity.Hint, "c"));

		result.Report(collector).Should().Be(result);

		collector.Written.Select(d => d.Message).Should().Equal("a", "b", "c");
	}

	[Test]
	public void EmitTo_ReportsAndReturnsTheValue_EvenWithErrors()
	{
		var collector = new BufferedCollector();

		Loaded(Of(Severity.Error, "e")).EmitTo(collector).Should().BeSameAs(Value);

		collector.Written.Should().HaveCount(1);
	}

	[Test]
	public void EmitTo_Unreadable_ReportsAndReturnsNull()
	{
		var collector = new BufferedCollector();

		Unreadable(Of(Severity.Error, "e")).EmitTo(collector).Should().BeNull();

		collector.Written.Should().HaveCount(1);
	}

	[Test]
	public void EmitErrorsTo_WritesOnlyErrors_AndReturnsNullWhenThereAreSome()
	{
		var collector = new BufferedCollector();

		Loaded(Of(Severity.Warning, "w"), Of(Severity.Error, "e")).EmitErrorsTo(collector).Should().BeNull();

		collector.Written.Select(d => d.Message).Should().Equal("e");
	}

	[Test]
	public void EmitErrorsTo_WithOnlyWarnings_WritesNothingAndReturnsTheValue()
	{
		var collector = new BufferedCollector();

		Loaded(Of(Severity.Warning, "w")).EmitErrorsTo(collector).Should().BeSameAs(Value);

		collector.Written.Should().BeEmpty();
	}

	[Test]
	public void OwnWarning_IsInFindingsAndEmitTo_ButNotEmitErrorsTo()
	{
		var result = Loaded(Of(Severity.Warning, "own warning"));
		var viaEmitTo = new BufferedCollector();
		var viaEmitErrorsTo = new BufferedCollector();

		_ = result.EmitTo(viaEmitTo);
		_ = result.EmitErrorsTo(viaEmitErrorsTo);

		result.Findings().Should().ContainSingle().Which.Message.Should().Be("own warning");
		viaEmitTo.Written.Should().ContainSingle();
		viaEmitErrorsTo.Written.Should().BeEmpty();
	}

	[Test]
	public void OrThrowIfMissing_ReturnsTheValueDespiteErrors() =>
		Loaded(Of(Severity.Error, "e")).OrThrowIfMissing().Should().BeSameAs(Value);

	[Test]
	public void OrThrowIfMissing_Unreadable_ThrowsTheLastErrorMessage()
	{
		var result = Unreadable(Of(Severity.Error, "first"), Of(Severity.Warning, "w"), Of(Severity.Error, "last"));

		var act = () => result.OrThrowIfMissing();

		act.Should().Throw<InvalidOperationException>().WithMessage("last");
	}

	[Test]
	public void OrThrowIfMissing_UnreadableWithoutErrors_NamesTheType()
	{
		var act = () => Unreadable().OrThrowIfMissing();

		act.Should().Throw<InvalidOperationException>().WithMessage("Could not load Widget");
	}

	[Test]
	public void OrThrow_WithoutErrors_ReturnsTheValue() => Loaded(Of(Severity.Warning, "w")).OrThrow().Should().BeSameAs(Value);

	[Test]
	public void OrThrow_WithErrors_ThrowsWithEveryErrorAndEveryFinding()
	{
		var result = Loaded(Of(Severity.Error, "one"), Of(Severity.Warning, "w"), Of(Severity.Error, "two"));

		var act = () => result.OrThrow();

		var thrown = act.Should().Throw<ConfigurationException>().Which;
		thrown.Message.Should().Be("one" + Environment.NewLine + "two");
		thrown.Diagnostics.Should().HaveCount(3);
	}

	[Test]
	public void OrThrow_Unreadable_ThrowsEvenWithoutErrors()
	{
		var act = () => Unreadable().OrThrow();

		act.Should().Throw<ConfigurationException>().WithMessage("Could not load Widget");
	}

	[Test]
	public void Default_HasNoCase_AndItsExtensionsThrow()
	{
		LoadResult<Widget> none = default;

		none.ValueOrNull().Should().BeNull();
		var act = () => none.Findings();
		act.Should().Throw<InvalidOperationException>();
	}

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
