// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Diagnostics;

/// <summary>
/// Regression tests for the count-before-write invariant in <see cref="DiagnosticsCollector.Write"/>.
/// The severity count must always move, even when the channel is completed and the item body is dropped
/// by <see cref="DiagnosticsChannel.Write"/> (TryWrite on a completed channel). This is the property
/// that keeps <see cref="IDiagnosticsCollector.Errors"/> authoritative for exit-code decisions.
/// </summary>
public class DiagnosticsCollectorTests
{
	private sealed class CapturingOutput : IDiagnosticsOutput
	{
		public int Received { get; private set; }

		public void Write(Diagnostic diagnostic) => Received++;
	}

	[Test]
	public void Write_AfterChannelCompleted_StillIncrementsErrors()
	{
		var output = new CapturingOutput();
		var collector = new DiagnosticsCollector([output]);

		// Simulate what CatchExceptionMiddleware would have seen before the fix: channel completed
		// (e.g. by ServiceInvoker disposal during unwind) before the error was emitted.
		collector.Channel.TryComplete();
		collector.EmitError("file.md", "something went wrong");

		// Count moves even though the body was dropped by TryWrite.
		collector.Errors.Should().Be(1);
		// The body was NOT delivered to outputs — it was dropped by the completed channel.
		output.Received.Should().Be(0);
	}

	[Test]
	public async Task Write_AfterDisposeAsync_StillIncrementsErrors()
	{
		var output = new CapturingOutput();
		var collector = new DiagnosticsCollector([output]);

		// DisposeAsync is the lifecycle path taken when ServiceInvoker's await using fires
		// during exception unwind. After this, the channel is completed.
		await collector.DisposeAsync();
		collector.EmitError("file.md", "something went wrong");

		collector.Errors.Should().Be(1);
		output.Received.Should().Be(0);
	}

	[Test]
	public void Write_MultipleCallsAfterChannelCompleted_AccumulatesCount()
	{
		var collector = new DiagnosticsCollector([]);

		collector.Channel.TryComplete();

		collector.EmitError("f.md", "error 1");
		collector.EmitWarning("f.md", "warning 1");
		collector.EmitError("f.md", "error 2");

		collector.Errors.Should().Be(2);
		collector.Warnings.Should().Be(1);
	}

	private static Diagnostic Ruled(
		string? ruleId = "rule-a",
		string file = "f.md",
		int? line = 3,
		int? column = 1,
		Severity severity = Severity.Warning,
		string message = "m"
	) =>
		new()
		{
			RuleId = ruleId,
			File = file,
			Line = line,
			Column = column,
			Severity = severity,
			Message = message
		};

	[Test]
	public void IdenticalRuleDiagnostic_IsDroppedAndNotCounted()
	{
		var collector = new DiagnosticsCollector([]);
		collector.Write(Ruled());
		collector.Write(Ruled());
		collector.Write(Ruled(severity: Severity.Error));
		collector.Write(Ruled(severity: Severity.Error));
		collector.Write(Ruled(severity: Severity.Hint));
		collector.Write(Ruled(severity: Severity.Hint));

		collector.Warnings.Should().Be(1);
		collector.Errors.Should().Be(1);
		collector.Hints.Should().Be(1);
		collector.DuplicatesDropped.Should().HaveCount(3);
		var queued = 0;
		while (collector.Channel.Reader.TryRead(out _))
			queued++;
		queued.Should().Be(3);
	}

	[Test]
	public void RuleDiagnostics_DifferingInAnyField_AreKept()
	{
		var collector = new DiagnosticsCollector([]);
		collector.Write(Ruled());
		collector.Write(Ruled(line: 4));
		collector.Write(Ruled(column: 2));
		collector.Write(Ruled(file: "g.md"));
		collector.Write(Ruled(message: "other"));
		collector.Write(Ruled(ruleId: "rule-b"));
		collector.Write(Ruled(line: null));

		collector.Warnings.Should().Be(7);
		collector.DuplicatesDropped.Should().BeEmpty();
	}

	[Test]
	public void DiagnosticWithoutRuleId_IsNeverDeduplicated()
	{
		var collector = new DiagnosticsCollector([]);
		collector.Write(Ruled(ruleId: null));
		collector.Write(Ruled(ruleId: null));
		collector.Write(Ruled(ruleId: ""));
		collector.Write(Ruled(ruleId: ""));
		collector.EmitWarning("f.md", "same");
		collector.EmitWarning("f.md", "same");

		collector.Warnings.Should().Be(6);
		collector.DuplicatesDropped.Should().BeEmpty();
	}

	[Test]
	public async Task ConcurrentIdenticalWrites_AdmitExactlyOne()
	{
		var collector = new DiagnosticsCollector([]);
		await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
		{
			for (var i = 0; i < 50; i++)
				collector.Write(Ruled(line: i));
		})));

		collector.Warnings.Should().Be(50);
		collector.DuplicatesDropped.Should().HaveCount(64 * 50 - 50);
	}
}
