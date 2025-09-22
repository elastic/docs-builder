// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Filters;

#pragma warning disable CS9113 // Parameter is unread.
public sealed class CatchExceptionFilter(ConsoleAppFilter next, ILogger<CatchExceptionFilter> logger, IDiagnosticsCollector collector)
#pragma warning restore CS9113 // Parameter is unread.
	: ConsoleAppFilter(next)
{
	private bool _cancelKeyPressed;
	public override async Task InvokeAsync(ConsoleAppContext context, Cancel cancellationToken)
	{
		Console.CancelKeyPress += (_, _) =>
		{
			logger.LogInformation("Received CTRL+C cancelling");
			_cancelKeyPressed = true;
		};
		var error = false;
		try
		{
			await Next.InvokeAsync(context, cancellationToken);
		}
		catch (Exception ex)
		{
			if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested && _cancelKeyPressed)
			{
				logger.LogInformation("Cancellation requested, exiting.");
				return;
			}
			error = true;
			_ = collector.StartAsync(cancellationToken);
			collector.EmitGlobalError($"Global unhandled exception: {ex.Message}", ex);
			await collector.StopAsync(cancellationToken);
		}
		finally
		{
			Environment.ExitCode = error ? 1 : 0;
		}
	}
}
