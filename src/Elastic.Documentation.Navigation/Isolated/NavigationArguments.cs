// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation.Isolated;

/// <summary>
/// Arguments for creating a file navigation leaf.
/// </summary>
/// <param name="RelativePathToDocumentationSet">The relative path from the documentation set root</param>
/// <param name="RelativePathToTableOfContents">The relative path from the table of contents root</param>
/// <param name="Hidden">Whether this navigation item should be hidden from navigation</param>
/// <param name="NavigationIndex">The index position in navigation</param>
/// <param name="Parent">The parent navigation item</param>
/// <param name="HomeAccessor">The home accessor for this navigation item</param>
public record FileNavigationArgs(
	string RelativePathToDocumentationSet,
	string RelativePathToTableOfContents,
	bool Hidden,
	int NavigationIndex,
	INodeNavigationItem<INavigationModel, INavigationItem>? Parent,
	INavigationHomeAccessor HomeAccessor
);

/// <summary>
/// Arguments for creating a virtual file navigation node.
/// </summary>
/// <param name="RelativePathToDocumentationSet">The relative path from the documentation set root</param>
/// <param name="RelativePathToTableOfContents">The relative path from the table of contents root</param>
/// <param name="Hidden">Whether this navigation item should be hidden from navigation</param>
/// <param name="NavigationIndex">The index position in navigation</param>
/// <param name="Parent">The parent navigation item</param>
/// <param name="HomeAccessor">The home accessor for this navigation item</param>
public record VirtualFileNavigationArgs(
	string RelativePathToDocumentationSet,
	string RelativePathToTableOfContents,
	bool Hidden,
	int NavigationIndex,
	INodeNavigationItem<INavigationModel, INavigationItem>? Parent,
	INavigationHomeAccessor HomeAccessor
);
