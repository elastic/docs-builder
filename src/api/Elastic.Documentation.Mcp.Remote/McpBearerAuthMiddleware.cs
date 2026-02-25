// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using Elastic.Documentation.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// AOT-compatible Bearer token validation middleware for MCP. Validates RS256 JWTs using
/// System.IdentityModel.Tokens.Jwt (AOT-compatible in v8.x). Skips when auth is disabled (no key set).
/// </summary>
public class McpBearerAuthMiddleware(RequestDelegate next, ILogger<McpBearerAuthMiddleware> logger)
{
	private const string McpUserKey = "McpUser";
	private const string ExpectedAlg = "RS256";
	private static readonly JwtSecurityTokenHandler TokenHandler = new();

	public async Task InvokeAsync(HttpContext context)
	{
		var env = SystemEnvironmentVariables.Instance;
		if (!env.McpAuthEnabled || env.McpJwtPublicKey is null)
		{
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
			await WriteUnauthorizedAsync(context, mcpPrefix);
			return;
		}

		var token = authHeader["Bearer ".Length..].Trim();
		if (string.IsNullOrEmpty(token))
		{
			await WriteUnauthorizedAsync(context, mcpPrefix);
			return;
		}

		var (user, errorStatusCode) = ValidateToken(token, env, context);
		if (user is not null)
		{
			context.Items[McpUserKey] = user;
			await next(context);
			return;
		}

		if (errorStatusCode == 403)
		{
			context.Response.StatusCode = 403;
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("""{"error":"forbidden"}""");
			return;
		}

		context.Response.StatusCode = 401;
		context.Response.ContentType = "application/json";
		await context.Response.WriteAsync("""{"error":"invalid_token"}""");
	}

	private static async Task WriteUnauthorizedAsync(HttpContext context, string mcpPrefix)
	{
		var host = context.Request.Host.Value;
		var scheme = context.Request.Scheme;
		var resourceMetadata = $"{scheme}://{host}{mcpPrefix}/.well-known/oauth-protected-resource";
		context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{resourceMetadata}\"";
		context.Response.StatusCode = 401;
		context.Response.ContentType = "application/json";
		await context.Response.WriteAsync("""{"error":"invalid_token"}""");
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
		catch
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

		RSA rsa;
		try
		{
			rsa = RSA.Create();
			rsa.ImportFromPem(env.McpJwtPublicKey);
		}
		catch
		{
			LogValidationFailure("invalid_public_key");
			return (null, 401);
		}

		var host = context.Request.Host.Value;
		var scheme = context.Request.Scheme;
		var expectedAud = $"{scheme}://{host}{env.McpPrefix}";

		var validationParams = new TokenValidationParameters
		{
			IssuerSigningKey = new RsaSecurityKey(rsa),
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
		catch (SecurityTokenInvalidSignatureException)
		{
			LogValidationFailure("signature_invalid");
			return (null, 401);
		}
		catch (SecurityTokenException)
		{
			LogValidationFailure("validation_failed");
			return (null, 401);
		}
		finally
		{
			rsa.Dispose();
		}
	}

	private void LogValidationFailure(string reason) =>
		logger.LogDebug("MCP auth validation failed: {Reason}", reason);
}
