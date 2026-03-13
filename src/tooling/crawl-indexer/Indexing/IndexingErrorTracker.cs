// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace CrawlIndexer.Indexing;

/// <summary>
/// Diagnostics output that tracks the first error for display in final summary.
/// Implements IDiagnosticsOutput to receive diagnostics from the collector.
/// Also provides fail-fast cancellation support.
/// </summary>
public sealed class IndexingErrorTracker : IDiagnosticsOutput
{
	private CancellationTokenSource? _failFastCts;
	private Diagnostic? _firstError;
	private Exception? _firstException;

	public Diagnostic? FirstError => _firstError;
	public Exception? FirstException => _firstException;
	public bool HasErrors => _firstError is not null;

	/// <summary>
	/// Sets the fail-fast cancellation token source.
	/// Call this at the start of a command when using --fail-fast.
	/// </summary>
	public void SetFailFastToken(CancellationTokenSource? failFastCts) => _failFastCts = failFastCts;

	public void Write(Diagnostic diagnostic)
	{
		if (diagnostic.Severity != Severity.Error)
			return;

		// Lock-free: only capture the first error
		_ = Interlocked.CompareExchange(ref _firstError, diagnostic, null);

		// Trigger fail-fast cancellation
		_failFastCts?.Cancel();
	}

	/// <summary>
	/// Records an exception associated with the first error.
	/// Call this when you have additional exception context.
	/// </summary>
	public void RecordException(Exception exception) =>
		_ = Interlocked.CompareExchange(ref _firstException, exception, null);

}
