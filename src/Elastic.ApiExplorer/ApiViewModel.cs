// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.FileProviders;

namespace Elastic.ApiExplorer;

public abstract class ApiViewModel
{
	public required string NavigationHtml { get; init; }
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }
}
