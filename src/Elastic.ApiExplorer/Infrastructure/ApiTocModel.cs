// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>The On this page list of an API page. <paramref name="DesignSystem"/> adds the hooks of the design system's scroll spy, which the production one must not also find.</summary>
public sealed record ApiTocModel(IReadOnlyList<TocItem> Items, bool DesignSystem);
