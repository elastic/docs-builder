// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Site-shaped documents indexed from Elastic Labs crawls (separate <c>labs-*</c> indices
/// from ContentStack <c>site-*</c>). Identical physical shape to <see cref="SiteDocument"/>.
/// </summary>
public record LabsDocument : SiteDocument
{
	[JsonIgnore]
	public override string Type { get; } = "labs";
}
