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

	/// <summary>
	/// Regression: when StartAsync is never called (e.g. a [NoOptionsInjection] command that
	/// bypasses CatchExceptionMiddleware), StopAsync used to return immediately without draining
	/// the channel. Items reached HandleItem only if the caller remembered to call StartAsync;
	/// forgetting it silently swallowed every error message while the count stayed correct.
	/// </summary>
	[Test]
	public async Task StopAsync_WithoutStartAsync_DrainsSynchronouslyBeforeReturning()
	{
		var output = new CapturingOutput();
		var collector = new DiagnosticsCollector([output]);

		// Emit errors without ever calling StartAsync — simulates a [NoOptionsInjection] command.
		collector.EmitError("", "first error");
		collector.EmitError("", "second error");

		await collector.StopAsync(CancellationToken.None);

		// Count was always correct; the fix is that output now also receives the items.
		collector.Errors.Should().Be(2);
		output.Received.Should().Be(2);
	}
}
