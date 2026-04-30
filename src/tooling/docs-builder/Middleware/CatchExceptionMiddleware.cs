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
		Console.CancelKeyPress += (_, _) =>
		{
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
				return;
			}
			_ = collector.StartAsync(context.CancellationToken);
			collector.EmitGlobalError($"Global unhandled exception: {ex.Message}", ex);
			await collector.StopAsync(context.CancellationToken);
			context.ExitCode = 1;
		}
	}
}
