// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

public class DiagnosticsCollectorDisposeTests
{
	private sealed class RecordingOutput : IDiagnosticsOutput
	{
		public List<Diagnostic> Items { get; } = [];
		public void Write(Diagnostic diagnostic) => Items.Add(diagnostic);
	}

	// Regression: the changelog-scrubber lambda used a DiagnosticsCollector without calling
	// StartAsync. Emitting a diagnostic and then disposing deadlocked on
	// Channel.Reader.Completion because nothing was draining the channel,
	// causing the lambda to hit its 180s timeout.
	[Fact]
	public async Task DisposeAsync_WithoutStartAsync_DoesNotHang()
	{
		var output = new RecordingOutput();
		var collector = new DiagnosticsCollector([output]);
		collector.EmitWarning("file.yaml", "test warning that nobody is reading");

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var disposeTask = collector.DisposeAsync().AsTask();
		var completed = await Task.WhenAny(disposeTask, Task.Delay(Timeout.Infinite, cts.Token));

		completed.Should().BeSameAs(disposeTask, "DisposeAsync must not deadlock when StartAsync was never called");
		await disposeTask;
		collector.Warnings.Should().Be(1);
		collector.OffendingFiles.Should().Contain("file.yaml", "Dispose must drain queued diagnostics so outputs and OffendingFiles still observe them");
		output.Items.Should().HaveCount(1);
	}

	[Fact]
	public async Task StopAsync_WithoutStartAsync_DoesNotHang()
	{
		var output = new RecordingOutput();
		var collector = new DiagnosticsCollector([output]);
		collector.EmitError("file.yaml", "test error that nobody is reading");

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var stopTask = collector.StopAsync(CancellationToken.None);
		var completed = await Task.WhenAny(stopTask, Task.Delay(Timeout.Infinite, cts.Token));

		completed.Should().BeSameAs(stopTask, "StopAsync must not deadlock when StartAsync was never called");
		await stopTask;
		collector.Errors.Should().Be(1);
		output.Items.Should().HaveCount(1, "Stop must drain queued diagnostics to outputs even without a background reader");
	}

	[Fact]
	public async Task WaitForDrain_WithoutStartAsync_ThrowsImmediately()
	{
		var collector = new DiagnosticsCollector([]);
		collector.EmitWarning(string.Empty, "queued");

		Func<Task> act = () => ((IDiagnosticsCollector)collector).WaitForDrain();
		_ = await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
