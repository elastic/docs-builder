// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst;
using SharpYaml;

namespace Elastic.Markdown.Myst.Directives.Wizard;

/// <summary>
/// Renders an interactive "which Elastic is right for you?" guide. The questions, routing and result copy all come
/// from a YAML file, which is validated at build time and handed to the <c>&lt;wizard&gt;</c> web component as JSON.
///
/// Usage in MyST Markdown:
///   :::{wizard} wizard.yaml
///   :::
///
/// The argument is a path to the YAML file, relative to the page (or to the docs root when it starts with <c>/</c>).
/// </summary>
public class WizardBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "wizard";

	public WizardConfig? Config { get; private set; }

	/// <summary>The validated config as JSON, safe to embed in a <c>&lt;script type="application/json"&gt;</c> element.</summary>
	public string? ConfigJson { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		var yamlPath = ResolvePath(context);
		if (yamlPath is null)
			return;

		string yaml;
		try
		{
			yaml = Build.ReadFileSystem.File.ReadAllText(yamlPath);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			this.EmitError($"{{wizard}} could not read `{yamlPath}`: {e.Message}");
			return;
		}

		WizardConfig? config;
		var unknownKey = false;
		try
		{
			// Strict on purpose: an unknown key here is almost always a typo, or an unquoted comma that split a flow-style label.
			var reporting = new YamlUnmappedMemberReporting(
				d =>
				{
					unknownKey = unknownKey || d.Severity == Severity.Error;
					this.Emit(d.Severity, d.Message);
				},
				CurrentFile.FullName
			);
			config = YamlSerialization.Deserialize<WizardConfig>(yaml, Build.ProductsConfiguration, reporting);
		}
		catch (YamlException e)
		{
			this.EmitError($"{{wizard}} YAML parse error in `{Path.GetFileName(yamlPath)}`: {e.Message}");
			return;
		}

		if (unknownKey)
			return;

		if (config is null)
		{
			this.EmitError($"{{wizard}} `{Path.GetFileName(yamlPath)}` is empty.");
			return;
		}

		var problems = WizardValidator.Validate(config);
		foreach (var problem in problems)
			this.EmitError($"{{wizard}} {Path.GetFileName(yamlPath)}: {problem}");
		if (problems.Count > 0)
			return;

		foreach (var warning in WizardValidator.Warnings(config))
			this.EmitWarning($"{{wizard}} {Path.GetFileName(yamlPath)}: {warning}");

		Config = config;
		ConfigJson = JsonSerializer.Serialize(config, WizardJsonContext.Default.WizardConfig);
	}

	private string? ResolvePath(ParserContext context)
	{
		var argument = Arguments;
		if (string.IsNullOrWhiteSpace(argument))
		{
			this.EmitError("{wizard} requires an argument with the path to its YAML config, for example `:::{wizard} wizard.yaml`.");
			return null;
		}

		var from = context.MarkdownSourcePath.Directory!.FullName;
		if (argument.StartsWith('/'))
			from = Build.DocumentationSourceDirectory.FullName;

		var trimmed = argument.TrimStart('/');
		if (Path.IsPathRooted(trimmed))
		{
			this.EmitError("{wizard} config path must not be an absolute path.");
			return null;
		}

		var path = Path.GetFullPath(Path.Join(from, trimmed));
		var file = Build.ReadFileSystem.FileInfo.New(path);
		if (!file.IsSubPathOf(Build.DocumentationSourceDirectory))
		{
			this.EmitError("{wizard} config path must resolve within the documentation source directory.");
			return null;
		}

		if (SymlinkValidator.ValidateFileAccess(file, Build.DocumentationSourceDirectory) is { } accessError)
		{
			this.EmitError(accessError);
			return null;
		}

		if (!Build.ReadFileSystem.File.Exists(path))
		{
			this.EmitError($"{{wizard}} config file `{path}` does not exist.");
			return null;
		}

		return path;
	}
}
