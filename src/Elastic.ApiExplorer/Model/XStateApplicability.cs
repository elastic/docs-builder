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
	/// Rewrites an <c>x-state</c> label as <c>applies_to</c> syntax, e.g. "Added in 7.7.0" → "ga 7.7.0",
	/// "Technical Preview" → "preview". The most restrictive lifecycle named in the label wins.
	/// </summary>
	internal static string? ToAppliesToSyntax(string? xState)
	{
		if (string.IsNullOrWhiteSpace(xState))
			return null;

		var lower = xState.ToLowerInvariant();
		var lifecycle = lower switch
		{
			_ when lower.Contains("removed") => "removed",
			_ when lower.Contains("deprecated") => "deprecated",
			_ when lower.Contains("beta") => "beta",
			_ when lower.Contains("experimental") => "experimental",
			_ when lower.Contains("preview") => "preview",
			_ when lower.Contains("generally available") => "ga",
			_ => null
		};

		var versionMatch = SemVersionRegex().Match(xState);
		return versionMatch.Success ? $"{lifecycle ?? "ga"} {versionMatch.Groups[1].Value}" : lifecycle;
	}

	/// <summary>
	/// True when the API is introduced in a Stack version newer than <paramref name="currentStack"/>.
	/// A future deprecation or removal does not count: the API still exists today.
	/// </summary>
	public static bool IsUnreleased(ApplicableTo? applicableTo, SemVersion currentStack) =>
		applicableTo?.Stack?.Any(
			a => a.Lifecycle is not (ProductLifecycle.Deprecated or ProductLifecycle.Removed) &&
				a.Version is { } version && version != AllVersionsSpec.Instance && version.Min > currentStack
		)
			== true;

	/// <summary>
	/// Plain "Generally available" with no version says nothing a reader can act on, so it gets no badge.
	/// </summary>
	public static bool IsWorthABadge(ApplicableTo applicableTo) =>
		applicableTo.Stack is { } stack
			&& !stack.All(
				a => a.Lifecycle == ProductLifecycle.GenerallyAvailable && (a.Version is null || a.Version == AllVersionsSpec.Instance)
			);
}
