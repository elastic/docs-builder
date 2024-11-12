// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Diagnostics;

public class DiagnosticsChannel
{
	private readonly Channel<Diagnostic> _channel;
	private readonly CancellationTokenSource _ctxSource;
	public ChannelReader<Diagnostic> Reader => _channel.Reader;

	public CancellationToken CancellationToken => _ctxSource.Token;

	public DiagnosticsChannel()
	{
		var options = new UnboundedChannelOptions { SingleReader = true, SingleWriter = false };
		_ctxSource = new CancellationTokenSource();
		_channel = Channel.CreateUnbounded<Diagnostic>(options);
	}

	public void TryComplete(Exception? exception = null)
	{
		_channel.Writer.TryComplete(exception);
		_ctxSource.Cancel();
	}

	public ValueTask<bool> WaitToWrite() => _channel.Writer.WaitToWriteAsync();

	public void Write(Diagnostic diagnostic)
	{
		var written = _channel.Writer.TryWrite(diagnostic);
		if (!written)
		{
			//TODO
		}
	}
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
	public void Write(Diagnostic diagnostic);
}

public class LogDiagnosticOutput(ILogger logger) : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Error)
			logger.LogError($"{diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
		else
			logger.LogWarning($"{diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
	}
}


public class DiagnosticsCollector(ILoggerFactory loggerFactory, IReadOnlyCollection<IDiagnosticsOutput> outputs)
	: IHostedService
{
	private readonly IReadOnlyCollection<IDiagnosticsOutput> _outputs =
		[new LogDiagnosticOutput(loggerFactory.CreateLogger<LogDiagnosticOutput>()), ..outputs];

	public DiagnosticsChannel Channel { get; } = new();

	private long _errors;
	private long _warnings;
	public long Warnings => _warnings;
	public long Errors => _errors;

	public async Task StartAsync(Cancel ctx)
	{
		await Channel.WaitToWrite();
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

		void Drain()
		{
			while (Channel.Reader.TryRead(out var item))
			{
				IncrementSeverityCount(item);
				HandleItem(item);
				foreach (var output in _outputs)
					output.Write(item);
			}
		}
	}

	private void IncrementSeverityCount(Diagnostic item)
	{
		if (item.Severity == Severity.Error)
			Interlocked.Increment(ref _errors);
		else if (item.Severity == Severity.Warning)
			Interlocked.Increment(ref _warnings);
	}

	protected virtual void HandleItem(Diagnostic diagnostic) {}

	public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
