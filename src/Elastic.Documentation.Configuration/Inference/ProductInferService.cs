// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.Inference;

/// <summary>
/// Service for inferring products from repository names and git context.
/// </summary>
public class ProductInferService(
	ProductsConfiguration productsConfiguration,
	GitCheckoutInformation? gitCheckout = null)
{
	/// <summary>
	/// Infers a product from repository name.
	/// Priority 1: Direct product match by repository name (products.yml key).
	/// Priority 2: Product by repository configuration (product.repository).
	/// </summary>
	public Product? InferProductFromRepository(string repositoryName)
	{
		// Priority 1: Direct product match by repository name
		if (productsConfiguration.Products.TryGetValue(repositoryName, out var directMatch))
			return directMatch;

		// Priority 2: Product by repository configuration
		return productsConfiguration.GetProductByRepositoryName(repositoryName);
	}

	/// <summary>
	/// Gets repository name from GitCheckoutInformation.
	/// Returns null if not available (no filesystem fallback).
	/// </summary>
	public string? GetRepositoryName() =>
		gitCheckout is not null && gitCheckout != GitCheckoutInformation.Unavailable
			? gitCheckout.RepositoryName
			: null;

	/// <summary>
	/// Convenience method: infers product from current git repository.
	/// </summary>
	public Product? InferProductFromCurrentRepository()
	{
		var repoName = GetRepositoryName();
		return repoName != null ? InferProductFromRepository(repoName) : null;
	}
}
