// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Services;

public interface IService;

public class ServiceInvoker(IDiagnosticsCollector collector) : IAsyncDisposable
{
	private sealed record InvokeState
	{
		public required string ServiceName { get; init; }
		public required bool Strict { get; init; }
		public required Func<Cancel, Task<bool>> Command { get; init; }
	}

	// Start the reader eagerly so diagnostics emitted before InvokeAsync (config load,
	// guard clauses, context construction) are drained live instead of dropped.
	// EnsureStarted is idempotent, so InvokeAsync's StartAsync call becomes a no-op.
	// The side-effectful StartAsync call is folded into the _tasks field initializer
	// (via EnsureReaderStarted) so that TreatWarningsAsErrors does not flag an unread field.
	private readonly List<InvokeState> _tasks = EnsureReaderStarted(collector);

	private static List<InvokeState> EnsureReaderStarted(IDiagnosticsCollector c)
	{
		_ = c.StartAsync(CancellationToken.None);
		return [];
	}

	public void AddCommand<TService, TState>(TService service, TState state, Func<TService, IDiagnosticsCollector, TState, Cancel, Task<bool>> invoke)
		where TService : IService =>
		_tasks.Add(new InvokeState
		{
			ServiceName = service.GetType().Name,
			Strict = false,
			Command = async ctx => await invoke(service, collector, state, ctx)
		});

	public void AddCommand<TService, TState>(TService service, TState state, bool strict, Func<TService, IDiagnosticsCollector, TState, Cancel, Task<bool>> invoke)
		where TService : IService =>
		_tasks.Add(new InvokeState
		{
			ServiceName = service.GetType().Name,
			Strict = strict,
			Command = async ctx => await invoke(service, collector, state, ctx)
		});

	public void AddCommand<TService>(TService service, Func<TService, IDiagnosticsCollector, Cancel, Task<bool>> invoke)
		where TService : IService =>
		_tasks.Add(new InvokeState
		{
			ServiceName = service.GetType().Name,
			Strict = false,
			Command = async ctx => await invoke(service, collector, ctx)
		});

	public async Task<int> InvokeAsync(Cancel ctx)
	{
		_ = collector.StartAsync(ctx);
		foreach (var task in _tasks)
		{
			try
			{
				var success = await task.Command(ctx).ConfigureAwait(false);
				await collector.WaitForDrain();
				if (!success && task.Strict && collector.Errors + collector.Warnings == 0)
					collector.EmitGlobalError($"Service {task.ServiceName} registered as strict but returned false without emitting errors or warnings ");
				if (!success && !task.Strict && collector.Errors == 0)
					collector.EmitGlobalError($"Service {task.ServiceName} returned false without emitting errors");
			}
			catch (Exception ex)
			{
				collector.EmitGlobalError($"Unhandled {task.ServiceName} exception: {ex.Message}", ex);
			}
			if (task.Strict && collector.Errors + collector.Warnings > 0)
			{
				await collector.StopAsync(ctx);
				return 1;
			}

			if (!task.Strict && collector.Errors > 0)
			{
				await collector.StopAsync(ctx);
				return 1;
			}
		}

		await collector.StopAsync(ctx);
		return 0;
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		// The collector is a shared singleton owned by the host; a per-command invoker must not
		// finalize it. Finalization happens once, at the CatchExceptionMiddleware boundary (via a
		// finally block), so an escaping exception's diagnostic is emitted BEFORE the summary is
		// drained and printed — not after.
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
