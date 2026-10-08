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
/// fake that reports diagnostics and spends virtual time. Every step waits until the service is at rest again, so a run is deterministic;
/// a subscribed client records what the service broadcasts, stamped by the same clock.
/// </summary>
internal sealed class BuildHarness : IDisposable
{
	private readonly ManualTimeProvider _time;
	private readonly InMemoryBuildState _state;
	private readonly CancellationTokenSource _shutdown = new();
	private readonly Task _loop;
	private readonly Task _client;
	private readonly Lock _gate = new();
	private readonly List<Timestamped<BuildEvent>> _log = [];
	private int _started;
	private int _ended;
	private BuildPlan _plan;
	private TaskCompletionSource? _hold;

	// A script step is synchronous: it waits for the service's async build loop, which needs a pool thread to make progress. CsCheck runs
	// iterations on several workers at once, so without spare threads the waiting workers can starve the loop they wait for.
	static BuildHarness() => _ = ThreadPool.SetMinThreads(256, 256);

	public BuildHarness(TimeProvider time)
	{
		_time = (ManualTimeProvider)time;
		_state = new InMemoryBuildState(NullLoggerFactory.Instance, null, time, RunFakeBuildAsync);

		var reader = _state.Subscribe();
		_client = Task.Run(async () =>
		{
			await foreach (var e in reader.ReadAllAsync())
			{
				lock (_gate)
				{
					_log.Add(new Timestamped<BuildEvent>(DateTimeOffset.FromUnixTimeMilliseconds(e.Timestamp), e));
					if (e.Type == "build_start")
						_started++;
					else if (e.Type is "build_complete" or "build_cancelled")
						_ended++;
				}
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

	/// <summary>The service's snapshot, as the status endpoint serves it.</summary>
	public BuildEvent Snapshot() => _state.GetCurrentState();

	/// <summary>An editor save: schedules a build and waits for it to end.</summary>
	public void Build(BuildPlan plan)
	{
		var expected = Ended + 1;
		_plan = plan;
		_state.ScheduleBuild("docs");
		WaitUntil(() => Ended >= expected);
	}

	/// <summary>
	/// Saves while a build is running: the first build is held, <paramref name="extraSaves"/> more are scheduled behind it, then it is released.
	/// The service promises one more build, not one per save.
	/// </summary>
	public void Burst(int extraSaves, BuildPlan plan)
	{
		var before = Started;
		_plan = plan;
		_hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_state.ScheduleBuild("docs");
		WaitUntil(() => Started > before);

		for (var i = 0; i < extraSaves; i++)
			_state.ScheduleBuild("docs");

		var expected = before + 1 + (extraSaves > 0 ? 1 : 0);
		_hold.SetResult();
		WaitUntil(() => Ended >= expected);
		Thread.Sleep(5); // a build that should not run would start now
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

	public void Dispose()
	{
		_shutdown.Cancel();
		_state.Dispose();
		try
		{
			Task.WaitAll([_loop, _client], TimeSpan.FromSeconds(5));
		}
		catch (AggregateException) { }
		_shutdown.Dispose();
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

	private static void WaitUntil(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow.AddSeconds(10);
		while (!condition())
		{
			if (DateTime.UtcNow > deadline)
				throw new TimeoutException("The service did not come to rest: a build never ended.");
			Thread.Sleep(1);
		}
	}
}
