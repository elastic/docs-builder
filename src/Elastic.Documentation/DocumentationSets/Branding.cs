// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.DocumentationSets;

/// <summary>White-label branding from <c>docset.yml</c>'s <c>branding</c>, with image paths validated against the docset.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public sealed record Branding
{
	public string? Icon { get; internal init; }
	public string? HeaderBg { get; internal init; }
	public string? OgImage { get; internal init; }
	public string? Favicon { get; internal init; }
	public string? AppleTouchIcon { get; internal init; }
	public string? Copyright { get; internal init; }
}
