// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Severity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Configuration.Tests.Loading;

/// <summary>
/// The <see cref="Fatal"/> modes of <see cref="Loader.For{TDto, T}(Func{string, string, TDto}, TryMap{TDto, T}, Fatal, Func{Exception, string}?)"/>.
/// Input is "name:size". A name starting with "maperr" makes the map step report an error, "giveup" makes it return null,
/// "throws" makes it throw after reporting. An empty name violates an Error rule of the check (see <see cref="WidgetConstraints"/>).
/// </summary>
internal sealed class FatalModesTests
{
	internal enum Mode
	{
		All,
		CheckErrors,
		None
	}

	private const string Because = "main keeps the file; locked by this test; strict would reject it";

	private static Fatal Of(Mode mode) => mode switch
	{
		Mode.All => Fatal.All,
		Mode.CheckErrors => Fatal.CheckErrors(Because),
		_ => Fatal.None(Because)
	};

	private sealed record Dto(string Name, int Size);

	private static Dto Parse(string text, string file)
	{
		var parts = text.Split(':');
		return parts.Length == 2 && int.TryParse(parts[1], out var size)
			? new Dto(parts[0], size)
			: throw new FormatException($"{file}: malformed");
	}

	private static Widget? Map(Dto dto, MapContext context)
	{
		context.Hint("first");
		if (dto.Name.StartsWith("maperr", StringComparison.Ordinal))
		{
			context.Collector.Write(new Diagnostic
			{
				Severity = Severity.Error,
				File = "parser.yml",
				Message = "written by a parser",
				Line = 7
			});
			context.Warn("warned");
			context.Error("explicit");
		}
		if (dto.Name == "giveup")
			return null;
		if (dto.Name == "throws")
			throw new InvalidOperationException("boom");
		return new Widget(dto.Name == "maperr" ? "ok" : dto.Name, dto.Size);
	}

	private static Loader<Dto, Widget> Declared(Fatal fatal) => Loader.For(Parse, Map, fatal);

	// parse reports unknown keys to the context before a DTO exists: "name:size:extra" reports a warning and, for "bad", an error
	private static Dto ParseReporting(string text, string file, MapContext context)
	{
		context.Warn("parse warning");
		if (text.StartsWith("bad", StringComparison.Ordinal))
			context.Collector.Write(new Diagnostic { Severity = Severity.Error, File = "parser.yml", Message = "unknown key" });
		return Parse(text.Replace("bad", "ok", StringComparison.Ordinal), file);
	}

	private static IEnumerable<(string Input, string Reads)> Messages(LoadResult<Widget> result) =>
		result.Findings().Select(d => (d.Message, d.File));

	// map error only
	private const string MapError = "maperr:1";
	// check error only
	private const string CheckError = ":1";
	private const string Clean = "ok:1";

	[Test]
	[Arguments(Mode.All, MapError, false)]
	[Arguments(Mode.All, CheckError, false)]
	[Arguments(Mode.All, Clean, true)]
	[Arguments(Mode.CheckErrors, MapError, true)]
	[Arguments(Mode.CheckErrors, CheckError, false)]
	[Arguments(Mode.CheckErrors, Clean, true)]
	[Arguments(Mode.None, MapError, true)]
	[Arguments(Mode.None, CheckError, true)]
	[Arguments(Mode.None, Clean, true)]
	public void Load_Mode_DecidesWhetherTheValueIsKept(Mode mode, string input, bool kept)
	{
		var result = Declared(Of(mode)).Load(input, "w.yml");

		(result.Value is Loaded<Widget>).Should().Be(kept);
		result.ValueOrNull().Should().Match<Widget?>(w => (w != null) == kept);
	}

	[Test]
	public void Load_NoneKeepsTheValueAndItsConstraintError_CheckErrorsDiscardsTheValueButKeepsTheError()
	{
		var none = Declared(Of(Mode.None)).Load(CheckError, "w.yml");
		var checkErrors = Declared(Of(Mode.CheckErrors)).Load(CheckError, "w.yml");

		none.Value.Should().BeOfType<Loaded<Widget>>();
		none.Findings().Should().ContainSingle(d => d.Severity == Severity.Error);
		checkErrors.Value.Should().BeOfType<Unreadable<Widget>>();
		checkErrors.Findings().Should().ContainSingle(d => d.Severity == Severity.Error);
	}

	[Test]
	[Arguments(Mode.All)]
	[Arguments(Mode.CheckErrors)]
	[Arguments(Mode.None)]
	public void Load_KeptValueWithErrors_StillCarriesEveryError(Mode mode)
	{
		var result = Declared(Of(mode)).Load(MapError, "w.yml");

		result.Findings().Count(d => d.Severity == Severity.Error).Should().Be(2);
	}

