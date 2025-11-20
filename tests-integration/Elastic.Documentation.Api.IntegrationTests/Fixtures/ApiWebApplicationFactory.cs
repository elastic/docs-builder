// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Telemetry;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for testing the API with mocked services.
/// This fixture can be reused across multiple test classes.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
	public List<Activity> ExportedActivities { get; } = [];
	public List<LogRecord> ExportedLogRecords { get; } = [];
	private readonly List<MemoryStream> _mockMemoryStreams = [];
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

																				  // Mock IParameterProvider to avoid AWS dependencies
																				  var mockParameterProvider = A.Fake<IParameterProvider>();
																				  A.CallTo(() => mockParameterProvider.GetParam(A<string>._, A<bool>._, A<Cancel>._))
																					  .Returns(Task.FromResult("mock-value"));
																				  _ = services.AddSingleton(mockParameterProvider);

																				  // Mock IAskAiGateway to avoid external AI service calls
																				  var mockAskAiGateway = A.Fake<IAskAiGateway<Stream>>();
																				  A.CallTo(() => mockAskAiGateway.AskAi(A<AskAiRequest>._, A<Cancel>._))
																					  .ReturnsLazily(() =>
																					  {
																						  var stream = new MemoryStream(Encoding.UTF8.GetBytes("data: test\n\n"));
																						  _mockMemoryStreams.Add(stream);
																						  return Task.FromResult<Stream>(stream);
																					  });
																				  _ = services.AddSingleton(mockAskAiGateway);

																				  // Mock IStreamTransformer
																				  var mockTransformer = A.Fake<IStreamTransformer>();
																				  A.CallTo(() => mockTransformer.AgentProvider).Returns("test-provider");
																				  A.CallTo(() => mockTransformer.AgentId).Returns("test-agent");
																				  A.CallTo(() => mockTransformer.TransformAsync(A<Stream>._, A<string?>._, A<Activity?>._, A<Cancel>._))
																					  .ReturnsLazily((Stream s, string? _, Activity? activity, Cancel _) =>
																					  {
																						  // Dispose the activity if provided (simulating what the real transformer does)
																						  activity?.Dispose();
																						  return Task.FromResult(s);
																					  });
																				  _ = services.AddSingleton(mockTransformer);

																				  // Allow tests to override services - RemoveAll + Add to properly replace
																				  _configureServices?.Invoke(services);
																			  });

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			foreach (var stream in _mockMemoryStreams)
			{
				stream.Dispose();
			}
			_mockMemoryStreams.Clear();
		}
		base.Dispose(disposing);
	}
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
