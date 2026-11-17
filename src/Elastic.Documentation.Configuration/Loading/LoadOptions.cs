// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Nullean.Entities;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>What a loader changes about <see cref="LoaderPipeline"/>'s defaults; every member is optional.</summary>
internal sealed record LoadOptions<T> where T : class
{
	/// <summary>
	/// Findings made while mapping (warnings the loader raises itself). The list is read after the map step ran, so the step may fill it; when the step then
	/// throws, these come first in the failed result, before the error.
	/// </summary>
	public IReadOnlyList<Diagnostic> Extra { get; init; } = [];

	/// <summary>The file to report violations against when the record knows it better than the loader (a legacy file name).</summary>
	public Func<T, string?>? FileOf { get; init; }

	/// <summary>When false the value is returned even when it has errors, for a caller that decides whether to stop.</summary>
	public bool ErrorsAreFatal { get; init; } = true;

	/// <summary>
	/// The set to check against instead of <c>T.Constraints</c>: a record that a different set checks at one stage (a read-time set) or a second set
	/// (<c>ConstraintSet.Merge(first, second)</c>).
	/// </summary>
	public ConstraintSet<T>? Rules { get; init; }

	/// <summary>The text of the error diagnostic for a step that threw. The default is the exception's message, so a loader keeps the text it always gave.</summary>
	public Func<Exception, string> Describe { get; init; } = static e => e.Message;
}
