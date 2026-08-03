// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace Elastic.Documentation.Diagnostics;

public interface IDiagnosticsCollector : IAsyncDisposable
{
	int Warnings { get; }
	int Errors { get; }
	int Hints { get; }

	bool NoHints { get; set; }

	DiagnosticsChannel Channel { get; }
	ConcurrentBag<string> CrossLinks { get; }
	HashSet<string> OffendingFiles { get; }
	ConcurrentDictionary<string, bool> InUseSubstitutionKeys { get; }

	/// True once the background reader is actively draining the channel.
	bool IsStarted { get; }

	/// True if StartAsync has been called and a reader task is pending or running,
	/// even if the background delegate has not yet executed. Distinguishes "start was
	/// requested but hasn't scheduled yet" from "StartAsync was never called at all".
	bool IsStartRequested { get; }

	/// Time source for drain waits and their timeouts; tests supply a fake to
	/// exercise timeout paths in virtual time instead of wall-clock time.
	TimeProvider TimeProvider { get; }

	Task StartAsync(Cancel cancellationToken);
	Task StopAsync(Cancel cancellationToken);

	void Emit(Severity severity, string file, string message);
	void EmitError(string file, string message, Exception? e = null);
	void EmitError(string file, string message, string specificErrorMessage);
	void EmitWarning(string file, string message);
	void EmitHint(string file, string message);
	void Write(Diagnostic diagnostic);
	void CollectUsedSubstitutionKey(ReadOnlySpan<char> key);
	void EmitCrossLink(string link);

	void EmitError(IFileInfo file, string message, Exception? e = null) => EmitError(file.FullName, message, e);

	void Emit(Severity severity, IFileInfo file, string message) => Emit(severity, file.FullName, message);

	void EmitWarning(IFileInfo file, string message) => EmitWarning(file.FullName, message);

	void EmitHint(IFileInfo file, string message) => EmitHint(file.FullName, message);

	/// Emit an error not associated with a file
	void EmitGlobalError(string message, Exception? e = null) => EmitError(string.Empty, message, e);

	/// Emit a warning not associated with a file
	void EmitGlobalWarning(string message) => EmitWarning(string.Empty, message);

	/// Emit a hint not associated with a file
	void EmitGlobalHint(string message) => EmitHint(string.Empty, message);

	async Task WaitForDrain()
	{
		if (!IsStarted)
		{
			// If StartAsync was never called, throw immediately — there is no pending reader.
			if (!IsStartRequested)
			{
				throw new InvalidOperationException(
					"WaitForDrain called on a collector that was never started; no reader is draining the channel. " +
					"Call StartAsync first or dispose the collector to drain synchronously.");
			}

			// StartAsync was called but the Task.Run delegate hasn't been picked up by the
			// thread pool yet (_readerStarted = true hasn't run). Spin briefly to let it start.
			// In practice this resolves in < 1ms; the 2s deadline is a safety net.
			var waitStart = TimeProvider.GetTimestamp();
			while (!IsStarted)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(10), TimeProvider);
				if (TimeProvider.GetElapsedTime(waitStart) > TimeSpan.FromSeconds(2))
					throw new InvalidOperationException(
						"WaitForDrain timed out waiting for the background reader to start. " +
						"StartAsync was called but the reader delegate did not start within the deadline.");
			}
		}

		var start = TimeProvider.GetTimestamp();
		while (Channel.Reader.TryPeek(out _))
		{
			await Task.Delay(TimeSpan.FromMilliseconds(10), TimeProvider);
			if (TimeProvider.GetElapsedTime(start) > TimeSpan.FromSeconds(2))
				throw new Exception("Could not iterate over all diagnostic messages in a timely fashion");
		}
	}


}
