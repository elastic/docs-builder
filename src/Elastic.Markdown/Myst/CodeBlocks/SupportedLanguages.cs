// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.CodeBlocks;

public static class CodeBlock
{

	private static readonly IReadOnlyDictionary<string, string> LanguageMapping = new Dictionary<string, string>
	{
		{ "asciidoc", "adoc" }, // AsciiDoc
		{ "bash", "sh, zsh" }, // Bash
		{ "c", "h" }, // C
		{ "csharp", "cs" }, // C#
		{ "css", "" }, // CSS
		{ "dockerfile", "docker" }, // Dockerfile
		{ "dos", "bat, cmd" }, // DOS
		{ "ebnf", "" }, // EBNF
		{ "go", "golang" }, // Go
		{ "gradle", "" }, // Gradle
		{ "groovy", "" }, // Groovy
		{ "handlebars", "hbs, html.hbs, html.handlebars" }, // Handlebars
		{ "http", "https" }, // HTTP
		{ "ini", "toml" }, // Ini, TOML
		{ "java", "jsp" }, // Java
		{ "javascript", "js, jsx" }, // JavaScript
		{ "json", "jsonc" }, // JSON
		{ "kotlin", "kt" }, // Kotlin
		{ "mermaid", ""},
		{ "markdown", "md, mkdown, mkd" }, // Markdown
		{ "nginx", "nginxconf" }, // Nginx
		{ "php", "" }, // PHP
		{ "plaintext", "txt, text" }, // Plaintext
		{ "powershell", "ps, ps1" }, // PowerShell
		{ "properties", "" }, // Properties
		{ "python", "py, gyp" }, // Python
		{ "ruby", "rb, gemspec, podspec, thor, irb" }, // Ruby
		{ "rust", "rs" }, // Rust
		{ "scala", "" }, // Scala
		{ "shell", "console" }, // Shell
		{ "sql", "" }, // SQL
		{ "swift", "" }, // Swift
		{ "typescript", "ts, tsx, mts, cts" }, // TypeScript
		{ "xml", "html, xhtml, rss, atom, xjb, xsd, xsl, plist, svg" }, // HTML, XML
		{ "yml", "yaml" }, // YAML

		//CUSTOM, Elastic language we wrote highlighters for
		{ "apiheader", "" },
		{ "eql", "" },
		{ "esql", "" },
		{ "painless", "" }
	};

	public static HashSet<string> Languages { get; } = new(
		LanguageMapping.Keys
			.Concat(LanguageMapping.Values
				.SelectMany(v => v.Split(',').Select(a => a.Trim()))
				.Where(v => !string.IsNullOrWhiteSpace(v))
			)
		, StringComparer.OrdinalIgnoreCase
	);
}
