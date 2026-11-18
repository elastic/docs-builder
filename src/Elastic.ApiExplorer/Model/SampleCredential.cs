// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

internal enum CredentialKind
{
	ApiKey,
	Basic,
	Digest,
	Bearer
}

/// <summary>How a sample sends one security scheme's credential: what it is, and the header or query key that carries it.</summary>
internal sealed record SampleCredential(CredentialKind Kind, ParameterLocation In, string Name)
{
	private const string Authorization = "Authorization";

	/// <summary>An API key in the <c>Authorization</c> header travels behind the <c>ApiKey</c> scheme word, as every Elastic API expects it.</summary>
	public bool IsAuthorizationHeader => In == ParameterLocation.Header && Name.Equals(Authorization, StringComparison.OrdinalIgnoreCase);

	/// <summary>Two credentials with the same slot cannot both be sent. Header names ignore case; query keys do not.</summary>
	public string Slot => In == ParameterLocation.Header ? "header:" + Name.ToUpperInvariant() : "query:" + Name;

	/// <summary>Null when no sample has a form for the scheme: a cookie key, mutual TLS, or an HTTP scheme such as negotiate.</summary>
	public static SampleCredential? From(IOpenApiSecurityScheme scheme) => scheme.Type switch
	{
		SecuritySchemeType.ApiKey when scheme.In is ParameterLocation.Header or ParameterLocation.Query
			&& !string.IsNullOrEmpty(scheme.Name) => new SampleCredential(CredentialKind.ApiKey, scheme.In.Value, scheme.Name),
		SecuritySchemeType.Http when HttpKind(scheme.Scheme) is { } kind =>
			new SampleCredential(kind, ParameterLocation.Header, Authorization),
		SecuritySchemeType.OAuth2 or SecuritySchemeType.OpenIdConnect =>
			new SampleCredential(CredentialKind.Bearer, ParameterLocation.Header, Authorization),
		_ => null
	};

	/// <summary>
	/// The credential a <c>.http</c> file can send. Digest is a challenge and response that curl runs itself, so the file has
	/// no form for it. A key in the <c>Content-Type</c> header would collide with the body's media type.
	/// </summary>
	public static SampleCredential? ForHttpFile(IOpenApiSecurityScheme scheme) =>
		From(scheme) is { Kind: not CredentialKind.Digest } credential && credential.Slot != "header:CONTENT-TYPE" ? credential : null;

	/// <summary>Whether a declared parameter is this credential, so the sample writes the credential and not the parameter.</summary>
	public bool Replaces(IOpenApiParameter parameter) =>
		parameter.In == In
			&& string.Equals(
				parameter.Name,
				Name,
				In == ParameterLocation.Header ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
			);

	private static CredentialKind? HttpKind(string? scheme) => scheme?.ToLowerInvariant() switch
	{
		"basic" => CredentialKind.Basic,
		"digest" => CredentialKind.Digest,
		"bearer" => CredentialKind.Bearer,
		_ => null
	};
}
