// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// Middleware that sends periodic SSE keepalive comments on <c>text/event-stream</c> responses
/// to prevent clients (notably Cursor) from timing out idle SSE connections.
/// Covers both Streamable HTTP (<c>/docs/_mcp/</c>) and legacy SSE (<c>/docs/_mcp/sse</c>) endpoints.
/// </summary>
public class SseKeepAliveMiddleware(RequestDelegate next, ILogger<SseKeepAliveMiddleware> logger)
{
	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(5);

	public async Task InvokeAsync(HttpContext context)
	{
		if (!context.Request.Path.StartsWithSegments("/docs/_mcp"))
		{
			await next(context);
			return;
		}

		var originalBody = context.Response.Body;
		await using var wrapper = new SseKeepAliveStream(originalBody, KeepAliveInterval, logger);
		context.Response.Body = wrapper;

		context.Response.OnStarting(() =>
		{
			if (context.Response.ContentType?.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) == true)
				wrapper.StartKeepAlive(context.RequestAborted);

			return Task.CompletedTask;
		});

		try
		{
			await next(context);
		}
		finally
		{
			await wrapper.StopKeepAlive();
			context.Response.Body = originalBody;
		}
	}
}

/// <summary>
/// Stream wrapper that periodically writes SSE comment lines (<c>: keepalive\n\n</c>)
/// when the underlying stream is idle, preventing between-bytes timeouts.
/// </summary>
internal sealed class SseKeepAliveStream(Stream inner, TimeSpan interval, ILogger logger) : Stream
{
	private static readonly byte[] KeepAliveBytes = Encoding.UTF8.GetBytes(": keepalive\n\n");

	// Used as an async mutex to synchronize writes between the MCP SDK and the keepalive timer
	private readonly SemaphoreSlim _writeLock = new(1, 1);

	private PeriodicTimer? _timer;
	private CancellationTokenSource? _cts;
	private Task? _keepAliveTask;
	private long _lastWriteTicks = Environment.TickCount64;

	/// <summary>Starts the periodic keepalive task, linked to the request's cancellation token.</summary>
	public void StartKeepAlive(CancellationToken requestAborted)
	{
		_cts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
		_timer = new PeriodicTimer(interval);
		_keepAliveTask = RunKeepAlive(_cts.Token);
		logger.LogDebug("SSE keepalive started with {Interval}s interval", interval.TotalSeconds);
	}

	/// <summary>Signals the keepalive task to stop and awaits its completion. Safe to call multiple times.</summary>
	public async Task StopKeepAlive()
	{
		var cts = Interlocked.Exchange(ref _cts, null);
		if (cts is null)
			return;

		await cts.CancelAsync();

		if (_keepAliveTask is not null)
		{
			try
			{
				await _keepAliveTask;
			}
			catch (OperationCanceledException)
			{
				// Expected on cancellation
			}
		}

		_timer?.Dispose();
		cts.Dispose();

		logger.LogDebug("SSE keepalive stopped");
	}

	private bool IsKeepAliveActive => _keepAliveTask is not null;

	private async Task RunKeepAlive(CancellationToken ct)
	{
		try
		{
			while (await _timer!.WaitForNextTickAsync(ct))
			{
				var elapsed = Environment.TickCount64 - Interlocked.Read(ref _lastWriteTicks);
				if (elapsed < interval.TotalMilliseconds)
					continue;

				await _writeLock.WaitAsync(ct);
				try
				{
					await inner.WriteAsync(KeepAliveBytes, ct);
					await inner.FlushAsync(ct);
					_ = Interlocked.Exchange(ref _lastWriteTicks, Environment.TickCount64);
				}
				catch (ObjectDisposedException)
				{
					break;
				}
				catch (IOException)
				{
					break;
				}
				finally
				{
					_ = _writeLock.Release();
				}
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			// Normal shutdown
		}
	}

	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!IsKeepAliveActive)
		{
			await inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
			return;
		}

		await _writeLock.WaitAsync(cancellationToken);
		try
		{
			await inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
			_ = Interlocked.Exchange(ref _lastWriteTicks, Environment.TickCount64);
		}
		finally
		{
			_ = _writeLock.Release();
		}
	}

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (!IsKeepAliveActive)
		{
			await inner.WriteAsync(buffer, cancellationToken);
			return;
		}

		await _writeLock.WaitAsync(cancellationToken);
		try
		{
			await inner.WriteAsync(buffer, cancellationToken);
			_ = Interlocked.Exchange(ref _lastWriteTicks, Environment.TickCount64);
		}
		finally
		{
			_ = _writeLock.Release();
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (!IsKeepAliveActive)
		{
			inner.Write(buffer, offset, count);
			return;
		}

		// SSE streams should always use async writes; blocking here risks threadpool starvation
		throw new NotSupportedException("Synchronous writes are not supported on active SSE keepalive streams.");
	}

	public override async Task FlushAsync(CancellationToken cancellationToken)
	{
		if (!IsKeepAliveActive)
		{
			await inner.FlushAsync(cancellationToken);
			return;
		}

		await _writeLock.WaitAsync(cancellationToken);
		try
		{
			await inner.FlushAsync(cancellationToken);
		}
		finally
		{
			_ = _writeLock.Release();
		}
	}

	public override void Flush()
	{
		if (!IsKeepAliveActive)
		{
			inner.Flush();
			return;
		}

		throw new NotSupportedException("Synchronous flush is not supported on active SSE keepalive streams.");
	}

	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		inner.ReadAsync(buffer, offset, count, cancellationToken);

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
		inner.ReadAsync(buffer, cancellationToken);

	public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

	public override void SetLength(long value) => inner.SetLength(value);

	public override bool CanRead => inner.CanRead;

	public override bool CanSeek => inner.CanSeek;

	public override bool CanWrite => inner.CanWrite;

	public override long Length => inner.Length;

	public override long Position
	{
		get => inner.Position;
		set => inner.Position = value;
	}

	public override async ValueTask DisposeAsync()
	{
		await StopKeepAlive();
		_writeLock.Dispose();

		await base.DisposeAsync();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			// Signal the background task to stop before disposing resources
			var cts = Interlocked.Exchange(ref _cts, null);
			if (cts is not null)
			{
				cts.Cancel();
				try
				{
					_keepAliveTask?.GetAwaiter().GetResult();
				}
				catch (OperationCanceledException)
				{
					// Expected on cancellation
				}

				_timer?.Dispose();
				cts.Dispose();
			}

			_writeLock.Dispose();
		}

		base.Dispose(disposing);
	}
}
