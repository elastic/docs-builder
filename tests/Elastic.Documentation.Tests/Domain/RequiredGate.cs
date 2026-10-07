// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Lets a Laws test null a member to prove its generated <c>*-required</c> rule. The other rules are written for loaded values, where the required rule has
/// already held, so they read the member without a null check; behind this gate they say nothing about a value a required rule rejects. The required rules
/// themselves are untouched, and a value no required rule rejects meets every rule exactly as before. This file is linked into the other test projects.
/// </summary>
internal static class RequiredGate
{
	public static ConstraintSet<T> GatedByRequired<T>(this ConstraintSet<T> set)
	{
		var all = set.AsConstraints();
		var required = all.Where(IsRequired).ToList();

		bool Rejected(T value) => required.Any(r => r.Check(value, value).Any());

		return ConstraintSet.From([.. all.Select(rule => (rule.Name!, IsRequired(rule) ? rule : Gate(rule, Rejected)))]);
	}

	private static bool IsRequired<T>(Constraint<T> rule) => rule.Name is { } name && name.EndsWith("-required", StringComparison.Ordinal);

	private static Constraint<T> Gate<T>(Constraint<T> rule, Func<T, bool> rejected)
	{
		var gated = rule.Kind == ConstraintKind.PointInTime
			? Constraints<T>.Check(value => rejected(value) ? [] : rule.Check(value, value))
			: Constraints<T>.CheckTransition((previous, next) => rejected(previous) || rejected(next) ? [] : rule.Check(previous, next));
		gated = gated.Named(rule.Name!);
		if (rule.Description is { } description)
			gated = gated.Describe(description);
		return gated;
	}
}
