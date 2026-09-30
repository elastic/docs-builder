// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Products;

/// <summary>A <c>products: [{id}]</c> entry in <c>docset.yml</c>, as authored.</summary>
[YamlSerializable]
internal record ProductLinkDto
{
	public string Id { get; set; } = string.Empty;
}
