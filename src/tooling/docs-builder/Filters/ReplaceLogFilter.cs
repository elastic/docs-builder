// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Filters;

internal sealed class ReplaceLogFilter(ConsoleAppFilter next, ILogger<Program> logger)
	: ConsoleAppFilter(next)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	public override Task InvokeAsync(ConsoleAppContext context, Cancel cancellationToken)
	{
		ConsoleApp.Log = msg => logger.LogInformation(msg);
		ConsoleApp.LogError = msg => logger.LogError(msg);

		return Next.InvokeAsync(context, cancellationToken);
	}
}
