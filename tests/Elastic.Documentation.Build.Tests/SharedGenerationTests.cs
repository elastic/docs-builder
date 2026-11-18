// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Documentation.Builder.Http;

namespace Elastic.Documentation.Build.Tests;

public class SharedGenerationTests
{
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	/// <summary>A generation the test controls: it starts, waits at a gate, and marks the output fresh like the real one.</summary>
	private sealed class FakeGeneration
	{
		private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _runs;

		public bool Stale { get; set; } = true;
		public int Runs => Volatile.Read(ref _runs);
		public bool TokenCancelled { get; private set; }

		/// <summary>Runs after the gate opens, before the run returns. Use it to simulate a change or a failure.</summary>
		public Action<int>? AtEnd { get; set; }

		public Task Started => _started.Task.WaitAsync(Patience);
		public void Release() => _gate.TrySetResult();

		public async Task GenerateAsync(Cancel ctx)
		{
			var run = Interlocked.Increment(ref _runs);
			Stale = false;
			_ = _started.TrySetResult();
			try
			{
				await _gate.Task.WaitAsync(ctx);
			}
			catch (OperationCanceledException)
			{
				TokenCancelled = true;
				throw;
			}
			AtEnd?.Invoke(run);
		}
	}

	[Test]
	public async Task EnsureAsync_RequestCancelledMidRun_LetsTheRunFinishAndTheNextRequestJoinsIt(CancellationToken cancellationToken)
	{
		var fake = new FakeGeneration();
		using var shared = new SharedGeneration(fake.GenerateAsync, () => fake.Stale);
		using var firstRequest = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		var first = shared.EnsureAsync(firstRequest.Token);
		await fake.Started;
		await firstRequest.CancelAsync();
		var cancelled = async () => await first;
		await cancelled.Should().ThrowAsync<OperationCanceledException>();

		var second = shared.EnsureAsync(cancellationToken);
		fake.Release();
		await second.WaitAsync(Patience, cancellationToken);

		fake.Runs.Should().Be(1);
		fake.TokenCancelled.Should().BeFalse();
	}

	[Test]
	public async Task EnsureAsync_ConcurrentRequests_ShareOneRun(CancellationToken cancellationToken)
	{
		var fake = new FakeGeneration();
		using var shared = new SharedGeneration(fake.GenerateAsync, () => fake.Stale);

		var requests = Enumerable.Range(0, 3).Select(_ => shared.EnsureAsync(cancellationToken)).ToArray();
		await fake.Started;
		fake.Release();
		await Task.WhenAll(requests).WaitAsync(Patience, cancellationToken);

		fake.Runs.Should().Be(1);
	}

	[Test]
	public async Task EnsureAsync_NothingStale_DoesNotStartARun(CancellationToken cancellationToken)
	{
		var fake = new FakeGeneration { Stale = false };
		using var shared = new SharedGeneration(fake.GenerateAsync, () => fake.Stale);

		await shared.EnsureAsync(cancellationToken).WaitAsync(Patience, cancellationToken);

		fake.Runs.Should().Be(0);
	}

	[Test]
	public async Task EnsureAsync_ChangeLandsDuringRun_RunsAgainBeforeReturning(CancellationToken cancellationToken)
	{
		var fake = new FakeGeneration();
		fake.AtEnd = run => fake.Stale = run == 1;
		using var shared = new SharedGeneration(fake.GenerateAsync, () => fake.Stale);

		var request = shared.EnsureAsync(cancellationToken);
		await fake.Started;
		fake.Release();
		await request.WaitAsync(Patience, cancellationToken);

		fake.Runs.Should().Be(2);
		fake.Stale.Should().BeFalse();
	}

	[Test]
	public async Task EnsureAsync_RunFails_TheNextRequestRetriesEvenWhenNothingIsMarkedStale(CancellationToken cancellationToken)
	{
		var fake = new FakeGeneration
		{
			AtEnd = run =>
			{
				if (run == 1)
					throw new IOException("disk");
			}
		};
		using var shared = new SharedGeneration(fake.GenerateAsync, () => fake.Stale);

		var failing = shared.EnsureAsync(cancellationToken);
		await fake.Started;
		fake.Release();
		var failed = async () => await failing.WaitAsync(Patience, cancellationToken);
		await failed.Should().ThrowAsync<IOException>();

		await shared.EnsureAsync(cancellationToken).WaitAsync(Patience, cancellationToken);

		fake.Runs.Should().Be(2);
	}

	[Test]
	public async Task Dispose_RunInFlight_CancelsTheRun(CancellationToken cancellationToken)
	{
		var fake = new FakeGeneration();
		var shared = new SharedGeneration(fake.GenerateAsync, () => fake.Stale);

		var request = shared.EnsureAsync(cancellationToken);
		await fake.Started;
		shared.Dispose();
		var cancelled = async () => await request.WaitAsync(Patience, cancellationToken);

		await cancelled.Should().ThrowAsync<OperationCanceledException>();
		fake.TokenCancelled.Should().BeTrue();
	}
}
