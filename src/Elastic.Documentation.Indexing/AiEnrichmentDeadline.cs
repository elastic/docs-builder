// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Indexing;

/// <summary>
/// Links an ambient cancellation token to an optional wall-clock timeout, so an AI enrichment
/// run can be bounded by <c>--max-*-time</c> without the caller managing the linked
/// <see cref="CancellationTokenSource"/> lifetime by hand.
/// </summary>
public sealed class AiEnrichmentDeadline : IDisposable
{
	private readonly CancellationTokenSource? _timeoutCts;
	private readonly CancellationTokenSource? _linkedCts;

	private AiEnrichmentDeadline(CancellationTokenSource? timeoutCts, CancellationTokenSource? linkedCts, CancellationToken effective)
	{
		_timeoutCts = timeoutCts;
		_linkedCts = linkedCts;
		Token = effective;
	}

	/// <summary>The token to pass downstream — either the linked deadline token, or the original token when no timeout is set.</summary>
	public CancellationToken Token { get; }

	/// <summary>Whether cancellation was triggered by the wall-clock timeout rather than the ambient token.</summary>
	public bool TimedOut => _timeoutCts?.IsCancellationRequested == true;

	/// <summary>Builds a deadline. When <paramref name="maxWallClock"/> is <see langword="null"/>, <see cref="Token"/> is <paramref name="ct"/> unchanged.</summary>
	public static AiEnrichmentDeadline Create(TimeSpan? maxWallClock, CancellationToken ct)
	{
		var timeoutCts = maxWallClock is { } d ? new CancellationTokenSource(d) : null;
		var linkedCts = timeoutCts is not null
			? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)
			: null;
		return new AiEnrichmentDeadline(timeoutCts, linkedCts, linkedCts?.Token ?? ct);
	}

	public void Dispose()
	{
		_linkedCts?.Dispose();
		_timeoutCts?.Dispose();
	}
}
