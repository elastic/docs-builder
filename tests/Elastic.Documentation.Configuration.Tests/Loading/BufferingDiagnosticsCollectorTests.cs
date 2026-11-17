// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Tests.Loading;

public class BufferingDiagnosticsCollectorTests
{
	private static Diagnostic Of(Severity severity, string message) => new() { Severity = severity, File = "f.yml", Message = message };

	[Test]
	public void Write_KeepsEveryDiagnosticInOrder_IncludingIdenticalOnes()
	{
		var collector = new BufferingDiagnosticsCollector();

		collector.Write(Of(Severity.Warning, "a"));
		collector.Write(Of(Severity.Warning, "a"));
		collector.Write(Of(Severity.Error, "b"));

		collector.Diagnostics.Select(d => d.Message).Should().Equal("a", "a", "b");
	}

	[Test]
	public void Write_CountsSeverities_SoAParserCanAskWhetherItsOwnLoadErrored()
	{
		var collector = new BufferingDiagnosticsCollector();

		collector.Write(Of(Severity.Warning, "w"));
		collector.Write(Of(Severity.Error, "e"));
		collector.Write(Of(Severity.Hint, "h"));

		(collector.Errors, collector.Warnings, collector.Hints).Should().Be((1, 1, 1));
	}

	private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

	[Test]
	public async Task StartedThroughTheInterface_ThenStopped_EndsTheBackgroundReader()
	{
		var collector = new BufferingDiagnosticsCollector();

		// the returned task is the reader's lifetime: it ends only when StopAsync completes the channel
		var reader = ((IDiagnosticsCollector)collector).StartAsync(CancellationToken.None);
		collector.Write(Of(Severity.Warning, "w"));
		await collector.StopAsync(CancellationToken.None).WaitAsync(Limit);
		await reader.WaitAsync(Limit);

		collector.Diagnostics.Should().ContainSingle();
	}

	[Test]
	public async Task StartedWithTheConcreteMethod_ThenStopped_Completes()
	{
		var collector = new BufferingDiagnosticsCollector();

		_ = collector.StartAsync(CancellationToken.None);
		await collector.StopAsync(CancellationToken.None).WaitAsync(Limit);
	}

	[Test]
	public async Task NeverStarted_Stops_Completes()
	{
		var collector = new BufferingDiagnosticsCollector();

		collector.Write(Of(Severity.Error, "e"));
		await collector.StopAsync(CancellationToken.None).WaitAsync(Limit);

		collector.Diagnostics.Should().ContainSingle();
	}
}
