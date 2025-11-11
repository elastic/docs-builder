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
	private readonly List<InvokeState> _tasks = [];

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
	public async ValueTask DisposeAsync()
	{
		await collector.DisposeAsync();
		GC.SuppressFinalize(this);
	}
}
