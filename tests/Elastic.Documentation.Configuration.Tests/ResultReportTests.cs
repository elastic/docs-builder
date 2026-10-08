// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Tests;

public class ResultReportTests
{
	private sealed class RecordingCollector() : DiagnosticsCollector([])
	{
		public List<Diagnostic> Seen { get; } = [];

		public override void Write(Diagnostic diagnostic) => Seen.Add(diagnostic);

		public override DiagnosticsCollector StartAsync(Cancel ctx) => this;

		public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
	}

	private static Diagnostic Of(Severity severity, string message) => new() { Severity = severity, File = "a.yml", Message = message };

	[Test]
	public void ReportWritesEveryDiagnosticInOrderAndReturnsTheResultItself()
	{
		var collector = new RecordingCollector();
		var result = Result.Success("value", [Of(Severity.Warning, "first"), Of(Severity.Hint, "second")]);

		var returned = result.Report(collector);

		returned.Should().BeSameAs(result);
		collector.Seen.Select(d => d.Message).Should().Equal("first", "second");
	}

	[Test]
	public void ReportThenOrThrowReportsBeforeThrowing()
	{
		var collector = new RecordingCollector();
		var result = Result.Failure<string>([Of(Severity.Error, "boom")]);

		var act = () => result.Report(collector).OrThrow();

		act.Should().Throw<ConfigurationException>();
		collector.Seen.Should().ContainSingle().Which.Message.Should().Be("boom");
	}

	[Test]
	public void ReportThenOrThrowReturnsTheValueWhenThereAreOnlyWarnings()
	{
		var collector = new RecordingCollector();

		var value = Result.Success("value", [Of(Severity.Warning, "careful")]).Report(collector).OrThrow();

		value.Should().Be("value");
		collector.Seen.Should().HaveCount(1);
	}

	[Test]
	public void EmitToIsReportThenValue()
	{
		var collector = new RecordingCollector();

		Result.Success("value", [Of(Severity.Warning, "w")]).EmitTo(collector).Should().Be("value");
		Result.Failure<string>([Of(Severity.Error, "e")]).EmitTo(collector).Should().BeNull();
		collector.Seen.Should().HaveCount(2);
	}
}
