// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// Represents a documentation file that can be used in navigation.
/// Extends <see cref="INavigationModel"/> with a navigation title.
public interface IDocumentationFile : INavigationModel
{
	/// Gets the page title (h1 heading) for this documentation file.
	string Title { get; }

	/// Gets the page description from frontmatter, if set.
	string? Description { get; }

	/// Gets the title to display in navigation for this documentation file.
	string NavigationTitle { get; }
}
