// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Represents a documentation file that can be used in navigation.
/// Extends <see cref="INavigationModel"/> with a navigation title.
/// </summary>
public interface IDocumentationFile : INavigationModel
{
	/// <summary>
	/// Gets the title to display in navigation for this documentation file.
	/// </summary>
	string NavigationTitle { get; }
}
