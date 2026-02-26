// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// Maps OAuth metadata endpoints required by the MCP authorization spec.
/// Serves RFC 9728 Protected Resource Metadata, OIDC Discovery (AS metadata),
/// and a JWKS endpoint under the MCP route prefix.
/// Only mapped when <c>MCP_OAUTH_ISSUER</c> is set.
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
	private static readonly string[] SubjectTypesSupported = ["public"];
	private static readonly string[] IdTokenSigningAlgValuesSupported = ["RS256"];

	/// <summary>Maps <c>.well-known</c> and JWKS endpoints onto the given route group.</summary>
	public static void MapEndpoints(RouteGroupBuilder group)
	{
		var env = SystemEnvironmentVariables.Instance;
		var issuer = env.McpOAuthIssuer!;
		var jwksJson = BuildJwksJson(env);

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
					JwksUri = $"{issuer}/jwks",
					ResponseTypesSupported = ResponseTypesSupported,
					GrantTypesSupported = GrantTypesSupported,
					CodeChallengeMethodsSupported = CodeChallengeMethodsSupported,
					TokenEndpointAuthMethodsSupported = TokenEndpointAuthMethodsSupported,
					ScopesSupported = ScopesSupported,
					SubjectTypesSupported = SubjectTypesSupported,
					IdTokenSigningAlgValuesSupported = IdTokenSigningAlgValuesSupported
				},
				OAuthMetadataJsonContext.Default.AuthorizationServerMetadata
			);
		});

		_ = group.MapGet("/jwks", (HttpContext context) =>
		{
			context.Response.Headers.CacheControl = CacheControlValue;
			return Results.Text(jwksJson, "application/json");
		});
	}

	private static string BuildJwksJson(IEnvironmentVariables env)
	{
		if (env.McpJwtPublicKey is null)
			return System.Text.Json.JsonSerializer.Serialize(new JwksDocument { Keys = [] }, OAuthMetadataJsonContext.Default.JwksDocument);

		using var rsa = RSA.Create();
		rsa.ImportFromPem(env.McpJwtPublicKey);
		var p = rsa.ExportParameters(false);

		var jwk = new JsonWebKey
		{
			Kty = "RSA",
			Use = "sig",
			Alg = "RS256",
			Kid = env.McpJwtKeyId ?? "default",
			N = Base64UrlEncode(p.Modulus!),
			E = Base64UrlEncode(p.Exponent!)
		};

		return System.Text.Json.JsonSerializer.Serialize(new JwksDocument { Keys = [jwk] }, OAuthMetadataJsonContext.Default.JwksDocument);
	}

	private static string Base64UrlEncode(byte[] data) =>
		Convert.ToBase64String(data)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
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

	[JsonPropertyName("jwks_uri")]
	public required string JwksUri { get; init; }

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

	[JsonPropertyName("subject_types_supported")]
	public required string[] SubjectTypesSupported { get; init; }

	[JsonPropertyName("id_token_signing_alg_values_supported")]
	public required string[] IdTokenSigningAlgValuesSupported { get; init; }
}

/// <summary>JWKS document containing public signing keys.</summary>
public sealed record JwksDocument
{
	[JsonPropertyName("keys")]
	public required JsonWebKey[] Keys { get; init; }
}

/// <summary>A single JWK entry.</summary>
public sealed record JsonWebKey
{
	[JsonPropertyName("kty")]
	public required string Kty { get; init; }

	[JsonPropertyName("use")]
	public required string Use { get; init; }

	[JsonPropertyName("alg")]
	public required string Alg { get; init; }

	[JsonPropertyName("kid")]
	public required string Kid { get; init; }

	[JsonPropertyName("n")]
	public required string N { get; init; }

	[JsonPropertyName("e")]
	public required string E { get; init; }
}

[JsonSerializable(typeof(ProtectedResourceMetadata))]
[JsonSerializable(typeof(AuthorizationServerMetadata))]
[JsonSerializable(typeof(JwksDocument))]
internal sealed partial class OAuthMetadataJsonContext : JsonSerializerContext;
