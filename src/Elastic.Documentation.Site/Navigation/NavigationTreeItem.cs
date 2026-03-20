// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>Model passed to <c>_TocTreeNavV2.cshtml</c> for recursive V2 sidebar rendering.</summary>
public record NavigationTreeItem
{
	public required bool IsPrimaryNavEnabled { get; init; }
	public required bool IsGlobalAssemblyBuild { get; init; }
	public required int Level { get; init; }
	public required INodeNavigationItem<INavigationModel, INavigationItem> SubTree { get; init; }
	public required string RootNavigationId { get; init; }
}
