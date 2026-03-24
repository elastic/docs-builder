// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Elastic.Documentation.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// AOT-compatible Bearer token validation middleware for MCP. Validates RS256 JWTs using
/// System.IdentityModel.Tokens.Jwt (AOT-compatible in v8.x). Skips when auth is disabled (no key set).
/// </summary>
public class McpBearerAuthMiddleware(RequestDelegate next, ILogger<McpBearerAuthMiddleware> logger)
{
	private const string McpUserKey = "McpUser";
	private const string ExpectedAlg = "RS256";
	private static readonly JwtSecurityTokenHandler TokenHandler = new() { MapInboundClaims = false };

	public async Task InvokeAsync(HttpContext context)
	{
		var env = SystemEnvironmentVariables.Instance;
		if (!env.McpAuthEnabled || env.McpJwtPublicKey is null)
		{
			logger.LogDebug("MCP auth: skipped (enabled={Enabled}, hasKey={HasKey})", env.McpAuthEnabled, env.McpJwtPublicKey is not null);
			await next(context);
			return;
		}

		var path = context.Request.Path;
		var mcpPrefix = env.McpPrefix;
		if (!path.StartsWithSegments(mcpPrefix, StringComparison.OrdinalIgnoreCase))
		{
			await next(context);
			return;
		}

		var pathValue = path.Value ?? "";
		if (pathValue.Contains("/.well-known", StringComparison.Ordinal) ||
			pathValue.EndsWith("/health", StringComparison.Ordinal) ||
			pathValue.EndsWith("/alive", StringComparison.Ordinal))
		{
			await next(context);
			return;
		}

		var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
		if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
		{
			logger.LogWarning("MCP auth: no Bearer token in Authorization header for {Path}", pathValue);
			await WriteUnauthorizedAsync(context, env);
			return;
		}

		var token = authHeader["Bearer ".Length..].Trim();
		if (string.IsNullOrEmpty(token))
		{
			logger.LogWarning("MCP auth: empty Bearer token for {Path}", pathValue);
			await WriteUnauthorizedAsync(context, env);
			return;
		}

		var tokenPrefix = token.Length > 20 ? token[..20] : token;
		var (user, errorStatusCode) = ValidateToken(token, env, context);
		if (user is not null)
		{
			logger.LogInformation("MCP auth: authenticated user {User} for {Path} (token={TokenPrefix}...)", user, pathValue, tokenPrefix);
			context.Items[McpUserKey] = user;
			await next(context);
			return;
		}

		if (errorStatusCode == 403)
		{
			context.Response.StatusCode = 403;
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(/*lang=json,strict*/ """{"error":"forbidden"}""");
			return;
		}

		context.Response.StatusCode = 401;
		context.Response.ContentType = "application/json";
		await context.Response.WriteAsync(/*lang=json,strict*/ """{"error":"invalid_token"}""");
	}

	private static async Task WriteUnauthorizedAsync(HttpContext context, IEnvironmentVariables env)
	{
		var resourceMetadata = env.McpOAuthIssuer is not null
			? $"{env.McpOAuthIssuer}/.well-known/oauth-protected-resource"
			: $"{context.Request.Scheme}://{context.Request.Host.Value}{env.McpPrefix}/.well-known/oauth-protected-resource";
		context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{resourceMetadata}\"";
		context.Response.StatusCode = 401;
		context.Response.ContentType = "application/json";
		await context.Response.WriteAsync(/*lang=json,strict*/ """{"error":"invalid_token"}""");
	}

	private (string? User, int? ErrorStatusCode) ValidateToken(string token, IEnvironmentVariables env, HttpContext context)
	{
		if (env.McpJwtPublicKey is null)
			return (null, 401);

		JwtSecurityToken? jwt;
		try
		{
			jwt = TokenHandler.ReadJwtToken(token);
		}
		catch (ArgumentException)
		{
			LogValidationFailure("invalid_token_format");
			return (null, 401);
		}
		catch (SecurityTokenException)
		{
			LogValidationFailure("invalid_token_format");
			return (null, 401);
		}

		if (jwt.Header.Alg != ExpectedAlg)
		{
			LogValidationFailure("alg_mismatch");
			return (null, 401);
		}

		if (env.McpJwtKeyId is not null && jwt.Header.Kid != env.McpJwtKeyId)
		{
			LogValidationFailure("kid_mismatch");
			return (null, 401);
		}

		RSAParameters rsaParams;
		try
		{
			using var rsa = RSA.Create();
			rsa.ImportFromPem(env.McpJwtPublicKey);
			rsaParams = rsa.ExportParameters(includePrivateParameters: false);
		}
		catch (CryptographicException)
		{
			LogValidationFailure("invalid_public_key");
			return (null, 401);
		}
		catch (ArgumentException)
		{
			LogValidationFailure("invalid_public_key");
			return (null, 401);
		}

		var expectedAud = env.McpOAuthIssuer ?? $"{context.Request.Scheme}://{context.Request.Host.Value}{env.McpPrefix}";

		var validationParams = new TokenValidationParameters
		{
			IssuerSigningKey = new RsaSecurityKey(rsaParams) { KeyId = env.McpJwtKeyId },
			ValidateIssuerSigningKey = true,
			ValidateIssuer = env.McpOAuthIssuer is not null,
			ValidIssuer = env.McpOAuthIssuer,
			ValidateAudience = true,
			ValidAudience = expectedAud,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.Zero
		};

		try
		{
			var principal = TokenHandler.ValidateToken(token, validationParams, out _);
			var sub = principal.FindFirst("sub")?.Value;
			if (string.IsNullOrEmpty(sub))
			{
				LogValidationFailure("sub_missing");
				return (null, 401);
			}

			var scope = principal.FindFirst("scope")?.Value;
			if (string.IsNullOrEmpty(scope) || !scope.Contains("mcp:read", StringComparison.Ordinal))
			{
				LogValidationFailure("scope_missing");
				return (null, 401);
			}

			var allowedDomains = env.McpAllowedEmailDomains.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var domainAllowed = allowedDomains.Length == 0 ||
				allowedDomains.Any(d => sub.EndsWith("@" + d.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
			if (!domainAllowed)
			{
				logger.LogWarning("MCP auth rejected: token valid but sub {Sub} not in allowed domains", sub);
				return (null, 403);
			}

			return (sub, null);
		}
		catch (SecurityTokenExpiredException)
		{
			LogValidationFailure("expired");
			return (null, 401);
		}
		catch (SecurityTokenInvalidSignatureException ex)
		{
			logger.LogWarning("MCP auth validation failed: signature_invalid (kid={Kid}, jti={Jti}, iss={Iss}, aud={Aud}, err={Err})",
				jwt.Header.Kid, jwt.Payload.Jti, jwt.Issuer, jwt.Audiences?.FirstOrDefault(), ex.Message);
			return (null, 401);
		}
		catch (SecurityTokenException ex)
		{
			logger.LogWarning("MCP auth validation failed: {Type} (kid={Kid}, jti={Jti}, err={Err})",
				ex.GetType().Name, jwt.Header.Kid, jwt.Payload.Jti, ex.Message);
			return (null, 401);
		}
	}

	private void LogValidationFailure(string reason) =>
		logger.LogWarning("MCP auth validation failed: {Reason}", reason);
}
