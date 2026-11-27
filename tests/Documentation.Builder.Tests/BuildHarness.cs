// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Builder.Http;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Entities;
using DiagnosticSeverity = Elastic.Documentation.Diagnostics.Severity;

namespace Documentation.Builder.Tests;

/// <summary>What the fake build does when it runs: how many diagnostics it reports and how long it takes (on the run's virtual clock).</summary>
internal readonly record struct BuildPlan(int Diagnostics, TimeSpan Duration);

/// <summary>
/// The service under test, as a test drives it: the real <see cref="InMemoryBuildState"/> and its real build loop, with the isolated build replaced by a
/// fake that reports diagnostics and spends virtual time. Every step awaits the events the service broadcasts until it is at rest again, so a run
/// is deterministic and holds no thread; a subscribed client records what the service broadcasts, stamped by the same clock.
/// </summary>
internal sealed class BuildHarness : IDisposable
{
	private readonly ManualTimeProvider _time;
	private readonly InMemoryBuildState _state;
	private CancellationTokenSource _shutdown = new();
	private Task _loop;
	private readonly Task _client;
	private readonly Lock _gate = new();
	private readonly List<Timestamped<BuildEvent>> _log = [];
	private int _started;
	private int _ended;
	private BuildPlan _plan;
	private TaskCompletionSource? _hold;
	private TaskCompletionSource _changed = NewSignal();
	private readonly CancellationTokenSource _lifetime = new();

	public BuildHarness(TimeProvider time)
	{
		_time = (ManualTimeProvider)time;
		_state = new InMemoryBuildState(NullLoggerFactory.Instance, null, time, RunFakeBuildAsync);

		var reader = _state.Subscribe();
		_client = Task.Run(async () =>
		{
			await foreach (var e in reader.ReadAllAsync())
			{
				TaskCompletionSource changed;
				lock (_gate)
				{
					_log.Add(new Timestamped<BuildEvent>(DateTimeOffset.FromUnixTimeMilliseconds(e.Timestamp), e));
					if (e.Type == "build_start")
						_started++;
					else if (e.Type is "build_complete" or "build_cancelled")
						_ended++;
					changed = _changed;
					_changed = NewSignal();
				}
				_ = changed.TrySetResult();
			}
		});
		_loop = _state.RunAsync(_shutdown.Token);
	}

	/// <summary>The events the service broadcast, in order, with the virtual time each was stamped with.</summary>
	public IReadOnlyList<Timestamped<BuildEvent>> Log
	{
		get
		{
			lock (_gate)
			{
				return [.. _log];
			}
		}
	}

	/// <summary>Where the service says it is: idle, building or complete.</summary>
	public BuildStatus Status => _state.Status;

	/// <summary>A save that starts a build the test holds open. Does nothing while one is running (the service would queue the next).</summary>
	public async Task StartAsync(Cancel ct = default)
	{
		if (_state.Status == BuildStatus.Building)
			return;
		var before = Started;
		_plan = new BuildPlan(0, TimeSpan.Zero);
		_hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_state.ScheduleBuild("docs");
		await WaitUntilAsync(() => _started > before, ct);
	}

	/// <summary>Lets the held build end. Does nothing when none is running.</summary>
	public Task EndBuildAsync(Cancel ct = default) => EndHeldBuildAsync(hold => hold.SetResult(), ct);

	/// <summary>Makes the held build throw. Does nothing when none is running.</summary>
	public Task FailAsync(Cancel ct = default) =>
		EndHeldBuildAsync(hold => hold.SetException(new InvalidOperationException("the build failed")), ct);

	/// <summary>
	/// The host stopping mid-build, then starting again: the build is cancelled and the service is at rest. Does nothing when none is running.
	/// The service object is kept, and a new loop reads its queue, as a restarted host would.
	/// </summary>
	public async Task InterruptAsync(Cancel ct = default)
	{
		if (_state.Status != BuildStatus.Building)
			return;
		var before = Ended;
		await _shutdown.CancelAsync();
		await WaitUntilAsync(() => _ended > before, ct);
		await _loop.WaitAsync(Deadline, ct);
		_shutdown.Dispose();
		_shutdown = new CancellationTokenSource();
		_loop = _state.RunAsync(_shutdown.Token);
	}

