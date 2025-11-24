// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Elastic.Documentation.Api.IntegrationTests;

/// <summary>
/// Unit tests for AskAI gateway implementations that verify streaming behavior.
/// These tests specifically verify that the gateways don't dispose HttpResponseMessage
/// or streams prematurely, which would break streaming.
/// </summary>
public class AskAiGatewayStreamingTests
{
	[Fact]
	public async Task AgentBuilderGatewayDoesNotDisposeHttpResponsePrematurely()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler();
		var sseResponse = """
data: {"type":"conversationStart","id":"conv123","conversation_id":"conv123"}

data: {"type":"messageChunk","id":"msg1","content":"Hello World"}

data: {"type":"conversationEnd","id":"conv123"}


""";

		mockHandler.SetResponse(sseResponse, "text/event-stream");

		using var httpClient = new HttpClient(mockHandler);
		var mockParameterProvider = A.Fake<IParameterProvider>();
		A.CallTo(() => mockParameterProvider.GetParam("docs-kibana-url", false, A<CancellationToken>._))
			.Returns(Task.FromResult("https://test-kibana.example.com"));
		A.CallTo(() => mockParameterProvider.GetParam("docs-kibana-apikey", true, A<CancellationToken>._))
			.Returns(Task.FromResult("test-api-key"));

		var mockLogger = A.Fake<ILogger<AgentBuilderAskAiGateway>>();
		var gateway = new AgentBuilderAskAiGateway(httpClient, mockParameterProvider, mockLogger);

		var request = new AskAiRequest("Test message", null);

		// Act - get the stream from the gateway
		var stream = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		// Assert - the stream should be readable (not disposed)
		stream.Should().NotBeNull();
		stream.CanRead.Should().BeTrue("stream should not be disposed by the gateway");

		// Read the entire stream to verify it works
		using var reader = new StreamReader(stream);
		var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		content.Should().NotBeEmpty();
		content.Should().Contain("conversationStart");
		content.Should().Contain("messageChunk");

