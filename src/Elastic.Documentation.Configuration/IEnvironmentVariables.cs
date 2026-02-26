// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Abstracts access to environment variables for testability.
/// Allows unit tests to mock environment variable values without modifying actual environment.
/// </summary>
public interface IEnvironmentVariables
{
	/// <summary>
	/// Gets the value of an environment variable.
	/// </summary>
	/// <param name="name">The name of the environment variable.</param>
	/// <returns>The value of the environment variable, or null if not set.</returns>
	string? GetEnvironmentVariable(string name);

	/// <summary>
	/// Indicates whether the current process is running in a CI environment.
	/// Checks for the presence of the GITHUB_ACTIONS environment variable.
	/// </summary>
	bool IsRunningOnCI { get; }

	/// <summary>
	/// API route prefix (e.g. /docs/_api for assembler, /api for codex).
	/// Reads DOCS_API_PREFIX, defaults to /docs/_api.
	/// </summary>
	string ApiPrefix => GetEnvironmentVariable("DOCS_API_PREFIX") ?? "/docs/_api";

	/// <summary>
	/// MCP route prefix (e.g. /docs/_mcp for assembler, /mcp for codex).
	/// Reads DOCS_MCP_PREFIX, defaults to /docs/_mcp.
	/// </summary>
	string McpPrefix => GetEnvironmentVariable("DOCS_MCP_PREFIX") ?? "/docs/_mcp";

	/// <summary>
	/// When true, MCP Bearer auth middleware validates JWTs. Default false.
	/// Reads MCP_AUTH_ENABLED (accepts "true", "1").
	/// </summary>
	bool McpAuthEnabled => string.Equals(GetEnvironmentVariable("MCP_AUTH_ENABLED"), "true", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(GetEnvironmentVariable("MCP_AUTH_ENABLED"), "1", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// RSA public key (PEM) for MCP JWT validation. When unset, auth middleware is disabled.
	/// Reads MCP_JWT_PUBLIC_KEY.
	/// </summary>
	string? McpJwtPublicKey => GetEnvironmentVariable("MCP_JWT_PUBLIC_KEY");

	/// <summary>
	/// OAuth issuer URL for MCP (e.g. https://codex.elastic.dev/mcp). When unset, .well-known endpoints are not mapped.
	/// Reads MCP_OAUTH_ISSUER.
	/// </summary>
	string? McpOAuthIssuer => GetEnvironmentVariable("MCP_OAUTH_ISSUER");

	/// <summary>
	/// Key ID for MCP JWT kid validation. Reads MCP_JWT_KEY_ID.
	/// </summary>
	string? McpJwtKeyId => GetEnvironmentVariable("MCP_JWT_KEY_ID");

	/// <summary>
	/// Comma-separated allowed email domains for sub claim (e.g. elastic.co). Defaults to elastic.co.
	/// Reads MCP_ALLOWED_EMAIL_DOMAINS.
	/// </summary>
	string McpAllowedEmailDomains => GetEnvironmentVariable("MCP_ALLOWED_EMAIL_DOMAINS") ?? "elastic.co";
}
