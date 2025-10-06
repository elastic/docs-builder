// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.IO.NewNavigation;

public class MarkdownFileFactory(BuildContext context, MarkdownParser markdownParser) : IDocumentationFileFactory<MarkdownFile>
{
	/// <inheritdoc />
	public MarkdownFile? TryCreateDocumentationFile(IFileInfo path, IFileSystem readFileSystem)
	{
		path.Refresh();
		if (!path.Exists)
			return null;

		return new MarkdownFile(path, context.DocumentationSourceDirectory, markdownParser, context);
	}
}
