// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Unified-index concrete type for the merged <c>website-search.semantic-{env}</c> index.
/// Mirrors the <see cref="SiteDocument"/> shape; documents reindexed from <c>site-*</c>,
/// <c>labs-*</c>, and <c>docs-*</c> retain their original <c>$type</c> discriminator
/// when deserialized polymorphically.
/// </summary>
public record WebsiteSearchDocument : SiteDocument
{
	[JsonIgnore]
	public override string Type { get; } = "website";
}
