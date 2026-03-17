// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Serialization;

/// <summary>
/// Wrapper type for YAML fields that can be specified as either a comma-separated string
/// or a YAML list/sequence. Deserialized by <see cref="YamlLenientListConverter"/>.
/// Uses mutable property for compatibility with YamlDotNet source generator.
/// </summary>
internal class YamlLenientList
{
	public List<string>? Values { get; set; }

	public YamlLenientList() { }

	public YamlLenientList(List<string>? values) => Values = values;
}
