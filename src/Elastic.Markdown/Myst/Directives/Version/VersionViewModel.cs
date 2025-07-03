// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Version;

public class VersionViewModel : DirectiveViewModel
{
	public required string Directive { get; init; }
	public required string VersionClass { get; init; }
	public required string Title { get; init; }
}
