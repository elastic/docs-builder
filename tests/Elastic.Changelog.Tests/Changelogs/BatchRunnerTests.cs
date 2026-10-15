// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.GitHub;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// <see cref="BatchRunner"/> runs GitHub query batches side by side. These tests use delayed, out-of-order batches
/// to check what a sequential test cannot: that batches overlap, that no more than the limit run at once, that the
/// results keep batch order, and that a failure stops the rest and leaves nothing running.
/// </summary>
public class BatchRunnerTests
{
	private static Cancel Token => TestContext.Current!.Execution.CancellationToken;

	private sealed class Probe
	{
		private int _running;
		private int _max;
		private int _started;

		public int Max => _max;
		public int Running => Volatile.Read(ref _running);
		public int Started => Volatile.Read(ref _started);

		public async Task<int> RunAsync(int batch, int delayMs, Cancel ctx)
		{
			_ = Interlocked.Increment(ref _started);
			var now = Interlocked.Increment(ref _running);
			int seen;
			while (now > (seen = Volatile.Read(ref _max)) && Interlocked.CompareExchange(ref _max, now, seen) != seen) { }

			try
			{
				await Task.Delay(delayMs, ctx);
				return batch;
			}
			finally
			{
				_ = Interlocked.Decrement(ref _running);
			}
		}
	}

	[Test]
	public async Task Run_ManyBatches_OverlapWithinTheLimitAndKeepBatchOrder()
	{
		var probe = new Probe();
		var batches = Enumerable.Range(0, 13).ToList();

		// Later batches finish first, so the order of the results does not follow the order of completion.
		var results = await BatchRunner.RunAsync(batches, (b, ctx) => probe.RunAsync(b, 60 - (b * 4), ctx), _ => false, Token);

		results.Should().NotBeNull();
		results.Should().Equal(batches);
		probe.Max.Should().BeGreaterThan(1).And.BeLessThanOrEqualTo(BatchRunner.DefaultDegree);
		probe.Running.Should().Be(0);
	}

	[Test]
	public async Task Run_ABatchFails_ReturnsNullWithoutStartingTheRestAndLeavesNothingRunning()
	{
		var probe = new Probe();
		var batches = Enumerable.Range(0, 40).ToList();

		var results = await BatchRunner.RunAsync(
			batches,
			(b, ctx) => b == 0 ? Task.FromResult(-1) : probe.RunAsync(b, 200, ctx),
			r => r < 0,
			Token
		);

		results.Should().BeNull();
		probe.Running.Should().Be(0, "the call returns only after every batch that started is done");
		probe.Started.Should().BeLessThan(39, "batches queued behind the failure never start");
	}

	[Test]
	public async Task Run_ABatchThrows_RethrowsAfterTheOthersAreDone()
	{
		var probe = new Probe();
		var batches = Enumerable.Range(0, 20).ToList();

		var act =
			async () => await BatchRunner.RunAsync(
				batches,
				(b, ctx) => b == 1 ? throw new InvalidOperationException("boom") : probe.RunAsync(b, 100, ctx),
				_ => false,
				Token
			);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
		probe.Running.Should().Be(0);
	}

	[Test]
	public async Task Run_NoBatches_ReturnsEmpty()
	{
		var results = await BatchRunner.RunAsync(Array.Empty<int>(), (b, _) => Task.FromResult(b), _ => false, Token);

		results.Should().BeEmpty();
	}
}
