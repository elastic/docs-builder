// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;

namespace Elastic.Markdown.Extensions;

public interface IDocsBuilderExtension
{
	/// Return true if this extension handles Navigation injection
	bool InjectsIntoNavigation(ITocItem tocItem);

	///  Inject items into the current navigation
	void CreateNavigationItem(
		DocumentationGroup? parent,
		ITocItem tocItem,
		NavigationLookups lookups,
		List<DocumentationGroup> groups,
		List<INavigationItem> navigationItems,
		int depth,
		ref int fileIndex,
		int index
	);

	/// Visit the <paramref name="tocItem"/> and its equivalent <see cref="DocumentationFile"/>
	void Visit(DocumentationFile file, ITocItem tocItem);

	/// Create an instance of <see cref="DocumentationFile"/> if it matches the <paramref name="file"/>.
	/// Return `null` to let another extension handle this.
	DocumentationFile? CreateDocumentationFile(IFileInfo file, IDirectoryInfo sourceDirectory, DocumentationSet documentationSet);

}
