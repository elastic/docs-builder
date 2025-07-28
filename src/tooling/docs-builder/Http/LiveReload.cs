// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130
namespace Westwind.AspNetCore.LiveReload;
#pragma warning restore IDE0130

// This exists to disable AOT trimming error messages for the LiveReload middleware's own AddLiveReload() method.
// longer term we should build our own LiveReload middleware that doesn't rely on this also to help reduce dependencies

[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode", Justification = "Manually verified")]
[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL3050:RequiresDynamicCode", Justification = "Manually verified")]
public static class LiveReloadMiddlewareExtensions
{
	public static IServiceCollection AddAotLiveReload(this IServiceCollection services, Action<LiveReloadConfiguration> configAction)
	{

		var provider = services.BuildServiceProvider();
		var configuration = provider.GetService<IConfiguration>();

		var config = new LiveReloadConfiguration();
		configuration!.Bind("LiveReload", config);

		LiveReloadConfiguration.Current = config;

		if (!config.LiveReloadEnabled)
			return services;

		var env = provider.GetService<IWebHostEnvironment>();
		if (string.IsNullOrEmpty(config.FolderToMonitor))
			config.FolderToMonitor = env!.ContentRootPath;
		else if (config.FolderToMonitor.StartsWith('~'))
		{
			if (config.FolderToMonitor.Length > 1)
			{
				var folder = config.FolderToMonitor[1..];
				if (folder.StartsWith('/') || folder.StartsWith('\\'))
					folder = folder[1..];
				config.FolderToMonitor = Path.Combine(env!.ContentRootPath, folder);
				config.FolderToMonitor = Path.GetFullPath(config.FolderToMonitor);
			}
			else
				config.FolderToMonitor = env!.ContentRootPath;
		}

		configAction.Invoke(config);

		LiveReloadConfiguration.Current = config;

		return services;
	}

	public static IApplicationBuilder UseLiveReloadWithManualScriptInjection(this IApplicationBuilder builder, IHostApplicationLifetime webApplicationLifetime)
	{
		var config = LiveReloadConfiguration.Current;

		if (config.LiveReloadEnabled)
		{
			var webSocketOptions = new WebSocketOptions
			{
				KeepAliveInterval = TimeSpan.FromSeconds(300)
			};
			_ = builder.UseWebSockets(webSocketOptions);

			_ = builder
				.Use((context, next) =>
				{
					var middleWare = new NoInjectLiveReloadMiddleware(next, webApplicationLifetime);
					return middleWare.InvokeAsync(context);
				});

			// always refresh when the server restarts...
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();
		}

		return builder;
	}
}


/// <inheritdoc />
public class NoInjectLiveReloadMiddleware(RequestDelegate next, IHostApplicationLifetime lifeTime) : LiveReloadMiddleware(next, lifeTime)
{
	private readonly MethodInfo _handleWebSocketRequest =
		typeof(LiveReloadMiddleware).GetMethod("HandleWebSocketRequest", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod)!;

	private readonly RequestDelegate _next = next;

	public new async Task InvokeAsync(HttpContext context)
	{
		var config = LiveReloadConfiguration.Current;
		if (!config.LiveReloadEnabled)
		{
			await _next(context);
			return;
		}

		if (await HandleServeLiveReloadScript(context))
			return;

		// See if we have a WebSocket request. True means we handled
		var invoked = await (Task<bool>)_handleWebSocketRequest.Invoke(this, [context])!;
		if (invoked)
			return;

		await _next(context);
	}
}
