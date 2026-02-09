// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

public record Optimizely
{
	[YamlMember(Alias = "enabled")]
	public bool Enabled { get; set; }

	[YamlMember(Alias = "id")]
	public string? Id { get; set; }
}
