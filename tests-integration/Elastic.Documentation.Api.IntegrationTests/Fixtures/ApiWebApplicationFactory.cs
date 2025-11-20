// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.Aws;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for testing the API with mocked services.
/// This fixture can be reused across multiple test classes.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
	public List<Activity> ExportedActivities { get; } = [];
	public List<TestLogEntry> LogEntries { get; } = [];
	private readonly List<MemoryStream> _mockMemoryStreams = [];
	protected override void ConfigureWebHost(IWebHostBuilder builder) =>
		builder.ConfigureServices(services =>
		{
			// Configure OpenTelemetry with in-memory exporter for testing
			// Uses the same production configuration via AddDocsApiTracing()
			_ = services.AddOpenTelemetry()
				.WithTracing(tracing =>
				{
					_ = tracing
						.AddDocsApiTracing() // Reuses production configuration
						.AddInMemoryExporter(ExportedActivities);
				});

			// Configure logging to capture log entries
			_ = services.AddLogging(logging =>
			{
				_ = logging.AddProvider(new TestLoggerProvider(LogEntries));
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
/// Test logger provider for capturing log entries with scopes.
/// </summary>
internal sealed class TestLoggerProvider(List<TestLogEntry> logEntries) : ILoggerProvider
{
	private readonly List<object> _sharedScopes = [];

	public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, logEntries, _sharedScopes);
	public void Dispose() { }
}

/// <summary>
/// Test logger that captures log entries with their scopes.
/// </summary>
internal sealed class TestLogger(string categoryName, List<TestLogEntry> logEntries, List<object> sharedScopes) : ILogger
{
	public IDisposable BeginScope<TState>(TState state) where TState : notnull
	{
		sharedScopes.Add(state);
		return new ScopeDisposable(sharedScopes, state);
	}

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		var entry = new TestLogEntry
		{
			CategoryName = categoryName,
			LogLevel = logLevel,
			Message = formatter(state, exception),
			Exception = exception,
			Scopes = [.. sharedScopes]
		};
		logEntries.Add(entry);
	}

	private sealed class ScopeDisposable(List<object> scopes, object state) : IDisposable
	{
		public void Dispose() => scopes.Remove(state);
	}
}

/// <summary>
/// Represents a captured log entry with its scopes.
/// </summary>
public sealed class TestLogEntry
{
	public required string CategoryName { get; init; }
	public LogLevel LogLevel { get; init; }
	public required string Message { get; init; }
	public Exception? Exception { get; init; }
	public List<object> Scopes { get; init; } = [];
}
