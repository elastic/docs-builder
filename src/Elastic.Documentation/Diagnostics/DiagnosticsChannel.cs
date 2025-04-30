// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Channels;

namespace Elastic.Documentation.Diagnostics;

public sealed class DiagnosticsChannel : IDisposable
{
	private readonly Channel<Diagnostic> _channel;
	private readonly CancellationTokenSource _ctxSource;
	public ChannelReader<Diagnostic> Reader => _channel.Reader;

	public Cancel CancellationToken => _ctxSource.Token;

	public DiagnosticsChannel()
	{
		var options = new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false
		};
		_ctxSource = new CancellationTokenSource();
		_channel = Channel.CreateUnbounded<Diagnostic>(options);
	}

	public void TryComplete(Exception? exception = null)
	{
		_ = _channel.Writer.TryComplete(exception);
		_ctxSource.Cancel();
	}

	public ValueTask<bool> WaitToWrite(Cancel ctx) => _channel.Writer.WaitToWriteAsync(ctx);

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

public interface IDiagnosticsOutput
{
	void Write(Diagnostic diagnostic);
}
