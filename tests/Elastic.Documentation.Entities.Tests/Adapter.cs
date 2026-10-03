// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Constraints;
using Nullean.Entities;
using Finding = Elastic.Documentation.Constraints.Finding;
using Severity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Entities.Tests;

/// <summary>The whole bridge between the two: a Nullean.Entities set as this repository's constraint list, so its collectors, reports and laws harness apply unchanged.</summary>
public static class Adapter
{
	public static IReadOnlyList<Elastic.Documentation.Constraints.Constraint<TModel>> ToDomain<TModel>(this ConstraintSet<TModel> set) =>
		[
			.. set.AsConstraints().Select(
				c => new Elastic.Documentation.Constraints.Constraint<TModel>(
					c.Name ?? "unnamed",
					c.Description ?? c.Name ?? "",
					model => c.Check(model, model).Select(ToFinding)
				)
			)
		];

	public static Finding ToFinding(this Violation violation) =>
		new(
			violation.Severity switch
			{
				Nullean.Entities.Severity.Error => Severity.Error,
				Nullean.Entities.Severity.Warning => Severity.Warning,
				_ => Severity.Hint,
			},
			violation.Message,
			violation.Line,
			violation.Column,
			violation.File,
			violation.Length
		);
}
