// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Filters;

public class StopwatchFilter(ConsoleAppFilter next, ILogger<StopwatchFilter> logger) : ConsoleAppFilter(next)
{
	public override async Task InvokeAsync(ConsoleAppContext context, Cancel cancellationToken)
	{
		var isHelpOrVersion = context.Arguments.Any(a => a is "--help" or "-h" or "--version");
		var name = string.IsNullOrWhiteSpace(context.CommandName) ? "generate" : context.CommandName;
		var startTime = Stopwatch.GetTimestamp();
		if (!isHelpOrVersion)
			logger.LogInformation("{Name} :: Starting...", name);
		try
		{
			await Next.InvokeAsync(context, cancellationToken);
		}
		finally
		{
			var endTime = Stopwatch.GetElapsedTime(startTime);
			if (!isHelpOrVersion)
				logger.LogInformation("{Name} :: Finished in '{EndTime}'", name, endTime);
		}
	}
}
