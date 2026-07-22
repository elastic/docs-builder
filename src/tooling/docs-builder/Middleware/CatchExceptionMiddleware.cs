// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Nullean.Argh.Middleware;

namespace Documentation.Builder.Middleware;

internal sealed class CatchExceptionMiddleware(ILogger<CatchExceptionMiddleware> logger, IDiagnosticsCollector collector)
	: ICommandMiddleware
{
	private bool _cancelKeyPressed;

	public async ValueTask InvokeAsync(CommandContext context, CommandMiddlewareDelegate next)
	{
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
			// ServiceInvoker no longer finalizes the collector on unwind, so the channel is still
			// open here. The error is counted, drained, rendered in errata detail, and reflected in
			// the summary that the finally block below prints.
			_ = collector.StartAsync(context.CancellationToken);
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
