// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// The first hub-body section: a short onboarding funnel with an intro line, an
/// optional install snippet, an optional tutorial link, and a set of numbered
/// steps. The schema is YAML-formatted in the directive body for predictable
/// structure.
/// </summary>
/// <example>
/// <code>
/// :::{get-started}
/// title: Get started in 3 steps
/// intro: Spin up Kibana, connect your data, and start exploring in minutes.
/// install:
///   code: curl -fsSL https://elastic.co/start-local | sh
///   language: sh
/// tutorial:
///   label: Tutorial
///   url: /deploy-manage/deploy/self-managed
/// steps:
///   - icon: launch
///     title: Run Kibana
///     description: Start locally, run in Docker, or open a free Cloud trial.
/// :::
/// </code>
/// </example>
public class GetStartedBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "get-started";

	public GetStartedData Data { get; private set; } = GetStartedData.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		var yaml = HubYamlBody.Extract(this, new BuildContextFileReader(Build.ReadFileSystem));
		if (yaml is null)
		{
			this.EmitError("{get-started} requires a YAML body. See the get-started directive docs.");
			return;
		}

		try
		{
			Data = YamlSerialization.Deserialize<GetStartedData>(yaml, Build.ProductsConfiguration) ?? GetStartedData.Empty;
		}
		catch (YamlException ex)
		{
			this.EmitError($"{{get-started}} YAML parse error: {ex.Message}");
			return;
		}

		if (string.IsNullOrWhiteSpace(Data.Title))
			this.EmitError("{get-started} requires a `title` field in its YAML body.");

		if (Data.Tutorial is { } tutorial)
			tutorial.Url = HubLinkValidator.ValidateAndResolve(tutorial.Url, this, context);

		foreach (var step in Data.Steps)
		{
			if (!string.IsNullOrWhiteSpace(step.Link))
				step.Link = HubLinkValidator.ValidateAndResolve(step.Link, this, context);
			foreach (var option in step.Options)
				option.Url = HubLinkValidator.ValidateAndResolve(option.Url, this, context);
		}
	}
}

[YamlSerializable]
public record GetStartedData
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "intro")]
	public string? Intro { get; set; }

	[YamlMember(Alias = "install")]
	public GetStartedInstall? Install { get; set; }

	[YamlMember(Alias = "tutorial")]
	public GetStartedTutorial? Tutorial { get; set; }

	[YamlMember(Alias = "steps")]
	public GetStartedStep[] Steps { get; set; } = [];

	public static GetStartedData Empty { get; } = new();
}

[YamlSerializable]
public record GetStartedInstall
{
	[YamlMember(Alias = "code")]
	public string? Code { get; set; }

	[YamlMember(Alias = "language")]
	public string? Language { get; set; }
}

[YamlSerializable]
public record GetStartedTutorial
{
	[YamlMember(Alias = "label")]
	public string? Label { get; set; }

	[YamlMember(Alias = "url")]
	public string? Url { get; set; }
}

[YamlSerializable]
public record GetStartedStep
{
	[YamlMember(Alias = "icon")]
	public string? Icon { get; set; }

	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	/// <summary>When set, the whole step card links here.</summary>
	[YamlMember(Alias = "link")]
	public string? Link { get; set; }

	[YamlMember(Alias = "link-label")]
	public string? LinkLabel { get; set; }

	/// <summary>Two-or-more equally-weighted start options rendered side by side.</summary>
	[YamlMember(Alias = "options")]
	public GetStartedStepOption[] Options { get; set; } = [];
}

[YamlSerializable]
public record GetStartedStepOption
{
	[YamlMember(Alias = "label")]
	public string? Label { get; set; }

	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	[YamlMember(Alias = "code")]
	public string? Code { get; set; }

	[YamlMember(Alias = "language")]
	public string? Language { get; set; }

	[YamlMember(Alias = "url")]
	public string? Url { get; set; }

	[YamlMember(Alias = "url-label")]
	public string? UrlLabel { get; set; }
}
