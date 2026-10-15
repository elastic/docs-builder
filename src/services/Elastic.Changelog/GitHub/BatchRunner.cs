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
	/// Starts every batch, with at most <paramref name="degree"/> running at once. The tasks come back in batch order,
	/// so the caller reads the results in order and its output does not depend on which batch finishes first.
	/// </summary>
	public static IReadOnlyList<Task<TResult>> Start<TBatch, TResult>(
		IReadOnlyList<TBatch> batches,
		Func<TBatch, Task<TResult>> run,
		int degree = DefaultDegree
	)
	{
		var gate = new SemaphoreSlim(degree);
		return [
			.. batches.Select(async batch =>
			{
				await gate.WaitAsync().ConfigureAwait(false);
				try
				{
					return await run(batch).ConfigureAwait(false);
				}
				finally
				{
					_ = gate.Release();
				}
			})
		];
	}
}
