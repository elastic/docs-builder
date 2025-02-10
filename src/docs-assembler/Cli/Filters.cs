// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using ConsoleAppFramework;

namespace Documentation.Assembler.Cli;

internal class StopwatchFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
	public override async Task InvokeAsync(ConsoleAppContext context, Cancel ctx)
	{
		var isHelpOrVersion = context.Arguments.Any(a => a is "--help" or "-h" or "--version");
		var name = string.IsNullOrWhiteSpace(context.CommandName) ? "generate" : context.CommandName;
		var startTime = Stopwatch.GetTimestamp();
		if (!isHelpOrVersion)
			ConsoleApp.Log($"{name} :: Starting...");
		try
		{
			await Next.InvokeAsync(context, ctx);
		}
		finally
		{
			var endTime = Stopwatch.GetElapsedTime(startTime);
			if (!isHelpOrVersion)
				ConsoleApp.Log($"{name} :: Finished in '{endTime}");
		}
	}
}

internal class CatchExceptionFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
	public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
	{
		try
		{
			await Next.InvokeAsync(context, cancellationToken);
		}
		catch (Exception ex)
		{
			if (ex is OperationCanceledException)
			{
				ConsoleApp.Log("Cancellation requested, exiting.");
				return;
			}

			throw;

		}
	}
}
