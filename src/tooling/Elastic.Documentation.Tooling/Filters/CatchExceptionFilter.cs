// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Filters;

public sealed class CatchExceptionFilter(ConsoleAppFilter next, ILogger<CatchExceptionFilter> logger)
	: ConsoleAppFilter(next)
{
	private bool _cancelKeyPressed;
	public override async Task InvokeAsync(ConsoleAppContext context, Cancel cancellationToken)
	{
		Console.CancelKeyPress += (sender, e) =>
		{
			logger.LogInformation("Received CTRL+C cancelling");
			_cancelKeyPressed = true;
		};
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

			throw;

		}
	}
}
