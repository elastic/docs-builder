// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Versions;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// One <c>api:</c> product to index from the version index, with the git checkout used to
/// resolve <c>repository:</c> when the config entry does not override it.
/// </summary>
public sealed record OpenApiExportSource(
	string ApiKey,
	ResolvedApiConfiguration ApiConfig,
	GitCheckoutInformation Git);

/// <summary>
/// Conversion inputs for one OpenAPI spec version. <see cref="ApiKey"/> is the URL moniker;
/// <see cref="ProductId"/> is the <c>products.yml</c> id used for inference.
/// </summary>
internal readonly record struct OpenApiConvertContext(
	string ApiKey,
	string VersionMoniker,
	SemVersion FilterCeiling,
	string DisplayName,
	string ProductId);
