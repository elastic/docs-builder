// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// Maps OAuth metadata endpoints required by the MCP authorization spec.
/// Serves both RFC 9728 Protected Resource Metadata and OIDC Discovery (AS metadata)
/// under the MCP route prefix. Only mapped when <c>MCP_OAUTH_ISSUER</c> is set.
/// </summary>
public static class McpOAuthMetadata
{
	private const string CacheControlValue = "public, max-age=3600";

	private static readonly string[] ScopesSupported = ["mcp:read"];
	private static readonly string[] BearerMethodsSupported = ["header"];
	private static readonly string[] ResponseTypesSupported = ["code"];
	private static readonly string[] GrantTypesSupported = ["authorization_code"];
	private static readonly string[] CodeChallengeMethodsSupported = ["S256"];
	private static readonly string[] TokenEndpointAuthMethodsSupported = ["none"];

	/// <summary>Maps both <c>.well-known</c> endpoints onto the given route group.</summary>
	public static void MapEndpoints(RouteGroupBuilder group)
	{
		var env = SystemEnvironmentVariables.Instance;
		var issuer = env.McpOAuthIssuer!;
		var mcpPrefix = env.McpPrefix;

		_ = group.MapGet("/.well-known/oauth-protected-resource", (HttpContext context) =>
		{
			var host = context.Request.Host.Value;
			var scheme = context.Request.Scheme;
			var resource = $"{scheme}://{host}{mcpPrefix}";

			context.Response.Headers.CacheControl = CacheControlValue;
			return Results.Json(new
			{
				resource,
				authorization_servers = new[] { issuer },
				scopes_supported = ScopesSupported,
				bearer_methods_supported = BearerMethodsSupported
			});
		});

		_ = group.MapGet("/.well-known/openid-configuration", (HttpContext context) =>
		{
			var host = context.Request.Host.Value;
			var scheme = context.Request.Scheme;
			var baseUrl = $"{scheme}://{host}{mcpPrefix}";

			context.Response.Headers.CacheControl = CacheControlValue;
			return Results.Json(new
			{
				issuer,
				authorization_endpoint = $"{baseUrl}/authorize",
				token_endpoint = $"{baseUrl}/token",
				registration_endpoint = $"{baseUrl}/register",
				response_types_supported = ResponseTypesSupported,
				grant_types_supported = GrantTypesSupported,
				code_challenge_methods_supported = CodeChallengeMethodsSupported,
				token_endpoint_auth_methods_supported = TokenEndpointAuthMethodsSupported,
				scopes_supported = ScopesSupported
			});
		});
	}
}
