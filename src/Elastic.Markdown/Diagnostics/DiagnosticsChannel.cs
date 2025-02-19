// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Elastic.Markdown.Diagnostics;

public sealed class DiagnosticsChannel : IDisposable
{
	private readonly Channel<Diagnostic> _channel;
	private readonly CancellationTokenSource _ctxSource;
	public ChannelReader<Diagnostic> Reader => _channel.Reader;

	public Cancel CancellationToken => _ctxSource.Token;

	public DiagnosticsChannel()
	{
		var options = new UnboundedChannelOptions { SingleReader = true, SingleWriter = false };
		_ctxSource = new CancellationTokenSource();
		_channel = Channel.CreateUnbounded<Diagnostic>(options);
	}

	public void TryComplete(Exception? exception = null)
	{
		_ = _channel.Writer.TryComplete(exception);
		_ctxSource.Cancel();
	}

	public ValueTask<bool> WaitToWrite() => _channel.Writer.WaitToWriteAsync(CancellationToken);

	public void Write(Diagnostic diagnostic)
	{
		var written = _channel.Writer.TryWrite(diagnostic);
		if (!written)
		{
			//TODO
		}
	}

	public void Dispose() => _ctxSource.Dispose();
}

public enum Severity { Error, Warning }

public readonly record struct Diagnostic
{
	public Severity Severity { get; init; }
	public int? Line { get; init; }
	public int? Column { get; init; }
	public int? Length { get; init; }
	public string File { get; init; }
	public string Message { get; init; }
}

public interface IDiagnosticsOutput
{
	void Write(Diagnostic diagnostic);
}

public class DiagnosticsCollector(IReadOnlyCollection<IDiagnosticsOutput> outputs)
	: IHostedService
{
	public DiagnosticsChannel Channel { get; } = new();

	private int _errors;
	private int _warnings;
	public int Warnings => _warnings;
	public int Errors => _errors;

	private Task? _started;

	public HashSet<string> OffendingFiles { get; } = [];

	public ConcurrentBag<string> CrossLinks { get; } = [];

	public Task StartAsync(Cancel cancellationToken)
	{
		if (_started is not null)
			return _started;
		_started = Task.Run(async () =>
		{
			_ = await Channel.WaitToWrite();
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
				IncrementSeverityCount(item);
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
	}

	protected virtual void HandleItem(Diagnostic diagnostic) { }

	public virtual async Task StopAsync(Cancel cancellationToken)
	{
		if (_started is not null)
			await _started;
		await Channel.Reader.Completion;
	}

	public void EmitCrossLink(string link) => CrossLinks.Add(link);

	public void EmitError(string file, string message, Exception? e = null)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = file,
			Message = message
					  + (e != null ? Environment.NewLine + e : string.Empty)
					  + (e?.InnerException != null ? Environment.NewLine + e.InnerException : string.Empty),

		};
		Channel.Write(d);
	}

	public void EmitWarning(string file, string message)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = file,
			Message = message,
		};
		Channel.Write(d);
	}
}
