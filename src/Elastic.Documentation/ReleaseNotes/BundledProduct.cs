// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Product included in a bundle with strongly typed lifecycle.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>ReleaseNotesLoader.LoadBundle</c> / <c>BundleLoader</c> in <c>Elastic.Documentation.Configuration</c> (a bundle YAML file) or the pure operations <see cref="Bundle.Assemble"/>, <see cref="Bundle.ForRelease"/> and <see cref="Bundle.AmendOf"/>, or <see cref="BundledProduct.Of"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>ProductId</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="BundleConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new BundledProduct { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
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
