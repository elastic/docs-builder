// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Product included in a bundle with strongly typed lifecycle.
/// </summary>
public record BundledProduct
{
	/// <summary>
	/// Parameterless constructor for object initializer syntax.
	/// </summary>
	public BundledProduct() { }

	/// <summary>
	/// Constructor with all parameters.
	/// </summary>
	[SetsRequiredMembers]
	public BundledProduct(string productId, string? target = null, Lifecycle? lifecycle = null)
	{
		ProductId = productId;
		Target = target;
		Lifecycle = lifecycle;
	}

	/// <summary>The product identifier.</summary>
	public required string ProductId { get; init; }

	/// <summary>Optional target version.</summary>
	public string? Target { get; init; }

	/// <summary>The lifecycle stage of the feature for this product.</summary>
	public Lifecycle? Lifecycle { get; init; }
}
