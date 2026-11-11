// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Argh.Middleware;

namespace Elastic.Documentation.Journeys.Cli;

/// <summary>
/// Bad input (an unknown area, a missing folder, a malformed URL) is the caller's to fix: one <c>Error:</c> line and exit
/// code 1, as <c>snap</c> does, not a stack trace.
/// </summary>
internal sealed class UserInputErrors : ICommandMiddleware
{
	public async ValueTask InvokeAsync(CommandContext context, CommandMiddlewareDelegate next)
	{
		try
		{
			await next(context).ConfigureAwait(false);
		}
		catch (Exception e) when (e is ArgumentException or DirectoryNotFoundException)
		{
			await Console.Error.WriteLineAsync($"Error: {e.Message}").ConfigureAwait(false);
			context.ExitCode = 1;
		}
	}
}
