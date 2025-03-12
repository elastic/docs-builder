// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Documentation.Assembler.Configuration;

public record NarrativeRepository : Repository
{
	public static string RepositoryName { get; } = "docs-content";
	public override string Name { get; set; } = RepositoryName;
	public override string? PathPrefix { get; set; }
}

public record Repository
{
	[YamlIgnore]
	public virtual string Name { get; set; } = string.Empty;

	[YamlMember(Alias = "repo")]
	public string Origin { get; set; } = string.Empty;

	[YamlMember(Alias = "current")]
	public string CurrentBranch { get; set; } = "main";

	[YamlMember(Alias = "checkout_strategy")]
	public string CheckoutStrategy { get; set; } = "partial";

	private string? _pathPrefix;
	[YamlMember(Alias = "path_prefix")]
	public virtual string? PathPrefix
	{
		get => _pathPrefix ?? $"reference/{Name}";
		set => _pathPrefix = value;
	}

	[YamlMember(Alias = "skip")]
	public bool Skip { get; set; }
}
