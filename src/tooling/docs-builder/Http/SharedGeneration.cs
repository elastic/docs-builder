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
		// A change that lands during a run marks the output stale again, so the caller that joined that run
		// waits for one more instead of getting output built from the inputs before the change.
		do
			await CurrentRun().WaitAsync(ctx);
		while (isStale());
	}

	/// <summary>Joins the run in flight, or starts one when the output is stale or the last run failed.</summary>
	private Task CurrentRun()
	{
		lock (_gate)
		{
			if (!_run.IsCompleted || (!isStale() && !_run.IsFaulted))
				return _run;

			return _run = Task.Run(() => generate(_lifetime.Token), _lifetime.Token);
		}
	}

	public void Dispose()
	{
		_lifetime.Cancel();
		_lifetime.Dispose();
	}
}
