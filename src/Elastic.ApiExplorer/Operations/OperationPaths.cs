// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Operations;

/// <summary>One HTTP method and route listed for an operation.</summary>
public sealed record OperationPathChoice(string Method, string Route)
{
	public string Key => $"{Method} {Route}";
}

/// <summary>Paths parsed from an operation description, plus the path the visible example follows.</summary>
public sealed record OperationPathSet(IReadOnlyList<OperationPathChoice> Paths, OperationPathChoice? Selected, string? Description)
{
	public bool HasChoices => Paths.Count > 1;
}

/// <summary>
/// Docs OpenAPI collapses every route of an operation onto one path item and repeats the
/// others as bump.sh verb/path badges in the description. This reads that list back out.
/// </summary>
public static partial class OperationPaths
{
	public static OperationPathSet Resolve(string? description, string method, string route, IReadOnlyList<CodeSample> codeSamples)
	{
		var canonical = new OperationPathChoice(method, route);
		if (string.IsNullOrEmpty(description))
			return new OperationPathSet([], null, description);

		var parsed = ParseBadges(description);
		if (parsed.Count == 0)
			return new OperationPathSet([], null, description);

		if (!parsed.Contains(canonical))
			parsed.Insert(0, canonical);

		var selected = Select(parsed, codeSamples) ?? canonical;
		var stripped = PathListing().Replace(description, "", 1).Trim();
		return new OperationPathSet(parsed, selected, stripped);
	}

	/// <summary>
	/// A path parameter is required only when the selected route contains its <c>{name}</c> placeholder.
	/// Single-path operations keep the OpenAPI flag.
	/// </summary>
	private static bool IsPathParameterRequired(
		string? parameterName,
		OperationPathChoice selected,
		IReadOnlyList<OperationPathChoice> paths,
		bool openApiRequired
	)
	{
		if (paths.Count <= 1 || string.IsNullOrEmpty(parameterName))
			return openApiRequired;

		return selected.Route.Contains("{" + parameterName + "}", StringComparison.Ordinal);
	}

	public static ApiPathParameter ApplyRequirement(ApiPathParameter parameter, OperationPathSet paths)
	{
		if (!paths.HasChoices || paths.Selected is null || parameter.Name is not { Length: > 0 } name)
			return parameter with { Required = parameter.Parameter.Required };

		var routes = paths
			.Paths
			.Where(path => path.Route.Contains("{" + name + "}", StringComparison.Ordinal))
			.Select(path => path.Route)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		var required = IsPathParameterRequired(name, paths.Selected, paths.Paths, parameter.Parameter.Required);
		return parameter with { Required = required, RequiredForRoutes = routes };
	}

	public static IReadOnlyList<ExampleScenario> PerPathExamples(
		ExampleScenario? basis,
		OperationPathSet paths,
		IReadOnlyList<CodeSample> codeSamples
	)
	{
		var seed = basis ?? new ExampleScenario { Title = "Examples", TabId = "examples" };
		var owner = paths.Selected;
		var examples = new ExampleScenario[paths.Paths.Count];
		for (var i = 0; i < paths.Paths.Count; i++)
		{
			var path = paths.Paths[i];
			var ownsSamples = owner == path && codeSamples.Count > 0;
			examples[i] = seed with
			{
				Title = path.Route,
				TabId = "path-" + i,
				HttpMethod = path.Method,
				Route = path.Route,
				CodeSamples = ownsSamples ? codeSamples : [ConsoleSample(path)],
				CodeSamplesIncludeRequest = ownsSamples && seed.CodeSamplesIncludeRequest
			};
		}

		return examples;
	}

	private static List<OperationPathChoice> ParseBadges(string description)
	{
		var parsed = new List<OperationPathChoice>();
		foreach (Match match in PathBadge().Matches(description))
		{
			var method = match.Groups[1].Value.Trim().ToLowerInvariant();
			var path = match.Groups[3].Value.Trim();
			if (method.Length == 0 || path.Length == 0)
				continue;

			var choice = new OperationPathChoice(method, path);
			if (!parsed.Contains(choice))
				parsed.Add(choice);
		}

		return parsed;
	}

