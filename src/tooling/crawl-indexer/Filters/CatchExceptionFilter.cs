// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace CrawlIndexer.Filters;

internal sealed class CatchExceptionFilter(ConsoleAppFilter next, ILogger<CatchExceptionFilter> logger, IDiagnosticsCollector collector)
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

			// Show error prominently using Spectre's exception rendering
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[red bold]━━━ UNHANDLED EXCEPTION ━━━[/]");
			WriteExceptionSafe(ex);
			AnsiConsole.MarkupLine("[red bold]━━━━━━━━━━━━━━━━━━━━━━━━━━━[/]");

			_ = collector.StartAsync(cancellationToken);
			collector.EmitGlobalError($"Global unhandled exception: {ex.Message}", ex);
			await collector.StopAsync(cancellationToken);
			Environment.ExitCode = 1;
		}
	}

	[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
		Justification = "Exception formatting is best-effort for error display")]
	private static void WriteExceptionSafe(Exception ex) =>
		AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes | ExceptionFormats.ShortenMethods);
}
