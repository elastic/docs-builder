// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Markdown.IO.Configuration;

public interface IDocumentationPluginFactory
{
	bool TryCreate(string key, BuildContext context, [NotNullWhen(true)] out IDocumentationPlugin? plugin);
}
