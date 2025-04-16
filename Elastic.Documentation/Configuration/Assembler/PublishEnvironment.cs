// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Model.Configuration.Assembler;

public record PublishEnvironment
{
	[YamlIgnore]
	public string Name { get; set; } = string.Empty;

	[YamlMember(Alias = "uri")]
	public string Uri { get; set; } = string.Empty;

	[YamlMember(Alias = "path_prefix")]
	public string? PathPrefix { get; set; } = string.Empty;

	[YamlMember(Alias = "allow_indexing")]
	public bool AllowIndexing { get; set; }

	[YamlMember(Alias = "content_source")]
	public ContentSource ContentSource { get; set; }

	[YamlMember(Alias = "google_tag_manager")]
	public GoogleTagManager GoogleTagManager { get; set; } = new();
}
