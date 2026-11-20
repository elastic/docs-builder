// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Diagnostics;

public static class CheckExtensions
{
	/// <summary>
	/// Checks a record built elsewhere (not by a <c>Loader</c>) once against its own <c>T.Constraints</c> and returns it with every finding, in the order the
	/// set reported them, attributed to <paramref name="file"/> unless a rule pointed at its own file. This is the one runner of a constraint set that code
	/// outside this assembly may call: <c>BannedSymbols.txt</c> bans <c>ConstraintSet&lt;T&gt;.Report</c> and friends everywhere else, so it lives here, next
	/// to <see cref="Loaded{T}"/>, and is public so a service can call <c>new SomeStage(record).Check(file).Findings</c> without a wrapper per type.
	/// It deliberately does not decide what an error means: the result is always <see cref="Loaded{T}"/>, even when a rule of Error severity was violated.
	/// Fatality is the caller's choice (a loader turns errors into <see cref="Unreadable{T}"/>, a service may report and continue). It takes no rule set and
	/// no options: the rules are the type's own, so a call site cannot pick, add or skip a rule.
	/// </summary>
	public static Loaded<T> Check<T>(this T value, string file) where T : class, IHaveConstraints<T> =>
		new(value, T.Constraints.Report(value).ToDiagnostics(file));
}
