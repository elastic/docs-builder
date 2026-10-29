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
		private int _cancelled;

		public int Max => _max;
		public int Running => Volatile.Read(ref _running);
		public int Started => Volatile.Read(ref _started);
		public int Cancelled => Volatile.Read(ref _cancelled);

		public async Task<int> RunAsync(int batch, int delayMs, Cancel ctx)
		{
			_ = Interlocked.Increment(ref _started);
			var now = Interlocked.Increment(ref _running);
			int seen;
			while (now > (seen = Volatile.Read(ref _max)))
			{
				if (Interlocked.CompareExchange(ref _max, now, seen) == seen)
					break;
			}

			try
			{
				await Task.Delay(delayMs, ctx);
				return batch;
			}
			catch (OperationCanceledException)
			{
				_ = Interlocked.Increment(ref _cancelled);
				throw;
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
	public async Task Run_ABatchFailsLate_RunningSiblingsObserveCancellation()
	{
		var probe = new Probe();

		// Batch 0 fails after the siblings have started. They would sleep for ten seconds if nothing cancelled them.
		var results = await BatchRunner.RunAsync(
			Enumerable.Range(0, 4).ToList(),
			async (b, ctx) =>
			{
				if (b != 0)
					return await probe.RunAsync(b, 10_000, ctx);

				await Task.Delay(100, ctx);
				return -1;
			},
			r => r < 0,
			Token
		);

		results.Should().BeNull();
		probe.Cancelled.Should().Be(3, "every running sibling gets the cancelled token, not the caller's token");
	}

	[Test]
	public async Task Run_ABatchThrowsLate_RunningSiblingsObserveCancellation()
	{
		var probe = new Probe();

		var act =
			async () => await BatchRunner.RunAsync(
				Enumerable.Range(0, 4).ToList(),
				async (b, ctx) =>
				{
					if (b != 0)
						return await probe.RunAsync(b, 10_000, ctx);

					await Task.Delay(100, ctx);
					throw new InvalidOperationException("boom");
				},
				_ => false,
				Token
			);

		_ = await act.Should().ThrowAsync<InvalidOperationException>();
		probe.Cancelled.Should().Be(3);
	}

	[Test]
	public async Task Run_ABatchTimesOutOnItsOwn_IsRethrownAndStopsTheRest()
	{
		var probe = new Probe();

		// An HttpClient timeout throws a cancellation that no token asked for.
		var act =
			async () => await BatchRunner.RunAsync(
				Enumerable.Range(0, 4).ToList(),
				async (b, ctx) =>
				{
					if (b != 0)
						return await probe.RunAsync(b, 10_000, ctx);

					await Task.Delay(100, ctx);
					throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.");
				},
				_ => false,
				Token
			);

		_ = await act.Should().ThrowAsync<TaskCanceledException>().WithMessage("*Timeout*");
		probe.Cancelled.Should().Be(3);
		probe.Running.Should().Be(0);
	}

	[Test]
	public async Task Run_TheCallerCancels_StopsEveryBatchAndThrows()
	{
		var probe = new Probe();
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
		cts.CancelAfter(100);

		var act =
			async () => await BatchRunner.RunAsync(
				Enumerable.Range(0, 8).ToList(),
				(b, ctx) => probe.RunAsync(b, 10_000, ctx),
				_ => false,
				cts.Token
			);

		_ = await act.Should().ThrowAsync<OperationCanceledException>();
		probe.Running.Should().Be(0);
	}

	[Test]
	public async Task Run_NoBatches_ReturnsEmpty()
	{
		var results = await BatchRunner.RunAsync(Array.Empty<int>(), (b, _) => Task.FromResult(b), _ => false, Token);

		results.Should().BeEmpty();
	}
}
