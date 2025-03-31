// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Extensions;

public interface IDocsBuilderExtension : IDocumentationPlugin
{
	IDocumentationFileExporter? FileExporter { get; }

	MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser, BuildContext context, DocumentationSet documentationSet);
}


public class DocsBuilderPluginFactory : IDocumentationPluginFactory
{
	public bool TryCreate(string key, BuildContext context, [NotNullWhen(true)] out IDocumentationPlugin? plugin)
	{
		plugin = null;
		if (key != "detection-rules")
			return false;

		plugin = new DetectionRulesDocsBuilderExtension(context);
		return true;

	}
}
