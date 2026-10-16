// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;

namespace Elastic.Documentation.Site.StyleGuide;

public sealed class ComponentsPageModel
{
	public required StyleGuideViewModel Guide { get; init; }
	public required IReadOnlyList<ComponentSection> Sections { get; init; }
}
