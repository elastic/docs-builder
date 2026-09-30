// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The laws every aggregate's constraint list must obey, checked on models built directly, with no loader involved.
/// A valid model breaks no constraint. Every constraint has a counter-example that breaks it and only it, so none is
/// dead or duplicated, and no counter-example outlives its constraint.
/// </summary>
public abstract class ConstraintSetTests<TModel>
{
	protected abstract IReadOnlyList<Constraint<TModel>> Constraints { get; }

	protected abstract TModel Valid();

	/// <summary>For each constraint id, a change to <see cref="Valid"/> that breaks that constraint alone.</summary>
	protected abstract IReadOnlyDictionary<string, Func<TModel, TModel>> CounterExamples { get; }

	[Test]
	public void Valid_BreaksNoConstraint() => Constraints.CheckAll(Valid()).Should().BeEmpty();

	[Test]
	public void EveryConstraint_HasACounterExample() =>
		Constraints
			.Select(c => c.Id)
			.Should()
			.BeEquivalentTo(CounterExamples.Keys, "each constraint needs a counter-example, and each counter-example a constraint");

	[Test]
	public void ConstraintIds_AreUnique() => Constraints.Select(c => c.Id).Should().OnlyHaveUniqueItems();

	[Test]
	public void EveryCounterExample_BreaksOnlyItsConstraint()
	{
		foreach (var (constraintId, breakIt) in CounterExamples)
		{
			var violations = Constraints.CheckAll(breakIt(Valid()));
			violations
				.Select(v => v.ConstraintId)
				.Distinct()
				.Should()
				.Equal([constraintId], $"the counter-example for '{constraintId}' should break it and nothing else");
		}
	}
}
