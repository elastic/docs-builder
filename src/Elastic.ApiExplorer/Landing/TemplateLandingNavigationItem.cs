// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Landing;

/// <summary>
/// Navigation item for template-based API landing pages.
/// </summary>
public class TemplateLandingNavigationItem : LandingNavigationItem
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TemplateLandingNavigationItem"/> class.
	/// </summary>
	/// <param name="url">The URL for the landing page.</param>
	/// <param name="apiConfig">The API configuration with template information.</param>
	public TemplateLandingNavigationItem(string url, ResolvedApiConfiguration apiConfig) : base(url) =>
		// Store template config for future use
		_ = apiConfig;
}
