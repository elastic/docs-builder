// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Aws;

public class LambdaExtensionParameterProvider(IHttpClientFactory httpClientFactory, ILogger<LambdaExtensionParameterProvider> logger) : IParameterProvider
{
	public const string HttpClientName = "AwsParametersAndSecretsLambdaExtensionClient";
	private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientName);

	public async Task<string> GetParam(string name, bool withDecryption = true, Cancel ctx = default)
	{
		try
		{
			logger.LogInformation("Retrieving parameter '{Name}' from Lambda Extension (SSM Parameter Store).", name);
			var response = await _httpClient.GetFromJsonAsync<ParameterResponse>($"/systemsmanager/parameters/get?name={Uri.EscapeDataString(name)}&withDecryption={withDecryption.ToString().ToLowerInvariant()}", AwsJsonContext.Default.ParameterResponse, ctx);
			return response?.Parameter?.Value ?? throw new InvalidOperationException($"Parameter value for '{name}' is null.");
		}
		catch (HttpRequestException httpEx)
		{
			logger.LogError(httpEx, "HTTP request failed for parameter '{Name}'. Status: {StatusCode}.", name, httpEx.StatusCode);
			throw;
		}
		catch (JsonException jsonEx)
		{
			logger.LogError(jsonEx, "JSON deserialization failed for parameter '{Name}'.", name);
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An unexpected error occurred while retrieving parameter '{Name}'.", name);
			throw;
		}
	}
}

internal sealed class ParameterResponse
{
	public Parameter? Parameter { get; set; }
}

internal sealed class Parameter
{
	public string? Arn { get; set; }
	public string? Name { get; set; }
	public string? Type { get; set; }
	public string? Value { get; set; }
	public string? Version { get; set; }
	public string? Selector { get; set; }
	public string? LastModifiedDate { get; set; }
	public string? LastModifiedUser { get; set; }
	public string? DataType { get; set; }
}


[JsonSerializable(typeof(ParameterResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AwsJsonContext : JsonSerializerContext;
