// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using SharpYaml.Serialization;

namespace Elastic.Markdown.Myst.Directives.Wizard;

/// <summary>
/// The YAML schema that drives <c>{wizard}</c>. The same types are read from YAML at build time and
/// written to JSON for the web component, so keys are snake_case in both (see the contexts that use them).
/// </summary>
public record WizardConfig
{
	[YamlPropertyName("meta")]
	public WizardMeta Meta { get; set; } = new();

	[YamlPropertyName("screens")]
	public Dictionary<string, WizardScreen> Screens { get; set; } = [];

	[YamlPropertyName("results")]
	public WizardResults Results { get; set; } = new();
}

public record WizardMeta
{
	[YamlPropertyName("title")]
	public string? Title { get; set; }

	[YamlPropertyName("intro")]
	public string? Intro { get; set; }

	[YamlPropertyName("start_button")]
	public string? StartButton { get; set; }

	[YamlPropertyName("start")]
	public string? Start { get; set; }

	[YamlPropertyName("progress_label")]
	public string? ProgressLabel { get; set; }
}

/// <summary>One screen. <c>type</c> is <c>question</c>, <c>route</c> or <c>result</c>.</summary>
public record WizardScreen
{
	[YamlPropertyName("type")]
	public string? Type { get; set; }

	[YamlPropertyName("progress")]
	public int Progress { get; set; }

	[YamlPropertyName("question")]
	public string? Question { get; set; }

	[YamlPropertyName("why")]
	public string? Why { get; set; }

	/// <summary><c>list</c> (default) or <c>cards</c>: a grid of larger options that can show <c>tags</c>.</summary>
	[YamlPropertyName("layout")]
	public string? Layout { get; set; }

	/// <summary>Checkboxes plus a Continue button. <see cref="Next"/> is then set on the screen, not the options.</summary>
	[YamlPropertyName("multi")]
	public bool Multi { get; set; }

	[YamlPropertyName("next")]
	public string? Next { get; set; }

	[YamlPropertyName("options")]
	public WizardOption[] Options { get; set; } = [];

	[YamlPropertyName("rules")]
	public WizardRule[] Rules { get; set; } = [];

	[YamlPropertyName("else")]
	[JsonPropertyName("else")]
	public string? Else { get; set; }
}

public record WizardOption
{
	[YamlPropertyName("label")]
	public string? Label { get; set; }

	[YamlPropertyName("hint")]
	public string? Hint { get; set; }

	[YamlPropertyName("next")]
	public string? Next { get; set; }

	/// <summary>Small chips under the label, for example the topics an option covers. Shown in the <c>cards</c> layout.</summary>
	[YamlPropertyName("tags")]
	public string[] Tags { get; set; } = [];

	/// <summary>In the <c>cards</c> layout, spans the full width. Good for a "not sure" option.</summary>
	[YamlPropertyName("wide")]
	public bool Wide { get; set; }

	[YamlPropertyName("exclusive")]
	public bool Exclusive { get; set; }

	[YamlPropertyName("flag")]
	public string? Flag { get; set; }

	/// <summary>Writes <c>answers[key] = value</c>.</summary>
	[YamlPropertyName("set")]
	public Dictionary<string, string> Set { get; set; } = [];

	/// <summary>Adds to <c>scores[key]</c>, which decides Serverless versus Hosted.</summary>
	[YamlPropertyName("add")]
	public Dictionary<string, int> Add { get; set; } = [];
}

public record WizardRule
{
	[YamlPropertyName("when")]
	public WizardCondition When { get; set; } = new();

	[YamlPropertyName("next")]
	public string? Next { get; set; }
}

/// <summary>A test against one answer: exactly one of <c>equals</c>, <c>starts_with</c> or <c>in</c>.</summary>
public record WizardCondition
{
	[YamlPropertyName("key")]
	public string? Key { get; set; }

	[YamlPropertyName("equals")]
	[JsonPropertyName("equals")]
	public string? Equal { get; set; }

	[YamlPropertyName("starts_with")]
	public string? StartsWith { get; set; }

	[YamlPropertyName("in")]
	[JsonPropertyName("in")]
	public string[]? In { get; set; }
}

