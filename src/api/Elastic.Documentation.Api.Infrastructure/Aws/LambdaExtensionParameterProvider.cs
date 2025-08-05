// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Aws;

public class LambdaExtensionParameterProvider(IHttpClientFactory httpClientFactory, AppEnvironment appEnvironment, ILogger<LambdaExtensionParameterProvider> logger) : IParameterProvider
{
	public const string HttpClientName = "AwsParametersAndSecretsLambdaExtensionClient";
	private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientName);

	public async Task<string> GetParam(string name, bool withDecryption = true, Cancel ctx = default)
	{
		try
		{
			var prefix = $"/elastic-docs-v3/{appEnvironment.Current.ToStringFast(true)}/";
			var prefixedName = prefix + name.TrimStart('/');
			logger.LogInformation("Retrieving parameter '{Name}' from Lambda Extension (SSM Parameter Store).", prefixedName);
			var response = await _httpClient.GetFromJsonAsync<ParameterResponse>($"/systemsmanager/parameters/get?name={Uri.EscapeDataString(prefixedName)}&withDecryption={withDecryption.ToString().ToLowerInvariant()}", AwsJsonContext.Default.ParameterResponse, ctx);
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
	public required Parameter Parameter { get; set; }
}

internal sealed class Parameter
{
	[JsonPropertyName("ARN")]
	public required string Arn { get; set; }
	public required string Name { get; set; }
	public required string Type { get; set; }
	public required string Value { get; set; }
	public required int Version { get; set; }
	public string? Selector { get; set; }
	public DateTime LastModifiedDate { get; set; }
	public required string DataType { get; set; }
}


[JsonSerializable(typeof(ParameterResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
internal sealed partial class AwsJsonContext : JsonSerializerContext;
