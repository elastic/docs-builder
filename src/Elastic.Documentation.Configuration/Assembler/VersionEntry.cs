// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

public record VersionEntry
{
	[YamlMember(Alias = "base")]
	public string? Base { get; set; }

	[YamlMember(Alias = "current")]
	public string? Current { get; set; }
}
