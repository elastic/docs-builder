// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Markdown.Myst.Directives.Wizard;

/// <summary>
/// Build-time checks for a <see cref="WizardConfig"/>. Returns human-readable problems; the block turns them into diagnostics.
/// </summary>
public static partial class WizardValidator
{
	private const string Question = "question";
	private const string Route = "route";
	private const string Result = "result";

	private static readonly string[] ScreenTypes = [Question, Route, Result];

	[GeneratedRegex(@"\{(\w+)\}")]
	private static partial Regex Placeholder();

	public static IReadOnlyList<string> Validate(WizardConfig config)
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(config.Meta.Title))
			errors.Add("`meta.title` is required.");
		if (config.Screens.Count == 0)
		{
			errors.Add("`screens` must define at least one screen.");
			return errors;
		}

		if (string.IsNullOrWhiteSpace(config.Meta.Start) || !config.Screens.ContainsKey(config.Meta.Start))
		{
			errors.Add($"`meta.start` must name a screen. Got `{config.Meta.Start}`.");
			return errors;
		}

		ValidateScreens(config, errors);
		if (errors.Count > 0)
			return errors; // Everything below assumes every screen target exists.

		ValidateResults(config, errors);
		if (errors.Count > 0)
			return errors;

		WalkPaths(config, errors);
		return errors;
	}

	/// <summary>
	/// Things that aren't wrong but are probably a mistake. Right now: a question that cannot change anything the result uses,
	/// so asking it only makes the wizard longer.
	/// </summary>
	public static IReadOnlyList<string> Warnings(WizardConfig config)
	{
		var warnings = new List<string>();
		var r = config.Results;

		var readKeys = r
			.Variables
			.Values
			.Select(v => v.Answer)
			.OfType<string>()
			.Concat(r.Variables.Values.Select(v => v.When?.Key).OfType<string>())
			.Concat(config.Screens.Values.SelectMany(s => s.Rules).Select(rule => rule.When.Key).OfType<string>())
			.Concat(r.Cta.Select(c => c.When?.Key).OfType<string>())
			.Concat(r.ExtraDocs.Select(e => e.When.Key).OfType<string>())
			.ToHashSet();
		var scoreKeys = r.Variables.Values.SelectMany(v => v.Score).ToHashSet();

		foreach (var (id, screen) in config.Screens.Where(s => s.Value.Type == Question))
		{
			var changesSomething = screen.Options.Any(
				o => o.Set.Keys.Any(readKeys.Contains) || o.Add.Keys.Any(scoreKeys.Contains) || o.Flag is not null
			);
			var branches = !screen.Multi && screen.Options.Select(o => o.Next).Distinct().Count() > 1;
			if (!changesSomething && !branches)
				warnings.Add(
					$"Question `{id}` doesn't change anything: none of its options set an answer, score or flag that the result or a route uses, and they all go to the same screen. Remove it or make an option do something."
				);
		}

		return warnings;
	}

	private static void ValidateScreens(WizardConfig config, List<string> errors)
	{
		var results = 0;
		foreach (var (id, screen) in config.Screens)
		{
			if (screen.Type is null || !ScreenTypes.Contains(screen.Type))
			{
				errors.Add($"Screen `{id}` has type `{screen.Type}`. Use one of: {string.Join(", ", ScreenTypes)}.");
				continue;
			}

			switch (screen.Type)
			{
				case Result:
					results++;
					break;
				case Route:
					if (screen.Rules.Length == 0)
						errors.Add($"Route `{id}` needs at least one rule.");
					foreach (var rule in screen.Rules)
					{
						CheckTarget(config, errors, id, rule.Next);
						CheckCondition(errors, $"Route `{id}`", rule.When);
					}
					if (string.IsNullOrWhiteSpace(screen.Else))
						errors.Add($"Route `{id}` needs an `else` target.");
					else
						CheckTarget(config, errors, id, screen.Else);
					break;
				default:
					ValidateQuestion(config, errors, id, screen);
					break;
			}
		}

		if (results != 1)
			errors.Add($"Exactly one screen must have `type: result`. Found {results}.");
	}

	private static void ValidateQuestion(WizardConfig config, List<string> errors, string id, WizardScreen screen)
	{
		if (string.IsNullOrWhiteSpace(screen.Question))
			errors.Add($"Question `{id}` needs a `question`.");
		if (screen.Options.Length == 0)
			errors.Add($"Question `{id}` needs at least one option.");
		if (screen.Layout is not (null or "list" or "cards"))
			errors.Add($"Question `{id}` has layout `{screen.Layout}`. Use `list` or `cards`.");

		if (screen.Multi)
			CheckTarget(config, errors, id, screen.Next);

		for (var i = 0; i < screen.Options.Length; i++)
		{
			var option = screen.Options[i];
			if (string.IsNullOrWhiteSpace(option.Label))
				errors.Add($"Question `{id}` option {i + 1} needs a `label`. If the label contains a comma in flow-style YAML, quote it.");
			if (!screen.Multi)
				CheckTarget(config, errors, $"{id} option {i + 1}", option.Next);
		}
	}

	private static void CheckTarget(WizardConfig config, List<string> errors, string from, string? target)
	{
		if (string.IsNullOrWhiteSpace(target))
			errors.Add($"`{from}` is missing a `next` target.");
		else if (!config.Screens.ContainsKey(target))
			errors.Add($"`{from}` points to unknown screen `{target}`.");
	}

	private static void CheckCondition(List<string> errors, string where, WizardCondition when)
	{
		var tests = (when.Equal is null ? 0 : 1) + (when.StartsWith is null ? 0 : 1) + (when.In is null ? 0 : 1);
		if (string.IsNullOrWhiteSpace(when.Key) || tests != 1)
			errors.Add($"{where} has a condition that needs a `key` and exactly one of `equals`, `starts_with` or `in`.");
	}

	// Links open in the reader's browser, so only web and site-relative links are allowed (no javascript: or data: URLs).
	private static void CheckHref(List<string> errors, string where, string? href)
	{
		var isWebOrSiteLink = !string.IsNullOrWhiteSpace(href)
			&& (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
				|| href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				|| href.StartsWith('/'));
		if (!isWebOrSiteLink)
			errors.Add($"{where} needs an `href` that starts with `https://` or `/`. Got `{href}`.");
	}

	private static void CheckPlaceholders(WizardConfig config, List<string> errors, string where, string? text, params string[] extra)
	{
		if (text is null)
			return;
		foreach (Match m in Placeholder().Matches(text))
		{
			var name = m.Groups[1].Value;
			if (!config.Results.Variables.ContainsKey(name) && !extra.Contains(name))
				errors.Add($"{where} uses `{{{name}}}`, which is not a variable in `results.variables`.");
		}
	}

	private static void ValidateResults(WizardConfig config, List<string> errors)
	{
		var r = config.Results;
		var setKeys = config.Screens.Values.SelectMany(s => s.Options).SelectMany(o => o.Set.Keys).ToHashSet();

		if (string.IsNullOrWhiteSpace(r.Heading))
			errors.Add("`results.heading` is required.");
		CheckPlaceholders(config, errors, "`results.heading`", r.Heading);

		if (r.Variables.Count == 0)
			errors.Add("`results.variables` must define at least one variable.");

		var declared = new HashSet<string>();
		foreach (var (name, variable) in r.Variables)
		{
			if (variable.When is not null)
			{
				CheckCondition(errors, $"Variable `{name}`", variable.When);
				if (variable.When.Key is not null && !setKeys.Contains(variable.When.Key) && !declared.Contains(variable.When.Key))
					errors.Add(
						$"Variable `{name}` has a `when` that tests `{variable.When.Key}`, which is neither an answer that an option sets nor a variable declared before it."
					);
			}
			_ = declared.Add(name);

			if (variable.Items.Count == 0)
				errors.Add($"Variable `{name}` needs at least one entry under `items`.");
			if (variable.Answer is null && variable.Score.Length == 0)
				errors.Add($"Variable `{name}` needs an `answer` key, a `score` list, or both.");
			if (variable.Answer is not null && !setKeys.Contains(variable.Answer))
				errors.Add($"Variable `{name}` reads answer `{variable.Answer}`, but no option ever sets it.");
			foreach (var candidate in variable.Score.Where(c => !variable.Items.ContainsKey(c)))
				errors.Add($"Variable `{name}` scores `{candidate}`, which has no entry under `items`.");
			foreach (var (key, item) in variable.Items)
			{
				if (string.IsNullOrWhiteSpace(item.Name))
					errors.Add($"Item `{name}.{key}` needs a `name`.");
				CheckHref(errors, $"Item `{name}.{key}` docs", item.Docs.Href);
			}
		}

		if (r.Cta.Length == 0)
			errors.Add("`results.cta` needs at least one rule.");
		else if (r.Cta[^1].When is not null)
			errors.Add("The last `results.cta` rule must not have a `when`, so there is always a button.");

		foreach (var rule in r.Cta)
		{
			if (string.IsNullOrWhiteSpace(rule.Label))
				errors.Add("Every `results.cta` rule needs a `label`.");
			CheckPlaceholders(config, errors, "A `results.cta` label", rule.Label);
			if (rule.When is not null)
			{
				CheckCondition(errors, "`results.cta`", rule.When);
				CheckConditionKey(errors, "`results.cta`", rule.When, setKeys, r);
			}

			if ((rule.Href is null) == (rule.HrefFrom is null))
				errors.Add($"`results.cta` rule `{rule.Label}` needs exactly one of `href` or `href_from`.");
			else if (rule.Href is not null)
				CheckHref(errors, $"`results.cta` rule `{rule.Label}`", rule.Href);
			else if (!r.Variables.ContainsKey(rule.HrefFrom!))
				errors.Add($"`results.cta` rule `{rule.Label}` has `href_from: {rule.HrefFrom}`, which is not a variable.");
		}

		foreach (var extra in r.ExtraDocs)
		{
			CheckCondition(errors, "`results.extra_docs`", extra.When);
			CheckConditionKey(errors, "`results.extra_docs`", extra.When, setKeys, r);
			CheckHref(errors, "`results.extra_docs`", extra.Href);
		}

		CheckPlaceholders(config, errors, "`results.close_call_note`", r.CloseCallNote, "other");

		foreach (var flag in config.Screens.Values.SelectMany(s => s.Options).Select(o => o.Flag).OfType<string>())
		{
			if (!r.FlagNotes.ContainsKey(flag))
				errors.Add($"Flag `{flag}` has no entry in `results.flag_notes`.");
		}

		// A route that tests an answer nothing sets can never match, which is almost always a typo.
		foreach (var (id, screen) in config.Screens.Where(s => s.Value.Type == Route))
		{
			foreach (var rule in screen.Rules.Where(rule => rule.When.Key is not null && !setKeys.Contains(rule.When.Key)))
				errors.Add($"Route `{id}` tests answer `{rule.When.Key}`, but no option ever sets it.");
		}
	}

	private static void CheckConditionKey(List<string> errors, string where, WizardCondition when, HashSet<string> setKeys, WizardResults r)
	{
		if (when.Key is not null && !setKeys.Contains(when.Key) && !r.Variables.ContainsKey(when.Key))
			errors.Add($"{where} tests `{when.Key}`, which is neither an answer that an option sets nor a variable.");
	}

	/// <summary>
	/// Explores every reachable (screen, relevant answers) state and checks that each one that ends on the result screen
	/// can name an item for every variable. Only answers that routes or variables read are tracked, which keeps this small.
	/// Scores are ignored: a scored variable always has a winner.
	/// </summary>
	private static void WalkPaths(WizardConfig config, List<string> errors)
	{
		var relevant = config
			.Results
			.Variables
			.Values
			.Select(v => v.Answer)
			.OfType<string>()
			.Concat(config.Screens.Values.SelectMany(s => s.Rules).Select(rule => rule.When.Key).OfType<string>())
			.ToHashSet();

		var seen = new HashSet<string>();
		var reached = new HashSet<string>();
		var stack = new Stack<(string Screen, Dictionary<string, string> Answers)>();
		stack.Push((config.Meta.Start!, []));
		var problems = new HashSet<string>();

		while (stack.Count > 0)
		{
			var (id, answers) = stack.Pop();
			var fingerprint = id
				+ "|"
				+ string.Join(";", answers.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}={a.Value}"));
			if (!seen.Add(fingerprint))
				continue;

			_ = reached.Add(id);
			var screen = config.Screens[id];

			switch (screen.Type)
			{
				case Result:
					CheckResultState(config, answers, problems);
					break;
				case Route:
					var next = screen.Rules.FirstOrDefault(rule => Matches(rule.When, answers.GetValueOrDefault(rule.When.Key ?? "")))?.Next
						?? screen.Else!;
					stack.Push((next, answers));
					break;
				default:
					foreach (var option in screen.Options)
					{
						var after = new Dictionary<string, string>(answers);
						foreach (var (key, value) in option.Set.Where(kv => relevant.Contains(kv.Key)))
							after[key] = value;
						stack.Push((screen.Multi ? screen.Next! : option.Next!, after));
					}
					break;
			}
		}

		foreach (var id in config.Screens.Where(s => s.Value.Type != Route).Select(s => s.Key).Where(id => !reached.Contains(id)))
			_ = problems.Add($"Screen `{id}` can never be reached from `{config.Meta.Start}`.");

		errors.AddRange(problems);
	}

	private static bool Matches(WizardCondition when, string? value) =>
		when.Equal is not null
			? value == when.Equal
			: when.StartsWith is not null
				? value?.StartsWith(when.StartsWith, StringComparison.Ordinal) == true
				: when.In is not null && value is not null && when.In.Contains(value);

	private static void CheckResultState(WizardConfig config, Dictionary<string, string> answers, HashSet<string> problems)
	{
		foreach (var (name, variable) in config.Results.Variables)
		{
			// A conditional variable depends on other results, which this walk doesn't track, so it may legitimately be absent.
			if (variable.Optional || variable.When is not null)
				continue;

			var answer = variable.Answer is null ? null : answers.GetValueOrDefault(variable.Answer);
			if (answer is not null)
			{
				if (!variable.Items.ContainsKey(answer))
					_ = problems.Add(
						$"A path reaches the result with `{variable.Answer}` = `{answer}`, which has no entry under `results.variables.{name}.items`."
					);
			}
			else if (variable.Score.Length == 0)
				_ = problems.Add(
					$"A path reaches the result without `{variable.Answer}` being set, so variable `{name}` has nothing to show. Add a `score` list or set it on every path."
				);
		}
	}
}
