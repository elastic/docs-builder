// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

// assembler.yml exactly as authored. AssemblyLoader maps these to the Publishing domain records.

internal sealed class AssemblyConfigurationDto
{
	[YamlPropertyName("narrative")]
	public NarrativeRepositoryDto? Narrative { get; set; } = new();

	[YamlPropertyName("references")]
	public Dictionary<string, RepositoryDto?> ReferenceRepositories { get; set; } = [];

	[YamlPropertyName("environments")]
	public Dictionary<string, PublishEnvironmentDto> Environments { get; set; } = [];

	[YamlPropertyName("shared_configuration")]
	public Dictionary<string, RepositoryDto> SharedConfigurations { get; set; } = [];
}

internal class RepositoryDto
{
	[YamlPropertyName("repo")]
	public string Origin { get; set; } = string.Empty;

	[YamlPropertyName("checkout_strategy")]
	public Publishing.CheckoutStrategy CheckoutStrategy { get; set; } = Publishing.CheckoutStrategy.Partial;

	[YamlPropertyName("skip")]
	public bool Skip { get; set; }

	[YamlPropertyName("path")]
	public string? Path { get; set; }

	[YamlPropertyName("current")]
	public string GitReferenceCurrent { get; set; } = "main";

	[YamlPropertyName("next")]
	public string GitReferenceNext { get; set; } = "main";

	[YamlPropertyName("edge")]
	public string GitReferenceEdge { get; set; } = "main";

	[YamlPropertyName("sparse_paths")]
	public string[] SparsePaths { get; set; } = ["docs"];

	[YamlPropertyName("private")]
	public bool Private { get; set; }

	/// <summary>Accepts duration strings such as <c>30s</c> or <c>15m</c>.</summary>
	[YamlPropertyName("clone_timeout")]
	public TimeSpan? CloneTimeout { get; set; }
}

internal sealed class NarrativeRepositoryDto : RepositoryDto;

internal sealed class PublishEnvironmentDto
{
	[YamlPropertyName("uri")]
	public string Uri { get; set; } = string.Empty;

	[YamlPropertyName("path_prefix")]
	public string? PathPrefix { get; set; } = string.Empty;

	[YamlPropertyName("allow_indexing")]
	public bool AllowIndexing { get; set; }

	[YamlPropertyName("content_source")]
	public Publishing.ContentSource? ContentSource { get; set; }

	[YamlPropertyName("google_tag_manager")]
	public GoogleTagManagerDto GoogleTagManager { get; set; } = new();

	[YamlPropertyName("optimizely")]
	public OptimizelyDto Optimizely { get; set; } = new();

	[YamlPropertyName("feature_flags")]
	public Dictionary<string, bool> FeatureFlags { get; set; } = [];

	[YamlPropertyName("website_search_url")]
	public string? WebsiteSearchScriptUrl { get; set; }
}

internal sealed class GoogleTagManagerDto
{
	[YamlPropertyName("enabled")]
	public bool Enabled { get; set; }

	// That an enabled container names an id is checked on the model: see AssemblyConstraints.
	[YamlPropertyName("id")]
	public string? Id { get; set; }

	[YamlPropertyName("auth")]
	public string? Auth { get; set; }

	[YamlPropertyName("preview")]
	public string? Preview { get; set; }

	[YamlPropertyName("cookies_win")]
	public string? CookiesWin { get; set; }
}

internal sealed class OptimizelyDto
{
	[YamlPropertyName("enabled")]
	public bool Enabled { get; set; }

	[YamlPropertyName("id")]
	public string? Id { get; set; }
}
