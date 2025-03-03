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
	bool Processes(ITocItem tocItem);

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

	DocumentationFile? CreateDocumentationFile(IFileInfo file, IDirectoryInfo sourceDirectory, DocumentationSet documentationSet);
}
