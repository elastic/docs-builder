// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Looks up an inline SVG by product key. Used by the {hero} directive (hero-icon
/// chip) and {link-card} (solution-card icon). Keys are kept lowercase and match
/// the product ids in <c>products.yml</c> wherever possible.
/// </summary>
public static class ProductIcons
{
	private static readonly FrozenDictionary<string, string> Icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["elasticsearch"] = """
			<svg width="32" height="32" viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
				<path fill-rule="evenodd" clip-rule="evenodd" d="M55.7246 14.7075L55.7276 14.7015C50.7746 8.77351 43.3286 4.99951 34.9996 4.99951C24.4006 4.99951 15.2326 11.1115 10.8136 19.9995H46.0056C48.5306 19.9995 50.9886 19.1295 52.9206 17.5035C53.9246 16.6585 54.8636 15.7385 55.7246 14.7075Z" fill="#FEC514"/>
				<path fill-rule="evenodd" clip-rule="evenodd" d="M8 32C8 34.422 8.324 36.767 8.922 39H42C45.866 39 49 35.866 49 32C49 28.134 45.866 25 42 25H8.922C8.324 27.233 8 29.578 8 32Z" fill="rgba(255,255,255,0.85)"/>
				<path fill-rule="evenodd" clip-rule="evenodd" d="M55.7246 49.2925L55.7276 49.2985C50.7746 55.2265 43.3286 59.0005 34.9996 59.0005C24.4006 59.0005 15.2326 52.8885 10.8136 44.0005H46.0056C48.5306 44.0005 50.9886 44.8705 52.9206 46.4965C53.9246 47.3415 54.8636 48.2615 55.7246 49.2925Z" fill="#00BFB3"/>
			</svg>
			""",
		["kibana"] = """
			<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32" aria-hidden="true">
				<g fill="none" fill-rule="evenodd" transform="translate(4)">
					<polygon fill="#F04E98" points="0 0 0 28.789 24.935 .017"/>
					<path fill="rgba(255,255,255,0.25)" d="M0,12 L0,28.789 L11.906,15.051 C8.368,13.115 4.317,12 0,12"/>
					<path fill="#00BFB3" d="M14.4785,16.664 L2.2675,30.754 L1.1945,31.991 L24.3865,31.991 C23.1345,25.699 19.5035,20.272 14.4785,16.664"/>
				</g>
			</svg>
			""",
		["observability"] = """
			<svg width="32" height="32" viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
				<path fill-rule="evenodd" clip-rule="evenodd" d="M22 59H17.0906C10.966 59 6 54.0201 6 47.8785V32H22V59Z" fill="#F04E98"/>
				<path fill-rule="evenodd" clip-rule="evenodd" d="M22 59H38V19H22V59Z" fill="rgba(255,255,255,0.85)"/>
				<path fill-rule="evenodd" clip-rule="evenodd" d="M59 59H43V6L46.5081 6.04052C53.4282 6.11868 59 12.18 59 19.6277V42.9611V59Z" fill="#0077CC"/>
			</svg>
			""",
		["security"] = """
			<svg width="32" height="32" viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
				<path fill-rule="evenodd" clip-rule="evenodd" d="M20 17V4H56V34C56 41.0112 43.7467 45.6044 39 47V17H20Z" fill="#FA744E"/>
				<path fill-rule="evenodd" clip-rule="evenodd" d="M9 39.3984V22H34V60C34 60 9 49.5847 9 39.3984Z" fill="#00BFB3"/>
				<path fill-rule="evenodd" clip-rule="evenodd" d="M19 22H34V47C28.406 44.9626 19 40.292 19 34.4224V22Z" fill="rgba(255,255,255,0.85)"/>
			</svg>
			"""
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Returns the inline SVG markup for <paramref name="key"/>, or null when no icon
	/// is registered for that product.
	/// </summary>
	public static string? Get(string? key)
	{
		if (string.IsNullOrWhiteSpace(key))
			return null;
		return Icons.TryGetValue(key, out var svg) ? svg : null;
	}

	/// <summary>
	/// Initials fallback: the first character of the key, uppercased. Returns a
	/// single-letter string, suitable for the standard hero-icon chip when no
	/// SVG is registered.
	/// </summary>
	public static string Initials(string? key) =>
		string.IsNullOrWhiteSpace(key) ? "?" : char.ToUpperInvariant(key[0]).ToString();
}
