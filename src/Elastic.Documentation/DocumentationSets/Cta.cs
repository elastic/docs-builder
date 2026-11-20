// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>A right-gutter call-to-action template.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record Cta
{
	/// <summary>Name of the template this was resolved from; <see cref="DefaultName"/> for the built-in default.</summary>
	public required string Name { get; init; }
	public required string Label { get; init; }
	public required string Url { get; init; }

	[OrderedEquality]
	public IReadOnlyList<string> Benefits { get; internal init; } = [];

	/// <summary>The built-in default, reproducing the CTA that renders when no <c>cta:</c> config exists.</summary>
	public const string DefaultName = "trial";

	/// <summary>Right-gutter card space is limited; benefit bullet lists are capped at this many entries.</summary>
	public const int MaxBenefits = 3;

	public static Cta Default { get; } = new()
	{
		Name = DefaultName,
		Label = "Get started free",
		Url = "https://cloud.elastic.co/registration?page=docs&placement=docs-siderail",
		Benefits = ["14-day free trial", "All features included", "No setup required"]
	};
}
