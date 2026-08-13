// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public sealed record NavV2FolderRenderModel
{
	public required string Id { get; init; }

	public required string Url { get; init; }

	public required string Title { get; init; }

	public required bool AllHidden { get; init; }

	public required bool IsOpen { get; init; }

	public required bool HasChildren { get; init; }

	public required string HxAttributes { get; init; }

	public required string? ActiveNavigationUrl { get; init; }

	public required NavigationTreeItem ChildTree { get; init; }
}
