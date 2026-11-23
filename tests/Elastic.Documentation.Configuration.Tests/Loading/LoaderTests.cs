// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Nullean.Entities;
using Severity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Configuration.Tests.Loading;

internal sealed record Widget(string Name, int Size) : IHaveConstraints<Widget>
{
	public static ConstraintSet<Widget> Constraints => WidgetConstraints.All;
}

[Constraints<Widget>]
internal static partial class WidgetConstraints
{
	public static readonly Constraint<Widget> NameRequired = Constraints<Widget>.Always(
		w => !string.IsNullOrEmpty(w.Name),
		"Widget has no name."
	);

	public static readonly Constraint<Widget> SizeSmall = Constraints<Widget>.Always(w => w.Size <= 10, "Widget is large.").WithSeverity(
		Nullean.Entities.Severity.Warning
	);
}

public class LoaderTests
{
	private sealed record Dto(string Name, int Size);

	// "name:size", e.g. "ok:1"; anything else is malformed
	private static Dto Parse(string text, string file)
	{
		var parts = text.Split(':');
		return parts.Length == 2 && int.TryParse(parts[1], out var size)
			? new Dto(parts[0], size)
			: throw new FormatException($"{file}: malformed");
	}

	private static Widget Map(Dto dto, MapContext context)
	{
		if (dto.Name == "legacy")
			context.Warn("legacy name", line: 1);
		return new Widget(dto.Name, dto.Size);
	}

	private static readonly Loader<Dto, Widget> Declared = Loader.For(Parse, Map);

	private static Diagnostic Error(string message) => new() { Severity = Severity.Error, File = "w.yml", Message = message };

	[Test]
	public void Load_ValidInput_ReturnsTheMappedRecord()
	{
		var result = Declared.Load("ok:1", "w.yml");

		result.ValueOrNull().Should().Be(new Widget("ok", 1));
		result.Value.Should().BeOfType<Loaded<Widget>>();
		result.Findings().Should().BeEmpty();
	}

	[Test]
	public void Load_WarningOnly_KeepsTheValueAndTheWarning()
	{
		var result = Declared.Load("ok:50", "w.yml");

		result.HasErrors().Should().BeFalse();
		result.ValueOrNull().Should().NotBeNull();
		result.Findings().Should().ContainSingle().Which.RuleId.Should().Be("size-small");
	}

	[Test]
	public void Load_ErrorIsFatalByDefault_IsUnreadableWithEveryFinding()
	{
		var result = Declared.Load(":50", "w.yml");

		result.Value.Should().BeOfType<Unreadable<Widget>>();
		result.Findings().Select(d => d.RuleId).Should().Equal("name-required", "size-small");
	}

	[Test]
	public void Load_ErrorsNotFatal_IsLoadedWithItsErrors()
	{
		var tolerant = Loader.For(Parse, Map, errorsAreFatal: false);

		var result = tolerant.Load(":1", "w.yml");

		result.Value.Should().BeOfType<Loaded<Widget>>();
		result.HasErrors().Should().BeTrue();
	}

	[Test]
	public void Load_MapFindings_ComeBeforeTheViolations()
	{
		var result = Declared.Load("legacy:50", "w.yml");

		result
			.Findings()
			.Select(d => (d.Severity, d.Message))
			.Should()
			.Equal((Severity.Warning, "legacy name"), (Severity.Warning, "Widget is large."));
	}

	[Test]
	public void Load_ParseThrows_IsUnreadableWithTheMessage()
	{
		var result = Declared.Load("nonsense", "w.yml");

		result.Value.Should().BeOfType<Unreadable<Widget>>();
		var finding = result.Findings().Should().ContainSingle().Which;
		(finding.Severity, finding.File, finding.Message).Should().Be((Severity.Error, "w.yml", "w.yml: malformed"));
	}

	[Test]
	public void Load_MapAddsAFindingThenThrows_KeepsTheFindingBeforeTheError()
	{
		var loader = Loader.For<Dto, Widget>(Parse, (_, context) =>
		{
			context.Hint("found while mapping");
			throw new InvalidOperationException("cannot map");
		});

		var result = loader.Load("a:1", "w.yml");

		result
			.Findings()
			.Select(d => (d.Severity, d.Message))
			.Should()
			.Equal((Severity.Hint, "found while mapping"), (Severity.Error, "cannot map"));
	}

	[Test]
	public void Load_Describe_ShapesTheTextOfTheErrorOnly()
	{
		var loader = Loader.For<Dto, Widget>(
			(_, _) => throw new InvalidOperationException("outer", new FormatException("inner")),
			Map,
			describe: e => $"{e.Message} / {e.InnerException?.Message}"
		);

		loader.Load("x", "w.yml").Findings().Should().ContainSingle().Which.Message.Should().Be("outer / inner");
	}

	[Test]
	public void Load_Cancellation_Propagates()
	{
		var loader = Loader.For<Dto, Widget>((_, _) => throw new OperationCanceledException(), Map);

		var act = () => loader.Load("x", "w.yml");

		act.Should().Throw<OperationCanceledException>();
	}

	[Test]
	public void Check_ReportsTheRecordsOwnConstraints_AndLeavesFatalityToTheCaller()
	{
		var result = new Widget("", 50).Check("w.yml");

		result.Value.Should().NotBeNull();
		result.Findings.Select(d => d.RuleId).Should().Equal("name-required", "size-small");
	}
}