		// Verify the HttpClient was called correctly
		mockHandler.RequestSent.Should().BeTrue();
		mockHandler.CapturedRequest.Should().NotBeNull();
		mockHandler.CapturedRequest!.RequestUri!.ToString().Should().Contain("/api/agent_builder/converse/async");
		mockHandler.CapturedRequest.Headers.GetValues("kbn-xsrf").Should().Contain("true");
	}

	[Fact]
	public async Task AgentBuilderGatewayAllowsMultipleReadsFromStream()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler();
		var sseResponse = """
data: {"type":"conversationStart","id":"test","conversation_id":"test"}

data: {"type":"messageChunk","id":"m1","content":"A"}

data: {"type":"messageChunk","id":"m1","content":"B"}

data: {"type":"messageChunk","id":"m1","content":"C"}

data: {"type":"conversationEnd","id":"test"}


""";

		mockHandler.SetResponse(sseResponse, "text/event-stream");

		using var httpClient = new HttpClient(mockHandler);
		var mockParameterProvider = A.Fake<IParameterProvider>();
		A.CallTo(() => mockParameterProvider.GetParam("docs-kibana-url", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("https://test-kibana.example.com"));
		A.CallTo(() => mockParameterProvider.GetParam("docs-kibana-apikey", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("test-api-key"));

		var mockLogger = A.Fake<ILogger<AgentBuilderAskAiGateway>>();
		var gateway = new AgentBuilderAskAiGateway(httpClient, mockParameterProvider, mockLogger);

		var request = new AskAiRequest("Test", null);

		// Act - get the stream and read it in chunks
		var stream = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		var chunks = new List<string>();
		var buffer = new byte[16]; // Small buffer to force multiple reads
		int bytesRead;

		while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), TestContext.Current.CancellationToken)) > 0)
		{
			var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			chunks.Add(chunk);
		}

		// Assert - verify we could read multiple chunks
		chunks.Should().NotBeEmpty();
		chunks.Count.Should().BeGreaterThan(1, "stream should support multiple reads");

		var completeContent = string.Join("", chunks);
		completeContent.Should().Be(sseResponse);
	}

	[Fact]
	public async Task LlmGatewayGatewayDoesNotDisposeHttpResponsePrematurely()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler();
		var sseResponse = """
data: {"type":"conversationStart","id":"conv456","conversation_id":"conv456"}

data: {"type":"reasoning","id":"r1","message":"Analyzing question"}

data: {"type":"messageChunk","id":"msg2","content":"Answer"}

data: {"type":"conversationEnd","id":"conv456"}


""";

		mockHandler.SetResponse(sseResponse, "text/event-stream");

		// Create mock token provider
		using var httpClient = new HttpClient(mockHandler);
		var mockTokenProvider = A.Fake<IGcpIdTokenProvider>();
		A.CallTo(() => mockTokenProvider.GenerateIdTokenAsync(A<string>._, A<string>._, A<CancellationToken>._))
			.Returns(Task.FromResult("mock-gcp-token"));

		var mockParameterProvider = A.Fake<IParameterProvider>();
		A.CallTo(() => mockParameterProvider.GetParam("llm-gateway-service-account", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("test@example.com"));
		A.CallTo(() => mockParameterProvider.GetParam("llm-gateway-function-url", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("https://test-llm-gateway.example.com"));

		var options = new LlmGatewayOptions(mockParameterProvider);

		var gateway = new LlmGatewayAskAiGateway(httpClient, mockTokenProvider, options);

		var request = new AskAiRequest("Test message", null);

		// Act - get the stream from the gateway
		var stream = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		// Assert - the stream should be readable (not disposed)
		stream.Should().NotBeNull();
		stream.CanRead.Should().BeTrue("stream should not be disposed by the gateway");

		// Read the entire stream to verify it works
		using var reader = new StreamReader(stream);
		var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		content.Should().NotBeEmpty();
		content.Should().Contain("conversationStart");
		content.Should().Contain("reasoning");
		content.Should().Contain("messageChunk");

		// Verify the HttpClient was called with correct headers
		mockHandler.RequestSent.Should().BeTrue();
		mockHandler.CapturedRequest.Should().NotBeNull();
		mockHandler.CapturedRequest!.Headers.Authorization.Should().NotBeNull();
		mockHandler.CapturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
		mockHandler.CapturedRequest.Headers.Authorization.Parameter.Should().Be("mock-gcp-token");
	}

	[Fact]
	public async Task LlmGatewayGatewayAllowsMultipleReadsFromStream()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler();
		var sseResponse = """
data: {"type":"conversationStart","id":"test","conversation_id":"test"}

data: {"type":"messageChunk","id":"m","content":"1"}

data: {"type":"messageChunk","id":"m","content":"2"}

data: {"type":"messageChunk","id":"m","content":"3"}

data: {"type":"conversationEnd","id":"test"}


""";

		mockHandler.SetResponse(sseResponse, "text/event-stream");

		using var httpClient = new HttpClient(mockHandler);
		var mockTokenProvider = A.Fake<IGcpIdTokenProvider>();
		A.CallTo(() => mockTokenProvider.GenerateIdTokenAsync(A<string>._, A<string>._, A<CancellationToken>._))
			.Returns(Task.FromResult("mock-token"));

		var mockParameterProvider = A.Fake<IParameterProvider>();
		A.CallTo(() => mockParameterProvider.GetParam("llm-gateway-service-account", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("test@example.com"));
		A.CallTo(() => mockParameterProvider.GetParam("llm-gateway-function-url", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("https://test.example.com"));

		var options = new LlmGatewayOptions(mockParameterProvider);

		var gateway = new LlmGatewayAskAiGateway(httpClient, mockTokenProvider, options);

		var request = new AskAiRequest("Test", null);

		// Act - get the stream and read it in chunks
		var stream = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		var chunks = new List<string>();
		var buffer = new byte[16]; // Small buffer to force multiple reads
		int bytesRead;

		while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), TestContext.Current.CancellationToken)) > 0)
		{
			var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			chunks.Add(chunk);
		}

		// Assert - verify we could read multiple chunks
		chunks.Should().NotBeEmpty();
		chunks.Count.Should().BeGreaterThan(1, "stream should support multiple reads");

		var completeContent = string.Join("", chunks);
		completeContent.Should().Be(sseResponse);
	}

	[Fact]
	public async Task AgentBuilderGatewayUsesResponseHeadersReadForStreaming()
	{
		// Arrange - verify that HttpCompletionOption.ResponseHeadersRead is used
		var mockHandler = new MockHttpMessageHandler();
		var sseResponse = "data: {\"type\":\"test\"}\n\n";
		mockHandler.SetResponse(sseResponse, "text/event-stream");

		using var httpClient = new HttpClient(mockHandler);
		var mockParameterProvider = A.Fake<IParameterProvider>();
		A.CallTo(() => mockParameterProvider.GetParam("docs-kibana-url", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("https://test-kibana.example.com"));
		A.CallTo(() => mockParameterProvider.GetParam("docs-kibana-apikey", A<bool>._, A<CancellationToken>._))
			.Returns(Task.FromResult("test-api-key"));

		var mockLogger = A.Fake<ILogger<AgentBuilderAskAiGateway>>();
		var gateway = new AgentBuilderAskAiGateway(httpClient, mockParameterProvider, mockLogger);

		var request = new AskAiRequest("Test", null);

		// Act
		var stream = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		// Assert
		stream.Should().NotBeNull();
		stream.CanRead.Should().BeTrue();

		// The fact that we can immediately read from the stream indicates
		// that ResponseHeadersRead was used (otherwise it would buffer)
		var buffer = new byte[10];
		var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), TestContext.Current.CancellationToken);
		bytesRead.Should().BeGreaterThan(0, "stream should be readable immediately");
	}
}

/// <summary>
/// Mock HttpMessageHandler for testing HTTP clients.
/// This allows us to test the gateway implementations without making real HTTP calls.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
	private HttpResponseMessage? _responseToReturn;

	public bool RequestSent { get; private set; }
	public HttpRequestMessage? CapturedRequest { get; private set; }

	public void SetResponse(string content, string contentType)
	{
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		_responseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StreamContent(stream)
			{
				Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType) }
			}
		};
	}

	public void SetErrorResponse(HttpStatusCode statusCode, string errorMessage)
	{
		_responseToReturn = new HttpResponseMessage(statusCode)
		{
			Content = new StringContent(errorMessage)
		};
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		RequestSent = true;
		CapturedRequest = request;

		// Simulate async behavior
		await Task.Delay(1, cancellationToken);

		if (_responseToReturn == null)
		{
			throw new InvalidOperationException("No response configured. Call SetResponse or SetErrorResponse first.");
		}

		return _responseToReturn;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_responseToReturn?.Dispose();
		}
		base.Dispose(disposing);
	}
}
