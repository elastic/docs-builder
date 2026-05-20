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

	private static async Task ShouldComplete(Task task, TimeSpan timeout, string because)
	{
		using var cts = new CancellationTokenSource(timeout);
		var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
		completed.Should().BeSameAs(task, because);
		await task;
	}

	// Regression: the changelog-scrubber lambda used a DiagnosticsCollector without calling
	// StartAsync. Emitting a diagnostic and then disposing deadlocked on
	// Channel.Reader.Completion because nothing was draining the channel,
	// causing the lambda to hit its 180s timeout.
	[Fact]
	public async Task DisposeAsync_WithoutStartAsyncAfterEmit_DoesNotHang()
	{
		var output = new RecordingOutput();
		var collector = new DiagnosticsCollector([output]);
		collector.EmitWarning("file.yaml", "test warning that nobody is reading");

		await ShouldComplete(collector.DisposeAsync().AsTask(), TimeSpan.FromSeconds(5),
			"DisposeAsync must not deadlock when StartAsync was never called");

		collector.Warnings.Should().Be(1, "severity counters update regardless of reader state");
		collector.IsStarted.Should().BeFalse();
		collector.OffendingFiles.Should().BeEmpty("writes are gated when no reader will drain them");
		output.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task StopAsync_WithoutStartAsyncAfterEmit_DoesNotHang()
	{
		var output = new RecordingOutput();
		var collector = new DiagnosticsCollector([output]);
		collector.EmitError("file.yaml", "test error that nobody is reading");

		await ShouldComplete(collector.StopAsync(CancellationToken.None), TimeSpan.FromSeconds(5),
			"StopAsync must not deadlock when StartAsync was never called");

		collector.Errors.Should().Be(1);
		collector.IsStarted.Should().BeFalse();
		output.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task DisposeAsync_WithoutStartAsyncAndNoEmissions_DoesNotHang()
	{
		var collector = new DiagnosticsCollector([]);

		await ShouldComplete(collector.DisposeAsync().AsTask(), TimeSpan.FromSeconds(5),
			"Instantiate-and-dispose with no emissions must be a no-op");

		collector.IsStarted.Should().BeFalse();
		collector.Warnings.Should().Be(0);
		collector.Errors.Should().Be(0);
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
