// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GitHub;

/// <summary>Runs the independent batches of a GitHub query side by side, a few at a time.</summary>
internal static class BatchRunner
{
	/// <summary>
	/// The number of batches in flight. GitHub limits concurrent requests per token, so this stays small.
	/// </summary>
	public const int DefaultDegree = 4;

	/// <summary>
	/// Runs every batch, with at most <paramref name="degree"/> running at once, and returns the results in batch order.
	/// When a batch fails (<paramref name="failed"/>) or throws, the batches that have not started never start, the
	/// running ones get a cancelled token, and the method returns only after all of them are done, so nothing keeps
	/// spending the rate budget once the caller has moved on. A failure returns <c>null</c>; an exception, including a
	/// batch's own timeout, is rethrown.
	/// </summary>
	public static async Task<IReadOnlyList<TResult>?> RunAsync<TBatch, TResult>(
		IReadOnlyList<TBatch> batches,
		Func<TBatch, Cancel, Task<TResult>> run,
		Func<TResult, bool> failed,
		Cancel ctx,
		int degree = DefaultDegree
	)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx);
		using var gate = new SemaphoreSlim(degree);
		Exception? timedOut = null;

		var tasks = batches.Select(async batch =>
		{
			await gate.WaitAsync(cts.Token).ConfigureAwait(false);
			try
			{
				var result = await run(batch, cts.Token).ConfigureAwait(false);
				if (failed(result))
					await cts.CancelAsync().ConfigureAwait(false);
				return result;
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				await cts.CancelAsync().ConfigureAwait(false);
				throw;
			}
			catch (OperationCanceledException e) when (!cts.IsCancellationRequested)
			{
				// Neither the caller nor another batch cancelled, so this is the batch's own timeout (an HttpClient
				// timeout throws it). It is an error, not a stop signal: it goes to the caller after the rest are done.
				_ = Interlocked.CompareExchange(ref timedOut, e, null);
				await cts.CancelAsync().ConfigureAwait(false);
				throw;
			}
			finally
			{
				_ = gate.Release();
			}
		}).ToList();

		try
		{
			_ = await Task.WhenAll(tasks).ConfigureAwait(false);
		}
		catch (Exception)
		{
			// Every task has finished by now. What went wrong is read from the tasks below.
		}

		if (tasks.FirstOrDefault(t => t.IsFaulted) is { } faulted)
			_ = await faulted.ConfigureAwait(false);

		if (timedOut is not null)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(timedOut).Throw();

		ctx.ThrowIfCancellationRequested();

		// A batch that was cancelled because another one failed has no result, and neither has the run.
		return tasks.All(t => t.IsCompletedSuccessfully) && !tasks.Any(t => failed(t.Result)) ? [.. tasks.Select(t => t.Result)] : null;
	}
}
