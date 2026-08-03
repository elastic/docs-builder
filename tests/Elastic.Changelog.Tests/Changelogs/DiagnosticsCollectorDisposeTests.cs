// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Time.Testing;

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
		collector.OffendingFiles.Should().BeEmpty("OffendingFiles is only populated by the background reader");
		output.Items.Should().BeEmpty("IDiagnosticsOutput sinks are only invoked by the background reader");
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

	// Regression: ServiceInvoker calls StartAsync fire-and-forget from its field initializer.
	// If the service command completes before the thread pool picks up the Task.Run delegate,
	// _readerStarted is still false when WaitForDrain runs. Previously this threw; now it
	// waits briefly for the reader to start and then drains successfully.
	[Fact]
	public async Task WaitForDrain_AfterStartAsync_WaitsForReaderEvenIfNotStartedYet()
	{
		var output = new RecordingOutput();
		var collector = new DiagnosticsCollector([output]);
		IDiagnosticsCollector iface = collector;

		// StartAsync is called but we don't await — simulates the fire-and-forget in ServiceInvoker.
		_ = iface.StartAsync(CancellationToken.None);
		iface.EmitWarning(string.Empty, "should be drained");

		// WaitForDrain must not throw even though IsStarted may still be false here.
		await ShouldComplete(iface.WaitForDrain(), TimeSpan.FromSeconds(5),
			"WaitForDrain must not throw when StartAsync was called but reader hasn't started yet");

		collector.Warnings.Should().Be(1);
		output.Items.Should().HaveCount(1, "the item must be drained once the reader starts");
	}

	// A pre-canceled token makes Task.Run return a canceled task without ever executing
	// the reader delegate: start is requested but the reader never comes up, so WaitForDrain
	// must hit its 2s deadline. FakeTimeProvider advances that deadline virtually.
	[Fact]
	public async Task WaitForDrain_ReaderNeverStarts_TimesOutInVirtualTime()
	{
		var timeProvider = new FakeTimeProvider();
		var collector = new DiagnosticsCollector([], timeProvider);
		IDiagnosticsCollector iface = collector;

		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();
		_ = iface.StartAsync(cts.Token);

		var drain = iface.WaitForDrain();
		for (var i = 0; i < 100 && !drain.IsCompleted; i++)
		{
			timeProvider.Advance(TimeSpan.FromMilliseconds(500));
			await Task.Delay(10); // real delay so the awaiting continuation observes the fired timer
		}

		drain.IsCompleted.Should().BeTrue("the 2s virtual deadline must trip long before 50s of virtual time");
		Func<Task> act = () => drain;
		_ = (await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*timed out waiting for the background reader to start*");
	}
}
