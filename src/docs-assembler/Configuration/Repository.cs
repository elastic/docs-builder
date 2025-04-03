// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO.State;
using YamlDotNet.Serialization;

namespace Documentation.Assembler.Configuration;

public record NarrativeRepository : Repository
{
	public static string RepositoryName { get; } = "docs-content";
	public override string Name { get; set; } = RepositoryName;
}

public record Repository
{
	[YamlIgnore]
	public virtual string Name { get; set; } = string.Empty;

	[YamlMember(Alias = "repo")]
	public string Origin { get; set; } = string.Empty;

	[YamlMember(Alias = "current")]
	public string GitReferenceCurrent { get; set; } = "main";

	[YamlMember(Alias = "next")]
	public string GitReferenceNext { get; set; } = "main";

	[YamlMember(Alias = "checkout_strategy")]
	public string CheckoutStrategy { get; set; } = "partial";

	[YamlMember(Alias = "skip")]
	public bool Skip { get; set; }

	public string GetBranch(ContentSource contentSource) => contentSource switch
	{
		ContentSource.Current => GitReferenceCurrent,
		ContentSource.Next => GitReferenceNext,
		_ => throw new ArgumentException($"The content source {contentSource} is not supported.", nameof(contentSource))
	};
}
