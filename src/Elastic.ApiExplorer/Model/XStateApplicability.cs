// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// The single projection of Elastic's free-text <c>x-state</c> label (for example
/// "Generally available; Added in 7.7.0") onto the <c>applies_to</c> model. API products
/// version against the Elastic Stack, so the result is always a <c>stack</c> applicability.
/// </summary>
public static partial class XStateApplicability
{
	[GeneratedRegex(@"(\d+\.\d+\.\d+)")]
	private static partial Regex SemVersionRegex();

	public static ApplicableTo? FromOperation(OpenApiOperation operation) => Parse(OpenApiExtensionReader.GetState(operation));

	public static ApplicableTo? FromSchema(IOpenApiSchema schema) =>
		Parse(OpenApiExtensionReader.TryGetString(schema.Extensions, "x-state"));

	/// <summary>Parses an <c>x-state</c> label; null when it carries no lifecycle or version.</summary>
	public static ApplicableTo? Parse(string? xState)
	{
		if (ToAppliesToSyntax(xState) is not { } syntax)
			return null;

		var diagnostics = new List<(Severity, string)>();
		return AppliesCollection.TryParse(syntax, diagnostics, out var stack) && stack is not null
			? new ApplicableTo { Stack = stack }
			: null;
	}

	/// <summary>
	/// Rewrites an <c>x-state</c> label as <c>applies_to</c> syntax, one entry per <c>;</c> segment so each
	/// version stays with its lifecycle: "Generally available; Added in 7.7.0; Deprecated in 9.2.0" →
	/// "ga 7.7.0, deprecated 9.2.0", "Technical Preview" → "preview".
	/// </summary>
	internal static string? ToAppliesToSyntax(string? xState)
	{
		if (string.IsNullOrWhiteSpace(xState))
			return null;

		var entries = new List<(string Lifecycle, string? Version)>();
		foreach (var segment in xState.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			var lifecycle = LifecycleIn(segment);
			var versionMatch = SemVersionRegex().Match(segment);
			var version = versionMatch.Success ? versionMatch.Groups[1].Value : null;

			// "Added in X" after "Generally available", or "Deprecated in X" after "Deprecated", versions the previous entry
			if (entries.Count > 0 && entries[^1].Version is null && (lifecycle is null || lifecycle == entries[^1].Lifecycle))
				entries[^1] = entries[^1] with { Version = version };
			else if (lifecycle is not null || version is not null)
				entries.Add((lifecycle ?? "ga", version));
		}

		// An unversioned lifecycle would claim every version and overlap the versioned ones
		if (entries.Any(e => e.Version is not null))
			_ = entries.RemoveAll(e => e.Version is null);

		return entries.Count == 0
			? null
			: string.Join(", ", entries.Select(e => e.Version is null ? e.Lifecycle : $"{e.Lifecycle} {e.Version}"));
	}

	private static string? LifecycleIn(string segment)
	{
		var lower = segment.ToLowerInvariant();
		return lower switch
		{
			_ when lower.Contains("removed") => "removed",
			_ when lower.Contains("deprecated") => "deprecated",
			_ when lower.Contains("beta") => "beta",
			_ when lower.Contains("experimental") => "experimental",
			_ when lower.Contains("preview") => "preview",
			_ when lower.Contains("generally available") => "ga",
			_ => null
		};
	}

	/// <summary>
	/// True when no lifecycle that introduces the API has shipped by <paramref name="currentStack"/>.
	/// A later transition does not count: "Beta; Added in 8.12.0; Generally available; Added in 9.5.0"
	/// is available today as beta, and a future deprecation or removal leaves the API in place.
	/// </summary>
	public static bool IsUnreleased(ApplicableTo? applicableTo, SemVersion currentStack)
	{
		var introductions = applicableTo?.Stack?.Where(
			a => a.Lifecycle is not (ProductLifecycle.Deprecated or ProductLifecycle.Removed)
		).ToArray()
			?? [];

		return introductions.Length > 0 && introductions.All(a => IsFuture(a.Version, currentStack));
	}

	private static bool IsFuture(VersionSpec? version, SemVersion currentStack) =>
		version is not null && version != AllVersionsSpec.Instance && version.Min > currentStack;

	/// <summary>
	/// Plain "Generally available" with no version says nothing a reader can act on, so it gets no badge.
	/// </summary>
	public static bool IsWorthABadge(ApplicableTo applicableTo) =>
		applicableTo.Stack is { } stack
			&& !stack.All(
				a => a.Lifecycle == ProductLifecycle.GenerallyAvailable && (a.Version is null || a.Version == AllVersionsSpec.Instance)
			);
}
