// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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
	protected override void ConfigureWebHost(IWebHostBuilder builder) =>
		builder.ConfigureServices(services =>
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
				.ReturnsLazily(() => {
					var stream = new MemoryStream(Encoding.UTF8.GetBytes("data: test\n\n"));
					MockMemoryStreams.Add(stream);
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
			foreach (var stream in MockMemoryStreams)
			{
				stream.Dispose();
			}
			MockMemoryStreams.Clear();
		}
		base.Dispose(disposing);
	}
}
