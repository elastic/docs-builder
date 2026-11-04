// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Nullean.Argh.Middleware;

namespace Documentation.Builder.Middleware;

internal sealed class CatchExceptionMiddleware(
	ILogger<CatchExceptionMiddleware> logger,
	IDiagnosticsCollector collector
) : ICommandMiddleware
{
	private bool _cancelKeyPressed;

	public async ValueTask InvokeAsync(CommandContext context, CommandMiddlewareDelegate next)
	{
		// Start the background reader unconditionally. Every command path — success, handled error,
		// and unhandled exception — needs the channel drained before StopAsync renders the summary.
		_ = collector.StartAsync(context.CancellationToken);

		Console.CancelKeyPress += (_, args) =>
		{
			// Suppress OS termination so the OperationCanceledException path below can run gracefully.
			args.Cancel = true;
			logger.LogInformation("Received CTRL+C cancelling");
			_cancelKeyPressed = true;
		};
		try
		{
			await next(context);
		}
		catch (Exception ex)
		{
			if (ex is OperationCanceledException && context.CancellationToken.IsCancellationRequested && _cancelKeyPressed)
			{
				logger.LogInformation("Cancellation requested, exiting.");
				context.ExitCode = 1;
				return; // finally still runs
			}
			collector.EmitGlobalError($"Global unhandled exception: {ex.Message}", ex);
			context.ExitCode = 1;
		}
		finally
		{
			// Single finalization point for the whole CLI. Idempotent: a no-op when InvokeAsync
			// already stopped the collector on the success / handled-error path (_stopped guard).
			await collector.StopAsync(context.CancellationToken);
		}
	}
}
