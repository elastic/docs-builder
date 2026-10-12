// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.CodeBlock;

public sealed record CodeBlockModel
{
	public required string Language { get; init; }

	/// <summary>The rendered <c>code</c> element or elements. The client-side highlighter finds them through <c>pre code</c>.</summary>
	public required IHtmlContent Body { get; init; }

	public string? Caption { get; init; }

	public string LanguageLabel => Language.ToLowerInvariant() switch
	{
		"" or "text" or "plaintext" => "Text",
		"csharp" or "cs" => "C#",
		"js" or "javascript" => "JavaScript",
		"ts" or "typescript" => "TypeScript",
		"json" => "JSON",
		"yaml" or "yml" => "YAML",
		"sh" or "bash" or "shell" => "Shell",
		"esql" => "ES|QL",
		"sql" => "SQL",
		"html" => "HTML",
		"css" => "CSS",
		"console" => "Console",
		"apiheader" => "Request",
		var other => char.ToUpperInvariant(other[0]) + other[1..]
	};
}
