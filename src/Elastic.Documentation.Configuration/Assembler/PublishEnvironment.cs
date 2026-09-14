// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Builder;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

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

	[YamlMember(Alias = "optimizely")]
	public Optimizely Optimizely { get; set; } = new();

	[YamlMember(Alias = "feature_flags")]
	public Dictionary<string, bool> FeatureFlags { get; set; } = [];

	[YamlMember(Alias = "website_search_url")]
	public string? WebsiteSearchScriptUrl { get; set; }

	/// <summary>
	/// Returns a normalized, env-var-overridable <see cref="FeatureFlags"/> view of <see cref="FeatureFlags"/>.
	/// Use this rather than accessing the raw dictionary so that key normalization and env-var overrides
	/// are applied consistently with how docset-level flags are processed.
	/// </summary>
	public Builder.FeatureFlags ToFeatureFlags()
	{
		var flags = new Builder.FeatureFlags([]);
		foreach (var (key, value) in FeatureFlags)
			flags.Set(key, value);
		return flags;
	}
}
