// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Markdig.Syntax;
using YamlDotNet.Core;

namespace Elastic.Markdown.Myst.Directives.Settings;

public class SettingsBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	private string[]? _generatedAnchors;
	private YamlSettings? _parsedSettings;
	private int? _groupHeadingLevel;

	public override string Directive => "settings";

	public ParserContext Context { get; } = context;

	public string? IncludePath { get; private set; }

	public IFileInfo IncludeFrom { get; } = context.MarkdownSourcePath;

	public bool Found { get; private set; }

	/// <summary>
	/// Heading level for each YAML group title (e.g. 3 when the directive follows an <c>##</c> heading), same rule as <see cref="Stepper.StepBlock"/>.
	/// </summary>
	public int GroupHeadingLevel => _groupHeadingLevel ??= CalculateGroupHeadingLevel();

	/// <inheritdoc />
	public override IEnumerable<string> GeneratedAnchors =>
		_generatedAnchors ??= LoadGeneratedAnchors();

	/// <summary>Right-rail and in-page TOC entries for each settings group.</summary>
	public IEnumerable<PageTocItem> GeneratedTableOfContent
	{
		get
		{
			if (TryLoadSettings() is not { } settings)
				return [];

			var level = GroupHeadingLevel;
			return settings.Groups.Select(g => new PageTocItem
			{
				Heading = g.Name ?? string.Empty,
				Slug = SettingsViewModel.GroupHeadingSlug(g),
				Level = level
			}).Where(t => !string.IsNullOrEmpty(t.Slug));
		}
	}


	//TODO add all options from
	//https://mystmd.org/guide/directives#directive-include
	public override void FinalizeAndValidate(ParserContext context) => ExtractInclusionPath(context);

	/// <summary>
	/// Records docset substitution keys referenced in raw settings YAML (e.g. <c>page_description</c>) so
	/// strict builds do not report them as unused when they never appear in processed markdown.
	/// </summary>
	public static void CollectSubstitutionUsageFromYaml(string yamlContent, BuildContext build)
	{
		if (string.IsNullOrEmpty(yamlContent) || build.Configuration.Substitutions.Count == 0)
			return;

		var span = yamlContent.AsSpan();
		if (span.IndexOf("}}") < 0)
			return;

		var subs = build.Configuration.Substitutions;
		foreach (var match in InterpolationRegex.MatchSubstitutions().EnumerateMatches(span))
		{
			if (match.Length < 4)
				continue;

			var token = span.Slice(match.Index, match.Length).ToString().Trim('{', '}', ' ');
			if (token.Length == 0)
				continue;

			var (cleanKey, _) = SubstitutionMutationHelper.ParseKeyWithMutations(token);
			if (subs.TryGetValue(cleanKey, out _))
				build.Collector.CollectUsedSubstitutionKey(cleanKey);
		}
	}

	private void ExtractInclusionPath(ParserContext context)
	{
		var includePath = Arguments;
		if (string.IsNullOrWhiteSpace(includePath))
		{
			this.EmitError("include requires an argument.");
			return;
		}

		var includeFrom = context.MarkdownSourcePath.Directory!.FullName;
		if (includePath.StartsWith('/'))
			includeFrom = Build.DocumentationSourceDirectory.FullName;

		IncludePath = Path.Combine(includeFrom, includePath.TrimStart('/'));
		if (Build.ReadFileSystem.File.Exists(IncludePath))
			Found = true;
		else
			this.EmitError($"`{IncludePath}` does not exist.");
	}

	private int CalculateGroupHeadingLevel()
	{
		var current = (ContainerBlock)this;
		while (current.Parent is not null)
			current = current.Parent;

		var allBlocks = current.Descendants().ToList();
		var thisIndex = allBlocks.IndexOf(this);
		if (thisIndex == -1)
			return 2;

		for (var i = thisIndex - 1; i >= 0; i--)
		{
			if (allBlocks[i] is HeadingBlock heading)
				return System.Math.Min(heading.Level + 1, 6);
		}

		return 2;
	}

	private YamlSettings? TryLoadSettings()
	{
		if (_parsedSettings is not null)
			return _parsedSettings;
		if (!Found || IncludePath is null)
			return null;

		try
		{
			var file = Build.ReadFileSystem.FileInfo.New(IncludePath);
			var yaml = file.FileSystem.File.ReadAllText(file.FullName);
			CollectSubstitutionUsageFromYaml(yaml, Build);
			_parsedSettings = YamlSerialization.Deserialize<YamlSettings>(yaml, Build.ProductsConfiguration);
			return _parsedSettings;
		}
		catch (YamlException)
		{
			return null;
		}
	}

	private string[] LoadGeneratedAnchors()
	{
		if (TryLoadSettings() is not { } settings)
			return [];

		return CollectSettingIds(settings).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private static IEnumerable<string> CollectSettingIds(YamlSettings yaml)
	{
		if (!string.IsNullOrWhiteSpace(yaml.Id))
			yield return yaml.Id;

		foreach (var group in yaml.Groups)
		{
			var groupSlug = SettingsViewModel.GroupHeadingSlug(group);
			if (!string.IsNullOrEmpty(groupSlug))
				yield return groupSlug;
			foreach (var id in CollectSettingIds(group.Settings))
				yield return id;
		}
	}

	private static IEnumerable<string> CollectSettingIds(Setting[] settings)
	{
		foreach (var setting in settings)
		{
			if (!string.IsNullOrWhiteSpace(setting.Id))
				yield return setting.Id;
			foreach (var id in CollectSettingIds(setting.Settings))
				yield return id;
		}
	}
}


