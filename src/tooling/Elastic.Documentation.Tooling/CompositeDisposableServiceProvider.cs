// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elastic.Documentation.Tooling;

// This exists temporarily while https://github.com/Cysharp/ConsoleAppFramework/pull/188 is open

public class CompositeDisposableServiceProvider(IDisposable host, IServiceProvider serviceServiceProvider, IDisposable scope, IServiceProvider serviceProvider)
	: IKeyedServiceProvider, IDisposable, IAsyncDisposable
{
	public object? GetService(Type serviceType) => serviceProvider.GetService(serviceType);

	public object? GetKeyedService(Type serviceType, object? serviceKey) => ((IKeyedServiceProvider)serviceProvider).GetKeyedService(serviceType, serviceKey);

	public object GetRequiredKeyedService(Type serviceType, object? serviceKey) => ((IKeyedServiceProvider)serviceProvider).GetRequiredKeyedService(serviceType, serviceKey);

	public void Dispose()
	{
		if (serviceProvider is IDisposable d)
			d.Dispose();

		scope.Dispose();
		if (serviceServiceProvider is IDisposable d2)
			d2.Dispose();

		host.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await CastAndDispose(host);
		await CastAndDispose(scope);
		await CastAndDispose(serviceProvider);
		await CastAndDispose(serviceServiceProvider);
		GC.SuppressFinalize(this);

		return;

		static async ValueTask CastAndDispose<T>(T resource)
		{
			if (resource is IAsyncDisposable resourceAsyncDisposable)
				await resourceAsyncDisposable.DisposeAsync();
			else if (resource is IDisposable resourceDisposable)
				resourceDisposable.Dispose();
		}
	}
}

public static class ConsoleAppHostBuilderExtensions
{
	public static TReturn ToConsoleAppBuilder<TReturn>(this IHostBuilder hostBuilder, Func<IServiceProvider, TReturn> configure)
	{
		var host = hostBuilder.Build();
		var serviceServiceProvider = host.Services;
		var scope = serviceServiceProvider.CreateScope();
		var serviceProvider = scope.ServiceProvider;
		var composite = new CompositeDisposableServiceProvider(host, serviceServiceProvider, scope, serviceProvider);
		return configure(composite);
	}

	public static TReturn ToConsoleAppBuilder<TReturn>(this HostApplicationBuilder hostBuilder, Func<IServiceProvider, TReturn> configure)
	{
		var host = hostBuilder.Build();
		var serviceServiceProvider = host.Services;
		var scope = serviceServiceProvider.CreateScope();
		var serviceProvider = scope.ServiceProvider;
		var composite = new CompositeDisposableServiceProvider(host, serviceServiceProvider, scope, serviceProvider);
		return configure(composite);
	}
}
