// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Documentation.Builder.Http;

/// <summary>
/// Runs one generation at a time on a token the server owns. A caller waits on the run with its own token,
/// so a caller that gives up does not cancel work the next caller would otherwise redo from scratch.
/// </summary>
/// <param name="generate">Does the work. It marks the output stale again when it fails, so a retry regenerates.</param>
/// <param name="isStale">True when the output no longer matches its inputs.</param>
public sealed class SharedGeneration(Func<Cancel, Task> generate, Func<bool> isStale) : IDisposable
{
	private readonly Lock _gate = new();
	private readonly CancellationTokenSource _lifetime = new();
	private Task _run = Task.CompletedTask;

	public async Task EnsureAsync(Cancel ctx)
	{
		// A caller leaves only when no run is in flight and the output is fresh, and it decides that under the
		// gate. A change that lands during a run therefore costs the caller one more run, and a caller that wakes
		// after another caller started that run joins it, so nobody returns while pages are still being rewritten.
		while (CurrentRun() is { } run)
			await run.WaitAsync(ctx);
	}

	/// <summary>The run to wait on: the one in flight, or a new one when the output is stale or the last run failed. Null when the output is fresh.</summary>
	private Task? CurrentRun()
	{
		lock (_gate)
		{
			if (!_run.IsCompleted)
				return _run;
			if (!isStale() && !_run.IsFaulted)
				return null;

			return _run = Task.Run(() => generate(_lifetime.Token), _lifetime.Token);
		}
	}

	public void Dispose()
	{
		_lifetime.Cancel();
		_lifetime.Dispose();
	}
}
