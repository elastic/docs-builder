// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation;

/// <summary>
/// The one spelling rule for product ids. A product is spelled <c>cloud-hosted</c> in the catalog and may be written <c>cloud_hosted</c> (and with stray
/// spaces) by a user, in a changelog entry, a configuration file or a bundle filter. Everything that compares a written product id with a catalog id
/// (or with another written one) goes through <see cref="Normalize"/> first.
/// </summary>
public static class ProductNames
{
	/// <summary>
	/// The id as the catalog spells it: trimmed, <c>_</c> becomes <c>-</c>. The case is NOT changed (catalog lookups are ordinal and the product filter of a
	/// bundle compares ignoring case on top of this). A <see langword="null"/> is the empty string. Pure and idempotent.
	/// </summary>
	public static string Normalize(string? productId) =>
		string.IsNullOrWhiteSpace(productId) ? string.Empty : productId.Trim().Replace('_', '-');

	/// <summary>Whether two written product ids name the same product (<see cref="Normalize"/>d, then compared ignoring case).</summary>
	public static bool AreSame(string? left, string? right) =>
		string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
}
