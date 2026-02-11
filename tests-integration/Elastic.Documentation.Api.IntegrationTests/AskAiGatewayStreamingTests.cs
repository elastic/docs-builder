// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
	private static IConfiguration CreateTestConfiguration(Dictionary<string, string?> values) =>
		new ConfigurationBuilder()
			.AddInMemoryCollection(values)
			.Build();

	[Fact]
	public async Task AgentBuilderGatewayDoesNotDisposeHttpResponsePrematurely()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler();
		var sseResponse = """
data: {"type":"conversationStart","id":"test","conversation_id":"test"}

data: {"type":"messageChunk","id":"m1","content":"Hello"}

data: {"type":"messageChunk","id":"m1","content":" World"}

data: {"type":"conversationEnd","id":"test"}


""";

		mockHandler.SetResponse(sseResponse, "text/event-stream");

		using var httpClient = new HttpClient(mockHandler);
		var kibanaOptions = new KibanaOptions(CreateTestConfiguration(new Dictionary<string, string?>
		{
			["DOCUMENTATION_KIBANA_URL"] = "https://test-kibana.example.com",
			["DOCUMENTATION_KIBANA_APIKEY"] = "test-api-key"
		}));

		var mockLogger = A.Fake<ILogger<AgentBuilderAskAiGateway>>();
		var gateway = new AgentBuilderAskAiGateway(httpClient, kibanaOptions, mockLogger);

		var request = new AskAiRequest("Test message", null);

		// Act - get the response from the gateway
		var response = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		// Assert - the stream should be readable (not disposed)
		response.Should().NotBeNull();
		response.Stream.CanRead.Should().BeTrue("stream should not be disposed by the gateway");

		// Read the entire stream to verify it works
		using var reader = new StreamReader(response.Stream);
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
		var kibanaOptions = new KibanaOptions(CreateTestConfiguration(new Dictionary<string, string?>
		{
			["DOCUMENTATION_KIBANA_URL"] = "https://test-kibana.example.com",
			["DOCUMENTATION_KIBANA_APIKEY"] = "test-api-key"
		}));

		var mockLogger = A.Fake<ILogger<AgentBuilderAskAiGateway>>();
		var gateway = new AgentBuilderAskAiGateway(httpClient, kibanaOptions, mockLogger);

		var request = new AskAiRequest("Test", null);

		// Act - get the response and read it in chunks
		var response = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		var chunks = new List<string>();
		var buffer = new byte[16]; // Small buffer to force multiple reads
		int bytesRead;

		while ((bytesRead = await response.Stream.ReadAsync(buffer.AsMemory(0, buffer.Length), TestContext.Current.CancellationToken)) > 0)
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
	public async Task LlmGatewayDoesNotDisposeHttpResponsePrematurely()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler();
		var sseResponse = """
data: {"type":"conversationStart","id":"test","conversation_id":"test"}

data: {"type":"reasoning","content":"thinking..."}

data: {"type":"messageChunk","id":"m1","content":"Response"}

data: {"type":"conversationEnd","id":"test"}


""";

		mockHandler.SetResponse(sseResponse, "text/event-stream");

		using var httpClient = new HttpClient(mockHandler);
		var mockTokenProvider = A.Fake<IGcpIdTokenProvider>();
		A.CallTo(() => mockTokenProvider.GenerateIdTokenAsync(A<string>._, A<string>._, A<CancellationToken>._))
			.Returns(Task.FromResult("mock-gcp-token"));

		var options = new LlmGatewayOptions(CreateTestConfiguration(new Dictionary<string, string?>
		{
			["LLM_GATEWAY_FUNCTION_URL"] = "https://test-llm-gateway.example.com",
			["LLM_GATEWAY_SERVICE_ACCOUNT"] = "test@example.com"
		}));

		var gateway = new LlmGatewayAskAiGateway(httpClient, mockTokenProvider, options);

		var request = new AskAiRequest("Test message", null);

		// Act - get the response from the gateway
		var response = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		// Assert - the stream should be readable (not disposed)
		response.Should().NotBeNull();
		response.Stream.CanRead.Should().BeTrue("stream should not be disposed by the gateway");

		// Read the entire stream to verify it works
		using var reader = new StreamReader(response.Stream);
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

		var options = new LlmGatewayOptions(CreateTestConfiguration(new Dictionary<string, string?>
		{
			["LLM_GATEWAY_FUNCTION_URL"] = "https://test.example.com",
			["LLM_GATEWAY_SERVICE_ACCOUNT"] = "test@example.com"
		}));

		var gateway = new LlmGatewayAskAiGateway(httpClient, mockTokenProvider, options);

		var request = new AskAiRequest("Test", null);

		// Act - get the response and read it in chunks
		var response = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		var chunks = new List<string>();
		var buffer = new byte[16]; // Small buffer to force multiple reads
		int bytesRead;

		while ((bytesRead = await response.Stream.ReadAsync(buffer.AsMemory(0, buffer.Length), TestContext.Current.CancellationToken)) > 0)
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
		var kibanaOptions = new KibanaOptions(CreateTestConfiguration(new Dictionary<string, string?>
		{
			["DOCUMENTATION_KIBANA_URL"] = "https://test-kibana.example.com",
			["DOCUMENTATION_KIBANA_APIKEY"] = "test-api-key"
		}));

		var mockLogger = A.Fake<ILogger<AgentBuilderAskAiGateway>>();
		var gateway = new AgentBuilderAskAiGateway(httpClient, kibanaOptions, mockLogger);

		var request = new AskAiRequest("Test", null);

		// Act
		var response = await gateway.AskAi(request, TestContext.Current.CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Stream.CanRead.Should().BeTrue();

		// The fact that we can immediately read from the stream indicates
		// that ResponseHeadersRead was used (otherwise it would buffer)
		var buffer = new byte[10];
		var bytesRead = await response.Stream.ReadAsync(buffer.AsMemory(0, buffer.Length), TestContext.Current.CancellationToken);
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

	public void SetErrorResponse(HttpStatusCode statusCode, string errorMessage) => _responseToReturn = new HttpResponseMessage(statusCode)
	{
		Content = new StringContent(errorMessage)
	};

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
