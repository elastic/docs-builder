// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Builder.Http;
using Nullean.Entities;

namespace Documentation.Builder.Tests;

/// <summary>What a snapshot of the service (<see cref="InMemoryBuildState.GetCurrentState"/>) must satisfy at rest.</summary>
internal static class BuildStateConstraints
{
	/// <summary>Conservation: every counted diagnostic is stored, and every stored one is counted.</summary>
	public static Constraint<BuildEvent> CountsMatchStoredDiagnostics { get; } = Constraints<BuildEvent>.Always(
		s => (s.Errors ?? 0) + (s.Warnings ?? 0) + (s.Hints ?? 0) == (s.Diagnostics?.Length ?? 0),
		"The error, warning and hint counts must add up to the stored diagnostics."
	);

	/// <summary>Invariant: the status is one the panel knows how to draw.</summary>
	public static Constraint<BuildEvent> StatusIsKnown { get; } = Constraints<BuildEvent>.Always(
		s => s.Status is "idle" or "building" or "complete",
		"The status must be idle, building or complete."
	);

	public static ConstraintSet<BuildEvent> All { get; } = ConstraintSet.From(
		("counts-match-stored-diagnostics", CountsMatchStoredDiagnostics),
		("status-is-known", StatusIsKnown)
	);
}
