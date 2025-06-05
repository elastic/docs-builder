// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.OpenApi.Models.Interfaces;

namespace Elastic.ApiExplorer.ApiListing;

public class ApiInformation(string pathKey, IOpenApiPathItem pathValue) : IPageInformation
{
	public INavigationGroup NavigationRoot { get; } = new ApiNavigationGroup();
	public string Url { get; } = pathKey;
	public string NavigationTitle { get; } = pathValue.Summary;
	public string CrossLink { get; } = pathValue.Summary; //TODO
}

public class IndexViewModel
{
	public required ApiInformation ApiInformation { get; init; }
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

}
