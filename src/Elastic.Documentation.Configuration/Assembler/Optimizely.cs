// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

public record Optimizely
{
	[YamlPropertyName("enabled")]
	public bool Enabled { get; set; }

	[YamlPropertyName("id")]
	public string? Id { get; set; }
}
