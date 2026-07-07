// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.SiteSearch.Cli.ContentStack;

internal sealed class ContentStackConfiguration
{
	public required string ApiKey { get; init; }
	public required string DeliveryToken { get; init; }
	public string Environment => "production";
	public Uri BaseUrl { get; init; } = new("https://cdn.contentstack.io");

	public static ContentStackConfiguration CreateFromEnvironment()
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets("docs-builder")
			.AddEnvironmentVariables()
			.Build();

		return CreateFromConfiguration(config);
	}

	/// <summary>
	/// Returns Contentstack credentials if both API key and delivery token are set; otherwise <c>null</c>
	/// (commands that talk to Contentstack must use <see cref="CreateFromConfiguration" /> or validate explicitly).
	/// </summary>
	public static ContentStackConfiguration? TryCreateFromConfiguration(IConfiguration config)
	{
		var apiKey = config["ContentStack:ApiKey"]
			?? config["CONTENTSTACK_API_KEY"];

		var deliveryToken = config["ContentStack:DeliveryToken"]
			?? config["CONTENTSTACK_DELIVERY_TOKEN"];

		if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deliveryToken))
			return null;

		return new ContentStackConfiguration
		{
			ApiKey = apiKey,
			DeliveryToken = deliveryToken
		};
	}

	public static ContentStackConfiguration CreateFromConfiguration(IConfiguration config)
	{
		var parsed = TryCreateFromConfiguration(config);
		if (parsed is not null)
			return parsed;

		var apiKey = config["ContentStack:ApiKey"] ?? config["CONTENTSTACK_API_KEY"];
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new InvalidOperationException(
				"Contentstack API key not found. Set 'ContentStack:ApiKey' via dotnet user-secrets " +
				"or the CONTENTSTACK_API_KEY environment variable.");

		throw new InvalidOperationException(
			"Contentstack delivery token not found. Set 'ContentStack:DeliveryToken' via dotnet user-secrets " +
			"or the CONTENTSTACK_DELIVERY_TOKEN environment variable.");
	}
}