	private async Task EndHeldBuildAsync(Action<TaskCompletionSource> end, Cancel ct)
	{
		if (_state.Status != BuildStatus.Building || _hold is not { } hold)
			return;
		var before = Ended;
		end(hold);
		await WaitUntilAsync(() => _ended > before, ct);
	}

	/// <summary>The service's snapshot, as the status endpoint serves it.</summary>
	public BuildEvent Snapshot() => _state.GetCurrentState();

	/// <summary>An editor save: schedules a build and waits for it to end.</summary>
	public async Task BuildAsync(BuildPlan plan, Cancel ct = default)
	{
		var expected = Ended + 1;
		_plan = plan;
		_state.ScheduleBuild("docs");
		await WaitUntilAsync(() => _ended >= expected, ct);
	}

	/// <summary>
	/// Saves while a build is running: the first build is held, <paramref name="extraSaves"/> more are scheduled behind it, then it is released.
	/// The service promises one more build, not one per save.
	/// </summary>
	public async Task BurstAsync(int extraSaves, BuildPlan plan, Cancel ct = default)
	{
		var before = Started;
		_plan = plan;
		_hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_state.ScheduleBuild("docs");
		await WaitUntilAsync(() => _started > before, ct);

		for (var i = 0; i < extraSaves; i++)
			_state.ScheduleBuild("docs");

		var expected = before + 1 + (extraSaves > 0 ? 1 : 0);
		_hold.SetResult();
		await WaitUntilAsync(() => _ended >= expected, ct);
		// a build that should not run would start now; waiting can only miss one, never fail a correct service
		await Task.Delay(TimeSpan.FromMilliseconds(5), ct);
		if (Started != expected || Ended != expected)
			throw new InvalidOperationException(
				$"{extraSaves} saves behind a running build should run {expected - before} builds in all, but {Started - before} started and {Ended - before} ended."
			);
		_hold = null;
	}

	/// <summary>Hands the log over and stops the service: the run is over.</summary>
	public IReadOnlyList<Timestamped<BuildEvent>> Finish()
	{
		var log = Log;
		Dispose();
		return log;
	}

	/// <summary>Stops the service without waiting for its loop: cancelling the shutdown token and completing the clients end both.</summary>
	public void Dispose()
	{
		_shutdown.Cancel();
		_lifetime.Cancel();
		_state.Dispose();
		_shutdown.Dispose();
		_lifetime.Dispose();
	}

	private int Started
	{
		get
		{
			lock (_gate)
			{
				return _started;
			}
		}
	}

	private int Ended
	{
		get
		{
			lock (_gate)
			{
				return _ended;
			}
		}
	}

	private async Task RunFakeBuildAsync(string sourcePath, DiagnosticsCollector collector, Cancel ct)
	{
		var plan = _plan;
		if (_hold is { } hold)
			await hold.Task.WaitAsync(ct);

		for (var i = 0; i < plan.Diagnostics; i++)
		{
			collector.Write(new Diagnostic
			{
				Severity = (i % 3) switch
				{
					0 => DiagnosticSeverity.Error,
					1 => DiagnosticSeverity.Warning,
					_ => DiagnosticSeverity.Hint
				},
				File = $"{sourcePath}/page-{i}.md",
				Message = $"finding {i}"
			});
		}
		_time.Advance(plan.Duration);
	}

	/// <summary>
	/// Waits, without holding a thread, until the counts the client keeps (read under the gate) satisfy <paramref name="condition"/>. A service that never
	/// comes to rest fails the run after <see cref="Deadline"/> instead of hanging it.
	/// </summary>
	private async Task WaitUntilAsync(Func<bool> condition, Cancel ct)
	{
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token);
		linked.CancelAfter(Deadline);
		while (true)
		{
			Task changed;
			lock (_gate)
			{
				if (condition())
					return;
				changed = _changed.Task;
			}
			try
			{
				await changed.WaitAsync(linked.Token);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				throw new TimeoutException("The service did not come to rest: a build never ended.");
			}
		}
	}

	private static TimeSpan Deadline => TimeSpan.FromSeconds(10);

	private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
