// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Diagram;

public class DiagramViewModel : DirectiveViewModel
{
	/// <summary>
	/// The diagram block containing the encoded URL and metadata
	/// </summary>
	public DiagramBlock? DiagramBlock { get; set; }
}
