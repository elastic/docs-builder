// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

public record AuthScheme(string Id, string Label, string? CredentialExample, string? DescriptionMarkdown);

/// <summary>One way to authenticate. OpenAPI treats the schemes inside one security requirement as AND, so all of them are sent together.</summary>
public record AuthRequirement(IReadOnlyList<AuthScheme> Schemes)
{
	public string Label => string.Join(" + ", Schemes.Select(static s => s.Label));
}

/// <summary>Where an endpoint's <c>auth</c> badge points: the Authentication page, the endpoint's own section, or nowhere.</summary>
public enum AuthBadgeTarget
{
	Page,
	Section,
	None
}

public enum AuthBadgeKind
{
	Required,
	Optional,
	None
}

public record AuthBadge(AuthBadgeKind Kind, AuthBadgeTarget Target)
{
	public string Text => Kind switch
	{
		AuthBadgeKind.Optional => "Auth optional",
		AuthBadgeKind.None => "No auth",
		_ => "Auth"
	};

	/// <summary>The word the CommonMark output says after <c>Authentication:</c>.</summary>
	public string Summary => Kind.ToString().ToLowerInvariant();
}

public static class OpenApiAuthSchemeResolver
{
	/// <summary>The alternatives every endpoint inherits: the document's <c>security</c>, or one per declared scheme when the document has none.</summary>
	public static IReadOnlyList<AuthRequirement> ResolveDocument(OpenApiDocument document)
	{
		if (document.Security is { Count: > 0 })
			return Alternatives(document.Security, document);

		var schemes = new List<AuthRequirement>();
		foreach (var (id, scheme) in document.Components?.SecuritySchemes ?? new Dictionary<string, IOpenApiSecurityScheme>())
		{
			var target = Target(scheme, document);
			if (LabelFor(target) is { } label)
				schemes.Add(new AuthRequirement([new AuthScheme(id, label, CredentialExample(target), DescriptionOf(target))]));
		}

		return schemes;
	}

	/// <summary>True when one of the alternatives is empty, which OpenAPI reads as "no authentication is fine too".</summary>
	public static bool AllowsAnonymous(IList<OpenApiSecurityRequirement>? requirements) =>
		requirements?.Any(static requirement => requirement.Count == 0) == true;

	/// <summary>True when the operation declares its own <c>security</c> and it names other alternatives than the document's.</summary>
	public static bool OverridesDocument(OpenApiOperation operation, OpenApiDocument document) =>
		operation.Security is { Count: > 0 } && Signature(operation.Security, document) != Signature(document.Security, document);

	/// <summary>The badge under an endpoint's path, or none when the spec says nothing about authentication.</summary>
	public static AuthBadge? Badge(OpenApiOperation operation, OpenApiDocument document)
	{
		var effective = operation.Security ?? document.Security;
		if (effective is null)
			return null;
		// An empty list, or alternatives that name no scheme at all, mean no authentication.
		if (effective.All(static requirement => requirement.Count == 0))
			return new AuthBadge(AuthBadgeKind.None, AuthBadgeTarget.None);

		var target = OverridesDocument(operation, document) ? AuthBadgeTarget.Section : AuthBadgeTarget.Page;
		return new AuthBadge(AllowsAnonymous(effective) ? AuthBadgeKind.Optional : AuthBadgeKind.Required, target);
	}

	/// <summary>The alternatives, in any order, and whether none at all is also fine, so dropping or adding the empty alternative counts as a change.</summary>
	private static string Signature(IList<OpenApiSecurityRequirement>? requirements, OpenApiDocument document)
	{
		var alternatives = Alternatives(requirements, document).Select(
			static a => string.Join('\u001f', a.Schemes.Select(static s => s.Id))
		);
		return $"{string.Join('|', alternatives.Order(StringComparer.Ordinal))}#{AllowsAnonymous(requirements)}";
	}

	private static string? DescriptionOf(IOpenApiSecurityScheme? scheme) =>
		string.IsNullOrWhiteSpace(scheme?.Description) ? null : scheme.Description;

	/// <summary>The alternatives (OR) from the operation's security, or the document's when the operation has none. Each holds the schemes (AND) it needs.</summary>
	public static IReadOnlyList<AuthRequirement> Resolve(OpenApiOperation operation, OpenApiDocument document)
	{
		// Omitted operation security is null (inherit). An empty list is an explicit override to none.
		var requirements = operation.Security ?? document.Security;
		return requirements is { Count: > 0 } ? Alternatives(requirements, document) : [];
	}

	private static List<AuthRequirement> Alternatives(IEnumerable<OpenApiSecurityRequirement>? requirements, OpenApiDocument document)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var alternatives = new List<AuthRequirement>();
		foreach (var requirement in requirements ?? [])
		{
			var schemes = SchemesOf(requirement, document);
			// The unit separator cannot be part of a scheme name, so {a, b} and {"a-b"} stay different alternatives.
			if (schemes.Count > 0 && seen.Add(string.Join('\u001f', schemes.Select(static s => s.Id))))
				alternatives.Add(new AuthRequirement(schemes));
		}

		return alternatives;
	}

	/// <summary>The schemes a sample authenticates with: those of the first alternative whose schemes a curl can all send.</summary>
	internal static IReadOnlyList<IOpenApiSecurityScheme> CurlSchemes(OpenApiOperation operation, OpenApiDocument document)
	{
		foreach (var requirement in operation.Security ?? document.Security ?? [])
		{
			var schemes = requirement.Keys.OfType<OpenApiSecuritySchemeReference>().Select(key => Target(key, document)).ToArray();
			// The schemes of one requirement are sent together, so one a curl can't send rules the whole alternative out.
			if (schemes.Length > 0 && schemes.All(static s => s is not null && SyntheticCodeSamples.Sends(s)))
				return [.. schemes.OfType<IOpenApiSecurityScheme>()];
		}

		return [];
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
