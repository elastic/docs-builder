// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nullean.Argh.Middleware;

namespace Documentation.Builder.Middleware;

internal sealed class StopwatchMiddleware(ILogger<StopwatchMiddleware> logger) : ICommandMiddleware
{
	public async ValueTask InvokeAsync(CommandContext context, CommandMiddlewareDelegate next)
	{
		var name = context.CommandName.Length == 0 ? "generate" : context.CommandName;
		var startTime = Stopwatch.GetTimestamp();
		logger.LogInformation("{Name} :: Starting...", name);
		try
		{
			await next(context);
		}
		finally
		{
			var elapsed = Stopwatch.GetElapsedTime(startTime);
			logger.LogInformation("{Name} :: Finished in '{Elapsed}'", name, elapsed);
		}
	}
}
