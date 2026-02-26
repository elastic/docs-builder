// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Default implementation of <see cref="IEnvironmentVariables"/> that reads from the system environment.
/// </summary>
public class SystemEnvironmentVariables : IEnvironmentVariables
{
	/// <summary>
	/// Singleton instance for use in production code and DI registration.
	/// </summary>
	public static readonly SystemEnvironmentVariables Instance = new();

	/// <inheritdoc />
	public string? GetEnvironmentVariable(string name) =>
		Environment.GetEnvironmentVariable(name);

	/// <inheritdoc />
	public bool IsRunningOnCI =>
		!string.IsNullOrEmpty(GetEnvironmentVariable("GITHUB_ACTIONS"));

	/// <inheritdoc />
	public string ApiPrefix => GetEnvironmentVariable("DOCS_API_PREFIX") ?? "/docs/_api";

	/// <inheritdoc />
	public string McpPrefix => GetEnvironmentVariable("DOCS_MCP_PREFIX") ?? "/docs/_mcp";

	/// <inheritdoc />
	public bool McpAuthEnabled =>
		string.Equals(GetEnvironmentVariable("MCP_AUTH_ENABLED"), "true", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(GetEnvironmentVariable("MCP_AUTH_ENABLED"), "1", StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc />
	public string? McpJwtPublicKey => GetEnvironmentVariable("MCP_JWT_PUBLIC_KEY");

	/// <inheritdoc />
	public string? McpOAuthIssuer => GetEnvironmentVariable("MCP_OAUTH_ISSUER");

	/// <inheritdoc />
	public string? McpJwtKeyId => GetEnvironmentVariable("MCP_JWT_KEY_ID");

	/// <inheritdoc />
	public string McpAllowedEmailDomains => GetEnvironmentVariable("MCP_ALLOWED_EMAIL_DOMAINS") ?? "elastic.co";
}
