// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.Inference;

/// <summary>
/// Service for inferring products from repository names and git context.
/// </summary>
public class ProductInferService(ProductsConfiguration productsConfiguration, GitCheckoutInformation? gitCheckout = null)
{
	/// <summary>
	/// Returns all products that map to <paramref name="repositoryName"/>.
	/// One repo can map to many products (e.g., <c>cloud</c> → three cloud products).
	/// </summary>
	public IReadOnlyList<Product> InferProductsFromRepository(string repositoryName) =>
		productsConfiguration.GetProductsByRepositoryName(repositoryName);

	/// <summary>
	/// Returns the single product for <paramref name="repositoryName"/>, or <c>null</c>
	/// when there is no match or more than one match.
	/// </summary>
	public Product? InferProductFromRepository(string repositoryName)
	{
		var matches = InferProductsFromRepository(repositoryName);
		return matches.Count == 1 ? matches[0] : null;
	}

	/// <summary>
	/// Gets repository name from GitCheckoutInformation.
	/// Returns null if not available (no filesystem fallback).
	/// </summary>
	public string? GetRepositoryName() =>
		gitCheckout is not null && gitCheckout != GitCheckoutInformation.Unavailable ? gitCheckout.RepositoryName : null;

	/// <summary>
	/// Convenience method: infers product from current git repository.
	/// </summary>
	public Product? InferProductFromCurrentRepository()
	{
		var repoName = GetRepositoryName();
		return repoName != null ? InferProductFromRepository(repoName) : null;
	}
}
