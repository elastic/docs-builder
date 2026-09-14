// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Markdown.Page;

internal readonly record struct PageTitleOptions(BuildType BuildType, BrandingConfiguration? Branding, string SiteName);

internal static class PageTitleResolver
{
	public static string Resolve(
		string title,
		string? overrideTitle,
		IReadOnlyCollection<Product> products,
		PageTitleOptions options
	) => string.IsNullOrWhiteSpace(overrideTitle) ? Resolve(title, products, options) : AddSuffix(overrideTitle, options);

	public static string Resolve(string title, IReadOnlyCollection<Product> products, PageTitleOptions options)
	{
		if (products is { Count: 1 })
		{
			var productName = products.First().DisplayName;
			if (!title.Contains(productName, StringComparison.OrdinalIgnoreCase))
				title = $"{title} - {productName}";
		}

		return AddSuffix(title, options);
	}

	private static string AddSuffix(string title, PageTitleOptions options)
	{
		var suffix = options.BuildType == BuildType.Assembler && options.Branding is null ? "Elastic Docs" : options.SiteName;
		return $"{title} | {suffix}";
	}
}
