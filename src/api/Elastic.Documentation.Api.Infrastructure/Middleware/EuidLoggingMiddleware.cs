// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware that adds the euid cookie value to the logging scope for all subsequent log entries in the request.
/// </summary>
public class EuidLoggingMiddleware(RequestDelegate next, ILogger<EuidLoggingMiddleware> logger)
{
	public async Task InvokeAsync(HttpContext context)
	{
		// Try to get the euid cookie
		if (context.Request.Cookies.TryGetValue("euid", out var euid) && !string.IsNullOrEmpty(euid))
		{
			// Add euid to logging scope so it appears in all log entries for this request
			using (logger.BeginScope(new Dictionary<string, object> { [TelemetryConstants.UserEuidAttributeName] = euid }))
			{
				await next(context);
			}
		}
		else
		{
			await next(context);
		}
	}
}

/// <summary>
/// Extension methods for registering the EuidLoggingMiddleware.
/// </summary>
public static class EuidLoggingMiddlewareExtensions
{
	/// <summary>
	/// Adds the EuidLoggingMiddleware to the application pipeline.
	/// This middleware enriches logs with the euid cookie value.
	/// </summary>
	public static IApplicationBuilder UseEuidLogging(this IApplicationBuilder app) => app.UseMiddleware<EuidLoggingMiddleware>();
}