public record WizardResults
{
	/// <summary>Template for the result title. <c>{name}</c> inserts the chosen item of the variable called <c>name</c>.</summary>
	[YamlPropertyName("heading")]
	public string? Heading { get; set; }

	[YamlPropertyName("answers_heading")]
	public string? AnswersHeading { get; set; }

	[YamlPropertyName("change_label")]
	public string? ChangeLabel { get; set; }

	[YamlPropertyName("start_over_label")]
	public string? StartOverLabel { get; set; }

	[YamlPropertyName("back_label")]
	public string? BackLabel { get; set; }

	/// <summary>What the wizard recommends, as named variables. A heading like "{solution} on {deployment}" uses two.</summary>
	[YamlPropertyName("variables")]
	public Dictionary<string, WizardVariable> Variables { get; set; } = [];

	/// <summary>The primary button. The first rule whose <c>when</c> matches wins, and a rule without <c>when</c> is the fallback.</summary>
	[YamlPropertyName("cta")]
	public WizardCtaRule[] Cta { get; set; } = [];

	[YamlPropertyName("extra_docs")]
	public WizardExtraDoc[] ExtraDocs { get; set; } = [];

	[YamlPropertyName("flag_notes")]
	public Dictionary<string, string> FlagNotes { get; set; } = [];

	/// <summary>Shown when a scored variable was a near tie. <c>{other}</c> is the runner-up's name.</summary>
	[YamlPropertyName("close_call_note")]
	public string? CloseCallNote { get; set; }
}

/// <summary>
/// One thing the wizard decides, such as a product or a way to run it. It is taken from an answer, from a score, or both
/// (an answer wins when it is set).
/// </summary>
public record WizardVariable
{
	/// <summary>The answer key that names the chosen item directly.</summary>
	[YamlPropertyName("answer")]
	public string? Answer { get; set; }

	/// <summary>Candidate items whose scores (from options' <c>add</c>) compete. The highest wins, and ties go to the first one listed.</summary>
	[YamlPropertyName("score")]
	public string[] Score { get; set; } = [];

	/// <summary>A scored result is a close call when the top two scores are within this distance.</summary>
	[YamlPropertyName("close_call_within")]
	public int CloseCallWithin { get; set; } = 1;

	/// <summary>Skip this variable when it can't be resolved, instead of failing. Useful for details that only apply on some paths.</summary>
	[YamlPropertyName("optional")]
	public bool Optional { get; set; }

	/// <summary>Only resolve this variable when the condition holds. It can test answers or variables declared before this one.</summary>
	[YamlPropertyName("when")]
	public WizardCondition? When { get; set; }

	[YamlPropertyName("items")]
	public Dictionary<string, WizardResultItem> Items { get; set; } = [];
}

public record WizardCtaRule
{
	[YamlPropertyName("when")]
	public WizardCondition? When { get; set; }

	/// <summary>Button text. <c>{name}</c> inserts a variable's chosen item.</summary>
	[YamlPropertyName("label")]
	public string? Label { get; set; }

	[YamlPropertyName("href")]
	public string? Href { get; set; }

	/// <summary>Use the docs link of the chosen item of this variable instead of a fixed <c>href</c>.</summary>
	[YamlPropertyName("href_from")]
	public string? HrefFrom { get; set; }
}

public record WizardResultItem
{
	[YamlPropertyName("name")]
	public string? Name { get; set; }

	[YamlPropertyName("blurb")]
	public string? Blurb { get; set; }

	[YamlPropertyName("docs")]
	public WizardDoc Docs { get; set; } = new();
}

public record WizardDoc
{
	[YamlPropertyName("title")]
	public string? Title { get; set; }

	[YamlPropertyName("href")]
	public string? Href { get; set; }
}

public record WizardExtraDoc
{
	[YamlPropertyName("when")]
	public WizardCondition When { get; set; } = new();

	[YamlPropertyName("title")]
	public string? Title { get; set; }

	[YamlPropertyName("href")]
	public string? Href { get; set; }
}

/// <summary>Writes the validated config to JSON for the <c>&lt;wizard&gt;</c> web component.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WizardConfig))]
internal sealed partial class WizardJsonContext : JsonSerializerContext;
