// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for testing the API with mocked services.
/// This fixture can be reused across multiple test classes.
/// Only mocks services that ALL tests need (OpenTelemetry, AWS Parameters).
/// Test-specific mocks should be configured using WithMockedServices.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
	public List<Activity> ExportedActivities { get; } = [];
	public List<LogRecord> ExportedLogRecords { get; } = [];
	private readonly Action<IServiceCollection>? _configureServices;

	public ApiWebApplicationFactory() : this(null)
	{
	}

	internal ApiWebApplicationFactory(Action<IServiceCollection>? configureServices) => _configureServices = configureServices;

	/// <summary>
	/// Creates a factory with specific services replaced by mocks.
	/// This allows tests to inject fake implementations for testing specific scenarios.
	/// </summary>
	/// <param name="serviceReplacements">Action to configure service replacements</param>
	/// <returns>New factory instance with replaced services</returns>
	public static ApiWebApplicationFactory WithMockedServices(Action<ServiceReplacementBuilder> serviceReplacements)
	{
		var builder = new ServiceReplacementBuilder();
		serviceReplacements(builder);
		return new ApiWebApplicationFactory(builder.Build());
	}

	/// <summary>
	/// Creates a factory with custom service configuration.
	/// </summary>
	/// <param name="configureServices">Action to configure services directly</param>
	/// <returns>New factory instance with custom service configuration</returns>
	public static ApiWebApplicationFactory WithMockedServices(Action<IServiceCollection> configureServices)
		=> new(configureServices);

	protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
	{
		// Configure OpenTelemetry with in-memory exporters for all tests
		var otelBuilder = services.AddOpenTelemetry();
		_ = otelBuilder.WithTracing(tracing =>
		{
			_ = tracing
				.AddDocsApiTracing() // Reuses production configuration
				.AddInMemoryExporter(ExportedActivities);
		});
		_ = otelBuilder.WithLogging(logging =>
		{
			_ = logging
				.AddDocsApiLogging() // Reuses production configuration
				.AddInMemoryExporter(ExportedLogRecords);
		});

		// Mock IParameterProvider to avoid AWS dependencies in all tests
		var mockParameterProvider = A.Fake<IParameterProvider>();
		A.CallTo(() => mockParameterProvider.GetParam(A<string>._, A<bool>._, A<Cancel>._))
			.Returns(Task.FromResult("mock-value"));
		_ = services.AddSingleton(mockParameterProvider);

		// Apply test-specific service replacements (if any)
		_configureServices?.Invoke(services);
	});
}

/// <summary>
/// Builder for replacing services in integration tests.
/// Provides a fluent API for replacing multiple services with mocks.
/// </summary>
public class ServiceReplacementBuilder
{
	private readonly List<Action<IServiceCollection>> _replacements = [];

	/// <summary>
	/// Replace a service of type TService with a specific instance.
	/// </summary>
	/// <typeparam name="TService">The service interface type to replace</typeparam>
	/// <param name="instance">The mock/fake instance to use</param>
	/// <returns>This builder for chaining</returns>
	public ServiceReplacementBuilder Replace<TService>(TService instance) where TService : class
	{
		_replacements.Add(services =>
		{
			services.RemoveAll<TService>();
			_ = services.AddScoped(_ => instance);
		});
		return this;
	}

	/// <summary>
	/// Replace a service of type TService with a factory function.
	/// </summary>
	/// <typeparam name="TService">The service interface type to replace</typeparam>
	/// <param name="factory">Factory function to create the service</param>
	/// <returns>This builder for chaining</returns>
	public ServiceReplacementBuilder Replace<TService>(Func<IServiceProvider, TService> factory) where TService : class
	{
		_replacements.Add(services =>
		{
			services.RemoveAll<TService>();
			_ = services.AddScoped(factory);
		});
		return this;
	}

	/// <summary>
	/// Replace a service with a singleton instance.
	/// </summary>
	/// <typeparam name="TService">The service interface type to replace</typeparam>
	/// <param name="instance">The singleton instance to use</param>
	/// <returns>This builder for chaining</returns>
	public ServiceReplacementBuilder ReplaceSingleton<TService>(TService instance) where TService : class
	{
		_replacements.Add(services =>
		{
			services.RemoveAll<TService>();
			_ = services.AddSingleton(_ => instance);
		});
		return this;
	}

	/// <summary>
	/// Builds the final service configuration action.
	/// </summary>
	internal Action<IServiceCollection> Build() => services =>
	{
		foreach (var replacement in _replacements)
		{
			replacement(services);
		}
	};
}
