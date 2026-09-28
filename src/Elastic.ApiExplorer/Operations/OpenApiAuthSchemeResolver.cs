// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

public record AuthSchemeBadge(string Id, string PillLabel, string Href);

public static class OpenApiAuthSchemeResolver
{
	public static IReadOnlyList<AuthSchemeBadge> Resolve(
		OpenApiOperation operation,
		OpenApiDocument document,
		string authenticationUrl = ""
	)
	{
		// Omitted operation security is null (inherit). An empty list is an explicit override to none.
		var requirements = operation.Security ?? document.Security;
		if (requirements is not { Count: > 0 })
			return [];

		var seen = new HashSet<string>(StringComparer.Ordinal);
		var badges = new List<AuthSchemeBadge>();
		foreach (var requirement in requirements)
		{
			foreach (var scheme in requirement)
			{
				if (scheme.Key is not OpenApiSecuritySchemeReference { Reference.Id: { Length: > 0 } id } || !seen.Add(id))
					continue;
				var label = LabelFor(Target(scheme.Key, document));
				if (label is null)
					continue;
				var pill = label is "Api key" or "Basic" or "Bearer" ? $"{label} auth" : label;
				var href = string.IsNullOrEmpty(authenticationUrl) ? "" : $"{authenticationUrl}#{id.ToLowerInvariant()}";
				badges.Add(new AuthSchemeBadge(id, pill, href));
			}
		}

		return badges;
	}

	private static IOpenApiSecurityScheme? Target(IOpenApiSecurityScheme scheme, OpenApiDocument document)
	{
		if (
			scheme is OpenApiSecuritySchemeReference { Reference.Id: { Length: > 0 } id }
			&& document.Components?.SecuritySchemes?.TryGetValue(id, out var listed) == true
		)
			return listed;
		return scheme;
	}

	internal static string? LabelFor(IOpenApiSecurityScheme? scheme) => scheme?.Type switch
	{
		SecuritySchemeType.ApiKey => "Api key",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase) => "Basic",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) => "Bearer",
		SecuritySchemeType.Http when !string.IsNullOrEmpty(scheme.Scheme) => scheme.Scheme,
		SecuritySchemeType.OAuth2 => "OAuth 2.0",
		SecuritySchemeType.OpenIdConnect => "OpenID Connect",
		_ => null
	};
}
