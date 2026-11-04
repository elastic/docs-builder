// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.Serialization;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

public record NarrativeRepository : Repository
{
	// Static constant, not a YAML member — the source generator doesn't filter static properties
	// on its own, so this must be explicitly ignored to avoid CS0176 in the generated (de)serializer.
	[YamlIgnore]
	public static string RepositoryName { get; } = "docs-content";

	public override string Name { get; set; } = RepositoryName;
}

public enum CheckoutStrategy
{
	[EnumMember(Value = "partial")]
	Partial,
	[EnumMember(Value = "full")]
	Full
}

public record Repository
{
	[YamlIgnore]
	public virtual string Name { get; set; } = string.Empty;

	[YamlPropertyName("repo")]
	public string Origin { get; set; } = string.Empty;

	[YamlPropertyName("checkout_strategy")]
	public CheckoutStrategy CheckoutStrategy { get; set; } = CheckoutStrategy.Partial;

	[YamlPropertyName("skip")]
	public bool Skip { get; set; }

	/// <summary> Allows you to override the path to the repository, but only during local builds. </summary>
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

	/// <summary>
	/// Per-attempt timeout for network git operations (fetch, pull) in CI.
	/// Accepts duration strings such as <c>30s</c> or <c>15m</c>.
	/// When omitted the global default applies (10 minutes in CI, unbounded locally).
	/// </summary>
	[YamlPropertyName("clone_timeout")]
	public TimeSpan? CloneTimeout { get; set; }

	public string GetBranch(ContentSource contentSource) => contentSource switch
	{
		ContentSource.Current => GitReferenceCurrent,
		ContentSource.Next => GitReferenceNext,
		ContentSource.Edge => GitReferenceEdge,
		_ => throw new ArgumentException($"The content source {contentSource} is not supported.", nameof(contentSource))
	};
}
