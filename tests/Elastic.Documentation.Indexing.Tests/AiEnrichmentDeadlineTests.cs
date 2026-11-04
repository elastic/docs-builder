// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Indexing.Tests;

public class AiEnrichmentDeadlineTests
{
	[Test]
	public void Create_NoWallClock_ReturnsOriginalToken()
	{
		using var cts = new CancellationTokenSource();

		using var deadline = AiEnrichmentDeadline.Create(null, cts.Token);

		deadline.Token.Should().Be(cts.Token);
		deadline.TimedOut.Should().BeFalse();
	}

	[Test]
	public void Create_AmbientTokenCancelled_LinkedTokenCancelledButNotTimedOut()
	{
		using var cts = new CancellationTokenSource();

		using var deadline = AiEnrichmentDeadline.Create(TimeSpan.FromMinutes(5), cts.Token);
		cts.Cancel();

		deadline.Token.IsCancellationRequested.Should().BeTrue();
		deadline.TimedOut.Should().BeFalse();
	}

	[Test]
	public async Task Create_WallClockElapses_LinkedTokenCancelledAndTimedOut()
	{
		using var deadline = AiEnrichmentDeadline.Create(TimeSpan.FromMilliseconds(1), CancellationToken.None);

		// Wait for the timer to fire instead of sleeping a fixed time: a loaded CI runner can delay a 1 ms timer
		// past any short sleep. Registering on an already cancelled token runs the callback immediately.
		var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = deadline.Token.Register(() => fired.TrySetResult());
		await fired.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current!.Execution.CancellationToken);

		deadline.Token.IsCancellationRequested.Should().BeTrue();
		deadline.TimedOut.Should().BeTrue();
	}
}
