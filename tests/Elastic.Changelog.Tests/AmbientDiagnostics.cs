// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests;

/// <summary>
/// A warning many bundle tests produce by accident of their setup, without being about it: a profile written with the
/// deprecated <c>output_products</c> key, or a config that sets no repository. A test about something else drops these
/// from its diagnostics snapshot with <see cref="AmbientDiagnostics.WithoutAmbient"/>, so rewording one message does not
/// fail dozens of unrelated tests. Each one has exactly one test that snapshots it in full (named on the member).
/// Nothing else is dropped: a new, unexpected warning or error still shows up in the snapshot.
/// </summary>
public sealed class AmbientDiagnostic
{
	private readonly Severity _severity;
	private readonly Regex _message;

	private AmbientDiagnostic(Severity severity, string messagePattern)
	{
		_severity = severity;
		_message = new Regex(messagePattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
	}

	internal bool Matches(Diagnostic d) => d.Severity == _severity && d.Message is { } m && _message.IsMatch(m);

	/// <summary>
	/// <c>bundle.profiles.&lt;name&gt;.output_products is deprecated. Use 'product: &lt;id&gt;' instead.</c>
	/// Dedicated test: <c>BundleOutputConventionTests.ProfileWithOutputProducts_WarnsItIsDeprecated</c>.
	/// </summary>
	public static AmbientDiagnostic OutputProductsDeprecation { get; } = new(
		Severity.Warning,
		@"^bundle\.profiles\.[^.\s]+\.output_products is deprecated\b"
	);

	/// <summary>
	/// <c>Could not resolve a repository name for the bundle file ...</c>, for a config with no <c>bundle.repo</c> and no
	/// repository from the command line, the environment or a git origin.
	/// Dedicated test: <c>BundleOutputConventionTests.ProfileWithoutOutput_WritesConventionalName</c>.
	/// </summary>
	public static AmbientDiagnostic UnresolvedBundleRepository { get; } = new(Severity.Warning, @"^Could not resolve a repository name\b");

	public static IReadOnlyList<AmbientDiagnostic> All { get; } = [OutputProductsDeprecation, UnresolvedBundleRepository];
}

public static class AmbientDiagnostics
{
	/// <summary>
	/// The diagnostics without the allow-listed ambient ones: all of <see cref="AmbientDiagnostic.All"/>, or only the
	/// <paramref name="ambient"/> given (a test that is about one of them keeps the other out of the way, and the one it is
	/// about in).
	/// </summary>
	public static IEnumerable<Diagnostic> WithoutAmbient(this IEnumerable<Diagnostic> diagnostics, params AmbientDiagnostic[] ambient)
	{
		IReadOnlyList<AmbientDiagnostic> allowed = ambient.Length == 0 ? AmbientDiagnostic.All : ambient;
		return diagnostics.Where(d => !allowed.Any(a => a.Matches(d)));
	}
}
