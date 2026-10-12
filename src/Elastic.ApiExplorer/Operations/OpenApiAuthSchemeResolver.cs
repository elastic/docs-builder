// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

public record AuthScheme(string Id, string Label, string? CredentialExample, string? DescriptionMarkdown);

/// <summary>One way to authenticate. OpenAPI treats the schemes inside one security requirement as AND, so all of them are sent together.</summary>
public record AuthRequirement(IReadOnlyList<AuthScheme> Schemes)
{
	public string Label => string.Join(" + ", Schemes.Select(static s => s.Label));
}

public static class OpenApiAuthSchemeResolver
{
	/// <summary>The alternatives (OR) from the operation's security, or the document's when the operation has none. Each holds the schemes (AND) it needs.</summary>
	public static IReadOnlyList<AuthRequirement> Resolve(OpenApiOperation operation, OpenApiDocument document)
	{
		// Omitted operation security is null (inherit). An empty list is an explicit override to none.
		var requirements = operation.Security ?? document.Security;
		if (requirements is not { Count: > 0 })
			return [];

		var seen = new HashSet<string>(StringComparer.Ordinal);
		var alternatives = new List<AuthRequirement>();
		foreach (var requirement in requirements)
		{
			var schemes = SchemesOf(requirement, document);
			// The unit separator cannot be part of a scheme name, so {a, b} and {"a-b"} stay different alternatives.
			if (schemes.Count > 0 && seen.Add(string.Join('\u001f', schemes.Select(static s => s.Id))))
				alternatives.Add(new AuthRequirement(schemes));
		}

		return alternatives;
	}

	private static List<AuthScheme> SchemesOf(OpenApiSecurityRequirement requirement, OpenApiDocument document)
	{
		var schemes = new List<AuthScheme>();
		foreach (var scheme in requirement)
		{
			if (scheme.Key is not OpenApiSecuritySchemeReference { Reference.Id: { Length: > 0 } id })
				continue;
			var target = Target(scheme.Key, document);
			if (LabelFor(target) is not { } label)
				continue;
			var description = target?.Description;
			schemes.Add(new AuthScheme(id, label, CredentialExample(target), string.IsNullOrWhiteSpace(description) ? null : description));
		}

		return schemes;
	}

	internal static IOpenApiSecurityScheme? Target(IOpenApiSecurityScheme scheme, OpenApiDocument document)
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

	internal static string? CredentialExample(IOpenApiSecurityScheme? scheme) => scheme?.Type switch
	{
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Header && !string.IsNullOrEmpty(scheme.Name) =>
			$"{scheme.Name}: <value>",
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Query && !string.IsNullOrEmpty(scheme.Name) =>
			$"?{scheme.Name}=<value>",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase) =>
			"Authorization: Basic <credentials>",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) =>
			"Authorization: Bearer <token>",
		SecuritySchemeType.Http when !string.IsNullOrEmpty(scheme.Scheme) => $"Authorization: {scheme.Scheme} <token>",
		_ => null
	};
}
