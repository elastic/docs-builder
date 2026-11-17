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

	[Test]
	public async Task StartAndStop_NeedNoBackgroundWork()
	{
		var collector = new BufferingDiagnosticsCollector();

		collector.StartAsync(CancellationToken.None).Should().BeSameAs(collector);
		await collector.StopAsync(CancellationToken.None);
	}
}
