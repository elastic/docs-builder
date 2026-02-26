// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
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

		_ = group.MapGet("/.well-known/oauth-protected-resource", (HttpContext context) =>
		{
			context.Response.Headers.CacheControl = CacheControlValue;
			return Results.Json(
				new ProtectedResourceMetadata
				{
					Resource = issuer,
					AuthorizationServers = [issuer],
					ScopesSupported = ScopesSupported,
					BearerMethodsSupported = BearerMethodsSupported
				},
				OAuthMetadataJsonContext.Default.ProtectedResourceMetadata
			);
		});

		_ = group.MapGet("/.well-known/openid-configuration", (HttpContext context) =>
		{
			context.Response.Headers.CacheControl = CacheControlValue;
			return Results.Json(
				new AuthorizationServerMetadata
				{
					Issuer = issuer,
					AuthorizationEndpoint = $"{issuer}/authorize",
					TokenEndpoint = $"{issuer}/token",
					RegistrationEndpoint = $"{issuer}/register",
					ResponseTypesSupported = ResponseTypesSupported,
					GrantTypesSupported = GrantTypesSupported,
					CodeChallengeMethodsSupported = CodeChallengeMethodsSupported,
					TokenEndpointAuthMethodsSupported = TokenEndpointAuthMethodsSupported,
					ScopesSupported = ScopesSupported
				},
				OAuthMetadataJsonContext.Default.AuthorizationServerMetadata
			);
		});
	}
}

/// <summary>RFC 9728 Protected Resource Metadata.</summary>
public sealed record ProtectedResourceMetadata
{
	[JsonPropertyName("resource")]
	public required string Resource { get; init; }

	[JsonPropertyName("authorization_servers")]
	public required string[] AuthorizationServers { get; init; }

	[JsonPropertyName("scopes_supported")]
	public required string[] ScopesSupported { get; init; }

	[JsonPropertyName("bearer_methods_supported")]
	public required string[] BearerMethodsSupported { get; init; }
}

/// <summary>OIDC Discovery / OAuth 2.0 Authorization Server Metadata.</summary>
public sealed record AuthorizationServerMetadata
{
	[JsonPropertyName("issuer")]
	public required string Issuer { get; init; }

	[JsonPropertyName("authorization_endpoint")]
	public required string AuthorizationEndpoint { get; init; }

	[JsonPropertyName("token_endpoint")]
	public required string TokenEndpoint { get; init; }

	[JsonPropertyName("registration_endpoint")]
	public required string RegistrationEndpoint { get; init; }

	[JsonPropertyName("response_types_supported")]
	public required string[] ResponseTypesSupported { get; init; }

	[JsonPropertyName("grant_types_supported")]
	public required string[] GrantTypesSupported { get; init; }

	[JsonPropertyName("code_challenge_methods_supported")]
	public required string[] CodeChallengeMethodsSupported { get; init; }

	[JsonPropertyName("token_endpoint_auth_methods_supported")]
	public required string[] TokenEndpointAuthMethodsSupported { get; init; }

	[JsonPropertyName("scopes_supported")]
	public required string[] ScopesSupported { get; init; }
}

[JsonSerializable(typeof(ProtectedResourceMetadata))]
[JsonSerializable(typeof(AuthorizationServerMetadata))]
internal sealed partial class OAuthMetadataJsonContext : JsonSerializerContext;
