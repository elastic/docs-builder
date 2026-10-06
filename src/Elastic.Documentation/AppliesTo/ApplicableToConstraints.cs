// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Versions;
using Nullean.Entities;
using DiagnosticSeverity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.AppliesTo;

/// <summary>Every rule an <see cref="ApplicableTo"/> (an <c>applies_to</c> block) must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class ApplicableToConstraints
{
	public static Constraint<ApplicableTo> AppliesToNotEmpty { get; } = Constraints<ApplicableTo>
		.Always(a => Collections(a).Any(), "The 'applies_to' field is present but empty. No applicability will be assumed.")
		.WithSeverity(Severity.Warning)
		.Describe("M87: An applies_to block declares at least one key; the empty-block warning is dead code and never emitted.");

	public static Constraint<ApplicableTo> ApplicabilityLifecycleNotDeprecated { get; } = Constraints<ApplicableTo>
		.Each(
			a => Collections(a).SelectMany(c => c.Items.Select(i => (c.Key, Item: i))),
			x =>
				x.Item.Lifecycle is not (ProductLifecycle.Development or ProductLifecycle.Planned or ProductLifecycle.Discontinued or ProductLifecycle.Unavailable),
			x => $"Key '{x.Key}': the '{x.Item.GetLifeCycleName()}' lifecycle is deprecated and will be removed in a future release."
		)
		.WithSeverity(Severity.Hint)
		.Describe("M91: No key uses a deprecated lifecycle (dev, development, coming, planned, discontinued, unavailable).");

	public static Constraint<ApplicableTo> VersionlessKeysHaveNoVersion { get; } = Constraints<ApplicableTo>.Each(
		Collections,
		c => !c.Versionless || !c.Items.Any(IsVersioned),
		c =>
			$"Can't specify a version for '{c.Key}' because this product is not versioned. Remove the version, or use 'stack:' for version-specific requirements."
	).Describe(
		"M94: ess, ech and serverless (and its projects) take no version; a version there is rejected by the YAML reader but not by the model."
	);

	public static Constraint<ApplicableTo> ApplicabilityVersionsConsistent { get; } = Constraints<ApplicableTo>.EachCheck(
		a => Collections(a).Where(c => !c.Versionless),
		(_, c) => InconsistentVersions(c.Key, c.Items)
	).Describe(
		"M96: Per key, one version per lifecycle, at most one '+' (greater-than-or-equal), range min <= max, and no overlapping ranges."
	);

	public static Constraint<ApplicableTo> ApplicabilityVersionReadable { get; } = Constraints<ApplicableTo>
		.EachCheck(Collections, (_, c) => UnreadVersions(c.Key, c.Items))
		.WithSeverity(Severity.Warning)
		.Describe(
			"M92: A version written after a lifecycle parses, and nothing follows it; an unparsable version is kept as 'no version' and tokens after the second are dropped."
		);

	public static Constraint<ApplicableTo> AppliesToKeysRead { get; } = Constraints<ApplicableTo>
		.Each<string>(
			a => a.UnknownKeys,
			key =>
				$"The applies_to key '{key}' is not read and has no effect (nested keys must be one of the keys of their parent; product keys use '_', as in 'apm_agent_java')."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"M89: Every nested applies_to key (and every product key) is one a reader maps; an unmapped one is silently ignored (top-level unknown keys are reported by M98)."
		);

	public static Constraint<ApplicableTo> AppliesToValuesAreText { get; } = Constraints<ApplicableTo>
		.Each<string>(
			a => a.IgnoredValues,
			key => $"The applies_to key '{key}' must have a text value such as 'ga 9.0'; any other value is ignored."
		)
		.WithSeverity(Severity.Warning)
		.Describe("M93: The value of an applies_to key is text (or empty); a list, a mapping or another non-text value is ignored.");

	public static Constraint<ApplicableTo> ApplicabilityDiagnosticsReported { get; } = Constraints<ApplicableTo>.EachCheck(
		a => (IEnumerable<(DiagnosticSeverity, string)>?)a.Diagnostics ?? [],
		(_, d) => [Report(d.Item1, d.Item2)]
	).Describe(
		"M98: The diagnostics raised while reading an applies_to block are reported, at their own severity; admonitions and applies-item drop them."
	);

	private static Violation Report(DiagnosticSeverity severity, string message) => severity switch
	{
		DiagnosticSeverity.Error => Violation.Error(message),
		DiagnosticSeverity.Warning => Violation.Warning(message),
		_ => Violation.Hint(message)
	};

	/// <summary>Every key of the block that is set, with its collection; keys are named as in YAML.</summary>
	public static IEnumerable<(string Key, AppliesCollection Items, bool Versionless)> Collections(ApplicableTo a)
	{
		if (a.Stack is { } stack)
			yield return (ApplicabilityKeys.Stack, stack, false);
		if (a.Deployment is { } deployment)
		{
			(string, AppliesCollection?, bool)[] keys =
			[
				(ApplicabilityKeys.Self, deployment.Self, false),
				(ApplicabilityKeys.Ece, deployment.Ece, false),
				(ApplicabilityKeys.Eck, deployment.Eck, false),
				(ApplicabilityKeys.Ess, deployment.Ess, true)
			];
			foreach (var (key, items, versionless) in keys)
			{
				if (items is not null)
					yield return (key, items, versionless);
			}
		}
		if (a.Serverless is { } serverless)
		{
			(string, AppliesCollection?)[] keys =
			[
				(ApplicabilityKeys.Elasticsearch, serverless.Elasticsearch),
				(ApplicabilityKeys.Observability, serverless.Observability),
				(ApplicabilityKeys.Security, serverless.Security),
				(ApplicabilityKeys.VectorDb, serverless.VectorDatabase)
			];
			foreach (var (key, items) in keys)
			{
				if (items is not null)
					yield return (key, items, true);
			}
		}
		if (a.Product is { } product)
			yield return (ApplicabilityKeys.Product, product, false);
		if (a.ProductApplicability is { } p)
		{
			(string, AppliesCollection?)[] keys =
			[
				("ecctl", p.Ecctl),
				("curator", p.Curator),
				("apm-agent-android", p.ApmAgentAndroid),
				("apm-agent-dotnet", p.ApmAgentDotnet),
				("apm-agent-go", p.ApmAgentGo),
				("apm-agent-ios", p.ApmAgentIos),
				("apm-agent-java", p.ApmAgentJava),
				("apm-agent-node", p.ApmAgentNode),
				("apm-agent-php", p.ApmAgentPhp),
				("apm-agent-python", p.ApmAgentPython),
				("apm-agent-ruby", p.ApmAgentRuby),
				("apm-agent-rum-js", p.ApmAgentRumJs),
				("edot-ios", p.EdotIos),
				("edot-android", p.EdotAndroid),
				("edot-dotnet", p.EdotDotnet),
				("edot-java", p.EdotJava),
				("edot-node", p.EdotNode),
				("edot-browser", p.EdotBrowser),
				("edot-php", p.EdotPhp),
				("edot-python", p.EdotPython),
				("edot-cf-aws", p.EdotCfAws),
				("edot-cf-azure", p.EdotCfAzure),
				("edot-cf-gcp", p.EdotCfGcp),
				("edot-collector", p.EdotCollector)
			];
			foreach (var (key, items) in keys)
			{
				if (items is not null)
					yield return (key, items, false);
			}
		}
	}

	private static IEnumerable<Violation> UnreadVersions(string key, AppliesCollection collection)
	{
		foreach (var item in collection)
		{
			if (item.Raw is not { } raw)
				continue;
			var tokens = raw.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length >= 2 && item.Version is null)
				yield return Violation.Warning(
					$"Key '{key}': the version '{tokens[1]}' in '{raw}' could not be parsed, so the item applies without a version."
				);
			if (tokens.Length > 2)
				yield return Violation.Warning(
					$"Key '{key}': '{string.Join(' ', tokens.Skip(2))}' after the version in '{raw}' is ignored."
				);
		}
	}

	private static bool IsVersioned(Applicability a) => a.Version is not null && a.Version != AllVersionsSpec.Instance;

	private static IEnumerable<Violation> InconsistentVersions(string key, AppliesCollection collection)
	{
		var items = collection.ToList();

		var repeated = items
			.GroupBy(i => i.Lifecycle)
			.Where(g => g.Count(IsVersioned) > 1)
			.Select(g => g.First().GetLifeCycleName())
			.ToList();
		if (repeated.Count > 0)
			yield return Violation.Hint(
				$"Key '{key}': Multiple version declarations found for lifecycle(s): {string.Join(", ", repeated)}. Only one version per lifecycle is allowed."
			);

		if (items.Count(i => i.Version is { Kind: VersionSpecKind.GreaterThanOrEqual } && IsVersioned(i)) > 1)
			yield return Violation.Hint(
				$"Key '{key}': Multiple items use greater-than-or-equal syntax. Only one item per key can use this syntax."
			);

		var inverted = items
			.Where(i => i.Version is { Kind: VersionSpecKind.Range, Max: { } max } v && v.Min.CompareTo(max) > 0)
			.Select(i => $"{i.GetLifeCycleName()} ({i.Version})")
			.ToList();
		if (inverted.Count > 0)
			yield return Violation.Hint(
				$"Key '{key}': Invalid range(s) where first version is greater than last version: {string.Join(", ", inverted)}."
			);

		var versioned = items.Where(IsVersioned).Select(i => i.Version!).ToList();
		if (versioned.Where((v, i) => versioned.Skip(i + 1).Any(w => Overlap(v, w))).Any())
			yield return Violation.Hint(
				$"Key '{key}': Overlapping version ranges detected. Ensure version ranges do not overlap within the same key."
			);
	}

	// A range may end where a greater-than-or-equal version starts (a version bump); otherwise two specs overlap when
	// each starts no later than the other ends, with no end meaning unbounded.
	private static bool Overlap(VersionSpec first, VersionSpec second)
	{
		if (BumpedInto(first, second) || BumpedInto(second, first))
			return false;
		return StartsBeforeEnd(first.Min, EndOf(second)) && StartsBeforeEnd(second.Min, EndOf(first));
	}

	private static bool BumpedInto(VersionSpec range, VersionSpec from) =>
		range is { Kind: VersionSpecKind.Range, Max: { } max }
			&& from.Kind == VersionSpecKind.GreaterThanOrEqual
			&& max.CompareTo(from.Min) <= 0;

	private static SemVersion? EndOf(VersionSpec spec) => spec.Kind switch
	{
		VersionSpecKind.Exact => spec.Min,
		VersionSpecKind.Range => spec.Max,
		_ => null
	};

	private static bool StartsBeforeEnd(SemVersion start, SemVersion? end) => end is null || start.CompareTo(end) <= 0;
}
