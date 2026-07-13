// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer;

/// <summary>
/// Host-specific navigation and layout settings for <see cref="OpenApiGenerator"/>.
/// </summary>
public record OpenApiNavigationHost(
	INavigationHtmlWriter NavigationHtmlWriter,
	FeatureFlags? FeatureFlags = null);
