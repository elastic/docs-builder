// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

// assembler.yml exactly as authored. AssemblyLoader maps these to the Publishing domain records.

[YamlSerializable]
internal sealed class AssemblyConfigurationDto
{
	[YamlMember(Alias = "narrative")]
	public NarrativeRepositoryDto? Narrative { get; set; } = new();

	[YamlMember(Alias = "references")]
	public Dictionary<string, RepositoryDto?> ReferenceRepositories { get; set; } = [];

	[YamlMember(Alias = "environments")]
	public Dictionary<string, PublishEnvironmentDto> Environments { get; set; } = [];

	[YamlMember(Alias = "shared_configuration")]
	public Dictionary<string, RepositoryDto> SharedConfigurations { get; set; } = [];
}

[YamlSerializable]
internal class RepositoryDto
{
	[YamlMember(Alias = "repo")]
	public string Origin { get; set; } = string.Empty;

	[YamlMember(Alias = "checkout_strategy")]
	public Publishing.CheckoutStrategy CheckoutStrategy { get; set; } = Publishing.CheckoutStrategy.Partial;

	[YamlMember(Alias = "skip")]
	public bool Skip { get; set; }

	[YamlMember(Alias = "path")]
	public string? Path { get; set; }

	[YamlMember(Alias = "current")]
	public string GitReferenceCurrent { get; set; } = "main";

	[YamlMember(Alias = "next")]
	public string GitReferenceNext { get; set; } = "main";

	[YamlMember(Alias = "edge")]
	public string GitReferenceEdge { get; set; } = "main";

	[YamlMember(Alias = "sparse_paths")]
	public string[] SparsePaths { get; set; } = ["docs"];

	[YamlMember(Alias = "private")]
	public bool Private { get; set; }

	/// <summary>Accepts duration strings such as <c>30s</c> or <c>15m</c>.</summary>
	[YamlMember(Alias = "clone_timeout")]
	public TimeSpan? CloneTimeout { get; set; }
}

[YamlSerializable]
internal sealed class NarrativeRepositoryDto : RepositoryDto;

[YamlSerializable]
internal sealed class PublishEnvironmentDto
{
	[YamlMember(Alias = "uri")]
	public string Uri { get; set; } = string.Empty;

	[YamlMember(Alias = "path_prefix")]
	public string? PathPrefix { get; set; } = string.Empty;

	[YamlMember(Alias = "allow_indexing")]
	public bool AllowIndexing { get; set; }

	[YamlMember(Alias = "content_source")]
	public Publishing.ContentSource ContentSource { get; set; }

	[YamlMember(Alias = "google_tag_manager")]
	public GoogleTagManagerDto GoogleTagManager { get; set; } = new();

	[YamlMember(Alias = "optimizely")]
	public OptimizelyDto Optimizely { get; set; } = new();

	[YamlMember(Alias = "feature_flags")]
	public Dictionary<string, bool> FeatureFlags { get; set; } = [];

	[YamlMember(Alias = "website_search_url")]
	public string? WebsiteSearchScriptUrl { get; set; }
}

[YamlSerializable]
internal sealed class GoogleTagManagerDto
{
	[YamlMember(Alias = "enabled")]
	public bool Enabled { get; set; }

	// Checked as the key is read, so it only fires when `enabled: true` comes before an empty `id:`. Kept as-is: moving
	// it to after parsing would reject configs that load today.
	[YamlMember(Alias = "id")]
	public string? Id
	{
		get;
		set
		{
			if (Enabled && string.IsNullOrEmpty(value))
				throw new ArgumentException("Id is required when Enabled is true.");
			field = value;
		}
	}

	[YamlMember(Alias = "auth")]
	public string? Auth { get; set; }

	[YamlMember(Alias = "preview")]
	public string? Preview { get; set; }

	[YamlMember(Alias = "cookies_win")]
	public string? CookiesWin { get; set; }
}

[YamlSerializable]
internal sealed class OptimizelyDto
{
	[YamlMember(Alias = "enabled")]
	public bool Enabled { get; set; }

	[YamlMember(Alias = "id")]
	public string? Id { get; set; }
}
