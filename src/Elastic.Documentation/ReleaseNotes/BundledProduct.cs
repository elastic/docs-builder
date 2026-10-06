// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Product included in a bundle with strongly typed lifecycle.
/// </summary>
public record BundledProduct
{
	/// <summary>
	/// Parameterless constructor for object initializer syntax.
	/// </summary>
	internal BundledProduct() { }

	/// <summary>
	/// Constructor with all parameters.
	/// </summary>
	internal BundledProduct(string productId, string? target = null, Lifecycle? lifecycle = null, string? repo = null, string? owner = null)
	{
		ProductId = productId;
		Target = target;
		Lifecycle = lifecycle;
		Repo = repo;
		Owner = owner;
	}

	/// <summary>The product identifier.</summary>
	public string ProductId { get; internal init; } = default!;

	/// <summary>Optional target version.</summary>
	public string? Target { get; internal init; }

	/// <summary>The lifecycle stage of the feature for this product.</summary>
	public Lifecycle? Lifecycle { get; internal init; }

	/// <summary>
	/// GitHub repository name for generating PR/issue links.
	/// If not specified, falls back to ProductId.
	/// </summary>
	public string? Repo { get; internal init; }

	/// <summary>
	/// GitHub owner (organization or user) for generating PR/issue links.
	/// If not specified, defaults to "elastic".
	/// </summary>
	public string? Owner { get; internal init; }

	/// <summary>
	/// A bundle's product from the pieces a CLI names: the product id, a target (<c>*</c> is none) and a lifecycle's text (<c>*</c> or an unrecognized one is none).
	/// </summary>
	public static BundledProduct Of(string? productId, string? target, string? lifecycle, string? repo = null, string? owner = null) =>
		new(
			productId ?? "",
			target == "*" ? null : target,
			DomainOperation.ParseLifecycle(lifecycle == "*" ? null : lifecycle),
			repo,
			owner
		);
}
