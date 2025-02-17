// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.CodeBlocks;

public static class CodeBlock
{
	private static readonly IReadOnlyDictionary<string, string> LanguageMapping = new Dictionary<string, string>
	{
		{ "1c", "" }, // 1C
		{ "abnf", "" }, // ABNF
		{ "accesslog", "" }, // Access logs
		{ "ada", "" }, // Ada
		{ "arduino", "ino" }, // Arduino (C++ w/Arduino libs)
		{ "armasm", "arm" }, // ARM assembler
		{ "avrasm", "" }, // AVR assembler
		{ "actionscript", "as" }, // ActionScript
		{ "angelscript", "asc" }, // AngelScript
		{ "apache", "apacheconf" }, // Apache
		{ "applescript", "osascript" }, // AppleScript
		{ "arcade", "" }, // Arcade
		{ "asciidoc", "adoc" }, // AsciiDoc
		{ "aspectj", "" }, // AspectJ
		{ "autohotkey", "" }, // AutoHotkey
		{ "autoit", "" }, // AutoIt
		{ "awk", "mawk, nawk, gawk" }, // Awk
		{ "bash", "sh, zsh" }, // Bash
		{ "basic", "" }, // Basic
		{ "bnf", "" }, // BNF
		{ "brainfuck", "bf" }, // Brainfuck
		{ "csharp", "cs" }, // C#
		{ "c", "h" }, // C
		{ "cpp", "hpp, cc, hh, c++, h++, cxx, hxx" }, // C++
		{ "cal", "" }, // C/AL
		{ "cos", "cls" }, // Cache Object Script
		{ "cmake", "cmake.in" }, // CMake
		{ "coq", "" }, // Coq
		{ "csp", "" }, // CSP
		{ "css", "" }, // CSS
		{ "capnproto", "capnp" }, // Cap’n Proto
		{ "clojure", "clj" }, // Clojure
		{ "coffeescript", "coffee, cson, iced" }, // CoffeeScript
		{ "crmsh", "crm, pcmk" }, // Crmsh
		{ "crystal", "cr" }, // Crystal
		{ "d", "" }, // D
		{ "dart", "" }, // Dart
		{ "dpr", "dfm, pas, pascal" }, // Delphi
		{ "diff", "patch" }, // Diff
		{ "django", "jinja" }, // Django
		{ "dns", "zone, bind" }, // DNS Zone file
		{ "dockerfile", "docker" }, // Dockerfile
		{ "dos", "bat, cmd" }, // DOS
		{ "dsconfig", "" }, // dsconfig
		{ "dts", "" }, // DTS (Device Tree)
		{ "dust", "dst" }, // Dust
		{ "ebnf", "" }, // EBNF
		{ "elixir", "" }, // Elixir
		{ "elm", "" }, // Elm
		{ "erlang", "erl" }, // Erlang
		{ "excel", "xls, xlsx" }, // Excel
		{ "fsharp", "fs, fsx, fsi, fsscript" }, // F#
		{ "fix", "" }, // FIX
		{ "fortran", "f90, f95" }, // Fortran
		{ "gcode", "nc" }, // G-Code
		{ "gams", "gms" }, // Gams
		{ "gauss", "gss" }, // GAUSS
		{ "gherkin", "" }, // Gherkin
		{ "go", "golang" }, // Go
		{ "golo", "gololang" }, // Golo
		{ "gradle", "" }, // Gradle
		{ "graphql", "gql" }, // GraphQL
		{ "groovy", "" }, // Groovy
		{ "xml", "html, xhtml, rss, atom, xjb, xsd, xsl, plist, svg" }, // HTML, XML
		{ "http", "https" }, // HTTP
		{ "haml", "" }, // Haml
		{ "handlebars", "hbs, html.hbs, html.handlebars" }, // Handlebars
		{ "haskell", "hs" }, // Haskell
		{ "haxe", "hx" }, // Haxe
		{ "hy", "hylang" }, // Hy
		{ "ini", "toml" }, // Ini, TOML
		{ "inform7", "i7" }, // Inform7
		{ "irpf90", "" }, // IRPF90
		{ "json", "jsonc" }, // JSON
		{ "java", "jsp" }, // Java
		{ "javascript", "js, jsx" }, // JavaScript
		{ "julia", "jl" }, // Julia
		{ "julia-repl", "" }, // Julia REPL
		{ "kotlin", "kt" }, // Kotlin
		{ "tex", "" }, // LaTeX
		{ "leaf", "" }, // Leaf
		{ "lasso", "ls, lassoscript" }, // Lasso
		{ "less", "" }, // Less
		{ "ldif", "" }, // LDIF
		{ "lisp", "" }, // Lisp
		{ "livecodeserver", "" }, // LiveCode Server
		{ "livescript", "ls" }, // LiveScript
		{ "lua", "pluto" }, // Lua
		{ "makefile", "mk, mak, make" }, // Makefile
		{ "markdown", "md, mkdown, mkd" }, // Markdown
		{ "mathematica", "mma, wl" }, // Mathematica
		{ "matlab", "" }, // Matlab
		{ "maxima", "" }, // Maxima
		{ "mel", "" }, // Maya Embedded Language
		{ "mercury", "" }, // Mercury
		{ "mips", "mipsasm" }, // MIPS Assembler
		{ "mizar", "" }, // Mizar
		{ "mojolicious", "" }, // Mojolicious
		{ "monkey", "" }, // Monkey
		{ "moonscript", "moon" }, // Moonscript
		{ "n1ql", "" }, // N1QL
		{ "nsis", "" }, // NSIS
		{ "nginx", "nginxconf" }, // Nginx
		{ "nim", "nimrod" }, // Nim
		{ "nix", "" }, // Nix
		{ "ocaml", "ml" }, // OCaml
		{ "objectivec", "mm, objc, obj-c, obj-c++, objective-c++" }, // Objective C
		{ "openscad", "scad" }, // OpenSCAD
		{ "ruleslanguage", "" }, // Oracle Rules Language
		{ "oxygene", "" }, // Oxygene
		{ "pf", "pf.conf" }, // PF
		{ "php", "" }, // PHP
		{ "parser3", "" }, // Parser3
		{ "perl", "pl, pm" }, // Perl
		{ "plaintext", "txt, text" }, // Plaintext
		{ "pony", "" }, // Pony
		{ "pgsql", "postgres, postgresql" }, // PostgreSQL & PL/pgSQL
		{ "powershell", "ps, ps1" }, // PowerShell
		{ "processing", "" }, // Processing
		{ "prolog", "" }, // Prolog
		{ "properties", "" }, // Properties
		{ "proto", "protobuf" }, // Protocol Buffers
		{ "puppet", "pp" }, // Puppet
		{ "python", "py, gyp" }, // Python
		{ "profile", "" }, // Python profiler results
		{ "python-repl", "pycon" }, // Python REPL
		{ "k", "kdb" }, // Q
		{ "qml", "" }, // QML
		{ "r", "" }, // R
		{ "reasonml", "re" }, // ReasonML
		{ "rib", "" }, // RenderMan RIB
		{ "rsl", "" }, // RenderMan RSL
		{ "graph", "instances" }, // Roboconf
		{ "ruby", "rb, gemspec, podspec, thor, irb" }, // Ruby
		{ "rust", "rs" }, // Rust
		{ "SAS", "sas" }, // SAS
		{ "scss", "" }, // SCSS
		{ "sql", "" }, // SQL
		{ "p21", "step, stp" }, // STEP Part 21
		{ "scala", "" }, // Scala
		{ "scheme", "" }, // Scheme
		{ "scilab", "sci" }, // Scilab
		{ "shell", "console" }, // Shell
		{ "smali", "" }, // Smali
		{ "smalltalk", "st" }, // Smalltalk
		{ "sml", "ml" }, // SML
		{ "stan", "stanfuncs" }, // Stan
		{ "stata", "" }, // Stata
		{ "stylus", "styl" }, // Stylus
		{ "subunit", "" }, // SubUnit
		{ "svelte", "" }, // Svelte 	highlight.svelte
		{ "swift", "" }, // Swift
		{ "tcl", "tk" }, // Tcl
		{ "tap", "" }, // Test Anything Protocol
		{ "thrift", "" }, // Thrift
		{ "toit", "" }, // Toit 	toit-highlight
		{ "tp", "" }, // TP
		{ "twig", "craftcms" }, // Twig
		{ "typescript", "ts, tsx, mts, cts" }, // TypeScript
		{ "vbnet", "vb" }, // VB.Net
		{ "vbscript", "vbs" }, // VBScript
		{ "vhdl", "" }, // VHDL
		{ "vala", "" }, // Vala
		{ "verilog", "v" }, // Verilog
		{ "vim", "" }, // Vim Script
		{ "axapta", "x++" }, // X++
		{ "x86asm", "" }, // x86 Assembly
		{ "xl", "tao" }, // XL
		{ "xquery", "xpath, xq, xqm" }, // XQuery
		{ "yml", "yaml" }, // YAML
		{ "zephir", "zep" }, // Zephir

		//CUSTOM, Elastic language we wrote highlighters for
		{ "eql", "" }
	};


	public static HashSet<string> Languages => new(
		LanguageMapping.Keys
			.Concat(LanguageMapping.Values
				.SelectMany(v => v.Split(',').Select(a => a.Trim()))
				.Where(v => !string.IsNullOrWhiteSpace(v))
			)
		, StringComparer.OrdinalIgnoreCase
	);
}
