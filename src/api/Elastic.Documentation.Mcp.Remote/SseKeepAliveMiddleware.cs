// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// Middleware that sends periodic SSE keepalive comments on <c>text/event-stream</c> responses
/// to prevent clients (notably Cursor) from timing out idle SSE connections.
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
				wrapper.StartKeepAlive();

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

	private readonly SemaphoreSlim _writeLock = new(1, 1);

	private PeriodicTimer? _timer;
	private CancellationTokenSource? _cts;
	private Task? _keepAliveTask;
	private long _lastWriteTicks = Environment.TickCount64;

	public void StartKeepAlive()
	{
		_cts = new CancellationTokenSource();
		_timer = new PeriodicTimer(interval);
		_keepAliveTask = RunKeepAlive(_cts.Token);
		logger.LogDebug("SSE keepalive started with {Interval}s interval", interval.TotalSeconds);
	}

	public async Task StopKeepAlive()
	{
		if (_cts is null)
			return;

		await _cts.CancelAsync();

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

		logger.LogDebug("SSE keepalive stopped");
	}

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
		_writeLock.Wait();
		try
		{
			inner.Write(buffer, offset, count);
			_ = Interlocked.Exchange(ref _lastWriteTicks, Environment.TickCount64);
		}
		finally
		{
			_ = _writeLock.Release();
		}
	}

	public override async Task FlushAsync(CancellationToken cancellationToken)
	{
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

	public override void Flush() => inner.Flush();

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

		_timer?.Dispose();
		_cts?.Dispose();
		_writeLock.Dispose();

		await base.DisposeAsync();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer?.Dispose();
			_cts?.Dispose();
			_writeLock.Dispose();
		}

		base.Dispose(disposing);
	}
}
