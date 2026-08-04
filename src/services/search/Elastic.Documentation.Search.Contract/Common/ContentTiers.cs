// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Editorial weight of a document, used to demote low-value content (marketing, legal, downloads)
/// and promote high-value content (editorial overviews, concept pages) without hand-maintained
/// downstream rules. Shared as string constants — not a CLR enum — because <see cref="ContentTiers"/>
/// values flow through <c>[Keyword]</c> JSON serialization the same way as other keyword fields
/// (e.g. <c>content_type</c>, <c>navigation_section</c>) and both essc and docs-builder must agree
/// on these exact literal values at compile time.
/// </summary>
public static class ContentTiers
{
	/// <summary>Editorial overviews, concept ("what is") pages, and flagship product pages.</summary>
	public const string Primary = "primary";

	/// <summary>Standard reference content. The neutral default — NOT a penalty default.</summary>
	public const string Reference = "reference";

	/// <summary>Plugins, glossary entries, integrations — useful but secondary.</summary>
	public const string Supplementary = "supplementary";

	/// <summary>Marketing, legal, downloads, press, and release notes — demoted in search.</summary>
	public const string Peripheral = "peripheral";
}