	[Test]
	public void Load_FindingsOrder_IsMapWritesAndContextCallsInterleaved_ThenTheCheck()
	{
		var result = Declared(Of(Mode.None)).Load("maperr:50", "w.yml");

		result
			.Findings()
			.Select(d => (d.Severity, d.File, d.Message))
			.Should()
			.Equal(
				(Severity.Hint, "w.yml", "first"),
				(Severity.Error, "parser.yml", "written by a parser"),
				(Severity.Warning, "w.yml", "warned"),
				(Severity.Error, "w.yml", "explicit"),
				(Severity.Warning, "w.yml", "Widget is large.")
			);
	}

	[Test]
	public void Load_CollectorWrite_KeepsTheDiagnosticUntouched()
	{
		var result = Declared(Of(Mode.None)).Load(MapError, "w.yml");

		var written = result.Findings().Single(d => d.Message == "written by a parser");
		(written.File, written.Line, written.Severity).Should().Be(("parser.yml", 7, Severity.Error));
	}

	[Test]
	[Arguments(Mode.All)]
	[Arguments(Mode.CheckErrors)]
	[Arguments(Mode.None)]
	public void Load_MapReturnsNull_IsUnreadableWithTheFindingsSoFar_AndNoCheck(Mode mode)
	{
		var result = Declared(Of(mode)).Load("giveup:1", "w.yml");

		result.Value.Should().BeOfType<Unreadable<Widget>>();
		result.Findings().Select(d => d.Message).Should().Equal("first");
	}

	[Test]
	[Arguments(Mode.All)]
	[Arguments(Mode.CheckErrors)]
	[Arguments(Mode.None)]
	public void Load_MapThrowsAfterFindings_IsUnreadableWithTheFindingsThenTheDescribedError(Mode mode)
	{
		var result = Declared(Of(mode)).Load("throws:1", "w.yml");

		result.Value.Should().BeOfType<Unreadable<Widget>>();
		result.Findings().Select(d => (d.Severity, d.Message)).Should().Equal((Severity.Hint, "first"), (Severity.Error, "boom"));
	}

	[Test]
	[Arguments(Mode.All)]
	[Arguments(Mode.CheckErrors)]
	[Arguments(Mode.None)]
	public void Load_ParseThrows_IsUnreadableInEveryMode(Mode mode) =>
		Declared(Of(mode)).Load("nonsense", "w.yml").Findings().Should().ContainSingle().Which.Message.Should().Be("w.yml: malformed");

	private static IEnumerable<string> Outcome(LoadResult<Widget> result) =>
		[
			result.Value is Loaded<Widget> ? "loaded" : "unreadable",
			result.ValueOrNull()?.ToString() ?? "-",
			.. result.Findings().Select(d => $"{d.Severity}|{d.File}|{d.Message}|{d.RuleId}")
		];

	[Test]
	[Arguments("ok:1")]
	[Arguments("ok:50")]
	[Arguments(":1")]
	[Arguments(":50")]
	[Arguments("legacy:1")]
	[Arguments("nonsense")]
	public void Load_TheBoolOverload_IsFatalAllOrNone_Exactly(string input)
	{
		static Widget Strict(Dto dto, MapContext context)
		{
			context.Warn("w");
			return new Widget(dto.Name, dto.Size);
		}

		var viaBool = (Loader.For(Parse, Strict), Loader.For(Parse, Strict, errorsAreFatal: false));
		var viaMode =
			(Loader.For<Dto, Widget>(Parse, Strict, Fatal.All), Loader.For<Dto, Widget>(Parse, Strict, Fatal.None("legacy twin")));

		Outcome(viaBool.Item1.Load(input, "w.yml")).Should().Equal(Outcome(viaMode.Item1.Load(input, "w.yml")));
		Outcome(viaBool.Item2.Load(input, "w.yml")).Should().Equal(Outcome(viaMode.Item2.Load(input, "w.yml")));
	}

	[Test]
	public void Collector_CountsOnlyThisContextsFindings_ByWriteAndByContextCalls()
	{
		var context = new MapContext("a.yml");
		var other = new MapContext("b.yml");

		context.Collector.Write(new Diagnostic { Severity = Severity.Warning, File = "x", Message = "w" });
		context.Error("e");
		context.Hint("h");
		other.Warn("elsewhere");

		(context.Collector.Errors, context.Collector.Warnings, context.Collector.Hints).Should().Be((1, 1, 1));
		(other.Collector.Errors, other.Collector.Warnings).Should().Be((0, 1));
		context.Findings.Select(d => d.Message).Should().Equal("w", "e", "h");
	}