	private static OperationPathChoice? Select(IReadOnlyList<OperationPathChoice> paths, IReadOnlyList<CodeSample> codeSamples)
	{
		var probe = ConsoleSample(codeSamples) ?? AnySample(codeSamples);
		return probe is null ? paths[0] : Match(probe.Source, paths) ?? paths[0];
	}

	private static CodeSample? ConsoleSample(IReadOnlyList<CodeSample> codeSamples)
	{
		for (var i = 0; i < codeSamples.Count; i++)
		{
			if (codeSamples[i].Language.Equals("Console", StringComparison.OrdinalIgnoreCase))
				return codeSamples[i];
		}

		return null;
	}

	private static CodeSample? AnySample(IReadOnlyList<CodeSample> codeSamples) => codeSamples.Count == 0 ? null : codeSamples[0];

	private static OperationPathChoice? Match(string source, IReadOnlyList<OperationPathChoice> paths)
	{
		OperationPathChoice? best = null;
		var bestScore = 0;
		foreach (var candidate in RequestPaths(source))
		{
			foreach (var path in paths)
			{
				var score = TemplateScore(candidate, path.Route);
				if (score <= bestScore)
					continue;
				bestScore = score;
				best = path;
			}
		}

		return best;
	}

	private static IEnumerable<string> RequestPaths(string source)
	{
		foreach (Match match in RequestLine().Matches(source))
			yield return match.Groups[1].Value;
		foreach (Match match in HostPath().Matches(source))
			yield return match.Groups[1].Value;
	}

	private static int TemplateScore(string actual, string template)
	{
		var actualSegments = Segments(actual);
		var templateSegments = Segments(template);
		if (actualSegments.Length == 0 || actualSegments.Length != templateSegments.Length)
			return 0;

		var literals = 0;
		for (var i = 0; i < templateSegments.Length; i++)
		{
			if (IsPlaceholder(templateSegments[i]))
				continue;
			if (!actualSegments[i].Equals(templateSegments[i], StringComparison.OrdinalIgnoreCase))
				return 0;
			literals++;
		}

		return (literals * 100) + templateSegments.Length;
	}

	private static string[] Segments(string path)
	{
		var cut = path;
		var query = cut.IndexOfAny(['?', '#']);
		if (query >= 0)
			cut = cut[..query];
		return cut.Split('/', StringSplitOptions.RemoveEmptyEntries);
	}

	private static bool IsPlaceholder(string segment) => segment.Length > 2 && segment[0] == '{' && segment[^1] == '}';

	// ponytail: one Console line when no sample matches this path. Use the spec sample when one exists.
	private static CodeSample ConsoleSample(OperationPathChoice path) =>
		new("Console", $"{path.Method.ToUpperInvariant()} {path.Route}", CodeSample.GetHighlightClass("Console"));

	[GeneratedRegex("""<span class="operation-verb\s+([^"]+)">([^<]*)</span>\s*(?:&nbsp;)?\s*<span class="operation-path">([^<]+)</span>""", RegexOptions.IgnoreCase
		| RegexOptions.CultureInvariant)]
	private static partial Regex PathBadge();

	[GeneratedRegex("""\*\*(?:All methods and paths|Spaces method and path) for this operation:\*\*\s*(?:<div\b[^>]*>.*?</div>\s*)+""", RegexOptions.IgnoreCase
		| RegexOptions.Singleline
		| RegexOptions.CultureInvariant)]
	private static partial Regex PathListing();

	[GeneratedRegex("""(?:GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)\s+(\S+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex RequestLine();

	[GeneratedRegex("""(?:\$\{?[A-Za-z0-9_]+\}?)(/[^\s"'?]+)""", RegexOptions.CultureInvariant)]
	private static partial Regex HostPath();
}
