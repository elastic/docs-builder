// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Services;

public interface IService;

public class ServiceInvoker(DiagnosticsCollector collector) : IAsyncDisposable
{
	private sealed record InvokeState
	{
		public required bool Strict { get; init; }
		public required Func<Cancel, Task> Command { get; init; }
	}
	private readonly List<InvokeState> _tasks = [];

	public void AddCommand<TService, TState>(TService service, TState state, Func<TService, TState, IDiagnosticsCollector, Cancel, Task> invoke)
		where TService : IService =>
		_tasks.Add(new InvokeState { Strict = false, Command = async ctx => await invoke(service, state, collector, ctx) });

	public void AddCommandStrict<TService, TState>(TService service, TState state, Func<TService, TState, IDiagnosticsCollector, Cancel, Task> invoke)
		where TService : IService =>
		_tasks.Add(new InvokeState { Strict = true, Command = async ctx => await invoke(service, state, collector, ctx) });

	public void AddCommand<TService>(TService service, Func<TService, IDiagnosticsCollector, Cancel, Task> invoke)
		where TService : IService =>
		_tasks.Add(new InvokeState { Strict = false, Command = async ctx => await invoke(service, collector, ctx) });

	public void AddCommandStrict<TService>(TService service, Func<TService, IDiagnosticsCollector, Cancel, Task> invoke)
		where TService : IService =>
		_tasks.Add(new InvokeState { Strict = true, Command = async ctx => await invoke(service, collector, ctx) });

	public async Task<int> InvokeAsync(Cancel ctx)
	{
		_ = collector.StartAsync(ctx);
		foreach (var task in _tasks)
		{
			await task.Command(ctx).ConfigureAwait(false);
			await collector.WaitForDrain();
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
