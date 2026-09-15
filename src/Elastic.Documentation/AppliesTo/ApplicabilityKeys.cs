// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.AppliesTo;

/// <summary>
/// Canonical <c>applies_to</c> identifiers. Catalog categories reuse the same keys
/// without inheriting lifecycle or version semantics.
/// </summary>
public static class ApplicabilityKeys
{
	public const string Stack = "stack";
	public const string Deployment = "deployment";
	public const string Serverless = "serverless";
	public const string Product = "product";

	public const string Ece = "ece";
	public const string Eck = "eck";
	public const string Ess = "ess";
	public const string Ech = "ech";
	public const string Self = "self";

	public const string Elasticsearch = "elasticsearch";
	public const string Observability = "observability";
	public const string Security = "security";
	public const string VectorDb = "vectordb";
}

/// <summary>
/// Category-only catalog membership. Distinct from <see cref="ApplicableTo"/> so
/// declaring a filter chip never claims versioned availability.
/// </summary>
public static class ApiCatalogCategory
{
	public static readonly IReadOnlyList<string> DisplayOrder =
	[
		ApplicabilityKeys.Ece,
		ApplicabilityKeys.Ess,
		ApplicabilityKeys.Self,
		ApplicabilityKeys.Serverless
	];

	public static string DisplayName(string canonical) => canonical switch
	{
		ApplicabilityKeys.Self => "Self-managed",
		ApplicabilityKeys.Ece => "Elastic Cloud Enterprise",
		ApplicabilityKeys.Ess => "Elastic Cloud Hosted",
		ApplicabilityKeys.Serverless => "Serverless",
		_ => canonical
	};

	public static string? Normalize(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return null;

		return raw.Trim().Replace('_', '-').ToLowerInvariant() switch
		{
			ApplicabilityKeys.Self => ApplicabilityKeys.Self,
			ApplicabilityKeys.Ece => ApplicabilityKeys.Ece,
			ApplicabilityKeys.Ess or ApplicabilityKeys.Ech => ApplicabilityKeys.Ess,
			ApplicabilityKeys.Serverless => ApplicabilityKeys.Serverless,
			_ => null
		};
	}
}