	[Test]
	public void Collector_ErrorHelpers_ReadAsAParserWouldUseThem()
	{
		var context = new MapContext("a.yml");

		context.Collector.EmitError("a.yml", "bad key");
		context.Collector.EmitWarning("a.yml", "odd key");

		context.Findings.Select(d => (d.Severity, d.Message)).Should().Equal((Severity.Error, "bad key"), (Severity.Warning, "odd key"));
	}

	[Test]
	public void Error_RecordsAnErrorAtTheFileWithItsPosition()
	{
		var context = new MapContext("a.yml");

		context.Error("e", line: 2, column: 5);

		context
			.Findings
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Match<Diagnostic>(d => d.Severity == Severity.Error && d.File == "a.yml" && d.Line == 2 && d.Column == 5);
	}

	[Test]
	[Arguments(Mode.All, false)]
	[Arguments(Mode.CheckErrors, true)]
	[Arguments(Mode.None, true)]
	public void Load_ParseWithContext_ReportsBeforeMapAndTheCheck_AndFollowsTheMode(Mode mode, bool kept)
	{
		var result = Loader.For<Dto, Widget>(ParseReporting, Map, Of(mode)).Load("bad:50", "w.yml");

		(result.Value is Loaded<Widget>).Should().Be(kept);
		result
			.Findings()
			.Select(d => (d.Severity, d.Message))
			.Should()
			.Equal(
				(Severity.Warning, "parse warning"),
				(Severity.Error, "unknown key"),
				(Severity.Hint, "first"),
				(Severity.Warning, "Widget is large.")
			);
	}

	[Test]
	public void Load_ParseWithContextThrows_KeepsItsFindingsThenTheDescribedError()
	{
		static Dto Throwing(string text, string file, MapContext context)
		{
			context.Warn("before");
			throw new InvalidOperationException("boom");
		}

		var result = Loader.For<Dto, Widget>(Throwing, Map, Fatal.None(Because), e => $"described: {e.Message}").Load("ok:1", "w.yml");

		result.Value.Should().BeOfType<Unreadable<Widget>>();
		result
			.Findings()
			.Select(d => (d.Severity, d.Message))
			.Should()
			.Equal((Severity.Warning, "before"), (Severity.Error, "described: boom"));
	}

	[Test]
	[Arguments("")]
	[Arguments("   ")]
	[Arguments(null)]
	public void TolerantFactories_BlankBecause_Throw(string? because)
	{
		var checkErrors = () => Fatal.CheckErrors(because!);
		var none = () => Fatal.None(because!);

		checkErrors.Should().Throw<ArgumentException>().WithParameterName("because");
		none.Should().Throw<ArgumentException>().WithParameterName("because");
	}

	[Test]
	public void Because_RoundTripsAndShowsInToString()
	{
		var checkErrors = Fatal.CheckErrors("why one");
		var none = Fatal.None("why two");

		(checkErrors.Because, none.Because, Fatal.All.Because).Should().Be(("why one", "why two", null));
		checkErrors.ToString().Should().Be("Fatal.CheckErrors(because: \"why one\")");
		none.ToString().Should().Be("Fatal.None(because: \"why two\")");
		Fatal.All.ToString().Should().Be("Fatal.All");
	}

	[Test]
	public void LegacyBoolOverload_IsNoneWithARecordedLegacyReason()
	{
		Loader
			.For(Parse, (dto, _) => new Widget(dto.Name, dto.Size), errorsAreFatal: false)
			.Load(CheckError, "w.yml")
			.Value
			.Should()
			.BeOfType<Loaded<Widget>>();
		Fatal.FromLegacyBool(false).Because.Should().Be("legacy errorsAreFatal: false (no justification recorded)");
		Fatal.FromLegacyBool(true).Should().Be(Fatal.All);
	}

	[Test]
	public void LegacyMode_IsRejectedByTheOverloadsThatTakeAFatal()
	{
		var legacy = Fatal.FromLegacyBool(false);
		var withTryMap = () => Loader.For(Parse, Map, legacy);
		var withTryParse = () => Loader.For<Dto, Widget>(ParseReporting, Map, legacy);

		withTryMap.Should().Throw<ArgumentException>().WithParameterName("fatal");
		withTryParse.Should().Throw<ArgumentException>().WithParameterName("fatal");
	}

	[Test]
	public void Factories_AreValueEqualOnModeAndBecause()
	{
		Fatal.None("a").Should().Be(Fatal.None("a"));
		Fatal.None("a").Should().NotBe(Fatal.None("b"));
		Fatal.None("a").Should().NotBe(Fatal.CheckErrors("a"));
		Fatal.All.Should().Be(default(Fatal));
	}
}
