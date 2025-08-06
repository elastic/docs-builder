// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

public record NarrativeRepository : Repository
{
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

	[YamlMember(Alias = "repo")]
	public string Origin { get; set; } = string.Empty;

	[YamlMember(Alias = "checkout_strategy")]
	public CheckoutStrategy CheckoutStrategy { get; set; } = CheckoutStrategy.Partial;

	[YamlMember(Alias = "skip")]
	public bool Skip { get; set; }

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

	public string GetBranch(ContentSource contentSource) => contentSource switch
	{
		ContentSource.Current => GitReferenceCurrent,
		ContentSource.Next => GitReferenceNext,
		ContentSource.Edge => GitReferenceEdge,
		_ => throw new ArgumentException($"The content source {contentSource} is not supported.", nameof(contentSource))
	};

}
