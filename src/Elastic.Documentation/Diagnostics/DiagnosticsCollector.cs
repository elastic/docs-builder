// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Elastic.Documentation.Diagnostics;

public class DiagnosticsCollector(IReadOnlyCollection<IDiagnosticsOutput> outputs)
	: IDiagnosticsCollector
{
	private DiagnosticsChannel Channel { get; } = new();

	private int _errors;
	private int _warnings;
	private int _hints;
	public int Warnings => _warnings;
	public int Errors => _errors;
	public int Hints => _hints;

	private Task? _started;

	public HashSet<string> OffendingFiles { get; } = [];

	public ConcurrentDictionary<string, bool> InUseSubstitutionKeys { get; } = [];

	public ConcurrentBag<string> CrossLinks { get; } = [];

	public bool NoHints { get; init; }

	public DiagnosticsCollector StartAsync(Cancel ctx)
	{
		_ = ((IHostedService)this).StartAsync(ctx);
		return this;
	}

	Task IHostedService.StartAsync(Cancel cancellationToken)
	{
		if (_started is not null)
			return _started;
		_started = Task.Run(async () =>
		{
			_ = await Channel.WaitToWrite(cancellationToken);
			while (!Channel.CancellationToken.IsCancellationRequested)
			{
				try
				{
					while (await Channel.Reader.WaitToReadAsync(Channel.CancellationToken))
						Drain();
				}
				catch
				{
					//ignore
				}
			}

			Drain();
		}, cancellationToken);
		return _started;

		void Drain()
		{
			while (Channel.Reader.TryRead(out var item))
			{
				if (item.Severity == Severity.Hint && NoHints)
					continue;
				HandleItem(item);
				_ = OffendingFiles.Add(item.File);
				foreach (var output in outputs)
					output.Write(item);
			}
		}
	}

	private void IncrementSeverityCount(Diagnostic item)
	{
		if (item.Severity == Severity.Error)
			_ = Interlocked.Increment(ref _errors);
		else if (item.Severity == Severity.Warning)
			_ = Interlocked.Increment(ref _warnings);
		else if (item.Severity == Severity.Hint && !NoHints)
			_ = Interlocked.Increment(ref _hints);
	}

	protected virtual void HandleItem(Diagnostic diagnostic) { }

	public virtual async Task StopAsync(Cancel cancellationToken)
	{
		Channel.TryComplete();
		if (_started is not null)
			await _started;
		await Channel.Reader.Completion;
	}

	public void EmitCrossLink(string link) => CrossLinks.Add(link);

	public void Write(Diagnostic diagnostic)
	{
		IncrementSeverityCount(diagnostic);
		Channel.Write(diagnostic);
	}

	public void Emit(Severity severity, string file, string message) =>
		Write(new Diagnostic
		{
			Severity = severity,
			File = file,
			Message = message
		});

	public void EmitError(string file, string message, Exception? e = null)
	{
		message = message
				+ (e != null ? Environment.NewLine + e : string.Empty)
				+ (e?.InnerException != null ? Environment.NewLine + e.InnerException : string.Empty);
		Emit(Severity.Error, file, message);
	}

	public void EmitWarning(string file, string message) => Emit(Severity.Warning, file, message);

	public void EmitHint(string file, string message) => Emit(Severity.Hint, file, message);

	public void EmitError(IFileInfo file, string message, Exception? e = null) => EmitError(file.FullName, message, e);

	public void EmitWarning(IFileInfo file, string message) => Emit(Severity.Warning, file.FullName, message);

	public void EmitHint(IFileInfo file, string message) => Emit(Severity.Hint, file.FullName, message);

	public async ValueTask DisposeAsync()
	{
		Channel.TryComplete();
		await StopAsync(CancellationToken.None);
		GC.SuppressFinalize(this);
	}

	public void CollectUsedSubstitutionKey(ReadOnlySpan<char> key) =>
		_ = InUseSubstitutionKeys.TryAdd(key.ToString(), true);
}
