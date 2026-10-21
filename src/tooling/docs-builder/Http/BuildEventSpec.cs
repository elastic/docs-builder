// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Documentation.Builder.Http;

/// <summary>
/// What a client of the live diagnostics stream may rely on: the events of the background validation build
/// (<see cref="InMemoryBuildState"/>) arrive as build_start, diagnostic*, then build_complete or build_cancelled, and every started build ends.
/// </summary>
internal static class BuildEventSpec
{
	private const string Start = "build_start";
	private const string Diagnostic = "diagnostic";
	private const string Complete = "build_complete";
	private const string Cancelled = "build_cancelled";

	private static bool Ends(BuildEvent e) => e.Type is Complete or Cancelled;

	/// <summary>Safety: nothing arrives out of order. A diagnostic only follows a start or another diagnostic, a start only follows an end.</summary>
	public static TemporalRule<BuildEvent> EventsFollowTheBuildLifecycle { get; } = Temporal<BuildEvent>.Transitions(
		(before, after) =>
			(before.Type, after.Type) switch
			{
				(Start or Diagnostic, Diagnostic) => true,
				(Start or Diagnostic, Complete or Cancelled) => true,
				(Complete or Cancelled, Start) => true,
				_ => false
			}
	);

	/// <summary>Liveness: a build that starts ends, however many diagnostics it makes on the way.</summary>
	public static TemporalRule<BuildEvent> EveryBuildEnds { get; } = Temporal<BuildEvent>.Eventually(
		scope: _ => "build",
		from: e => e.Type == Start,
		isTarget: Ends,
		message: "a build started and never completed or was cancelled"
	);

	/// <summary>Bounded: a build that starts ends within half an hour, or the editor's panel shows "building" for good.</summary>
	public static TemporalRule<BuildEvent> BuildsEndWithinHalfAnHour { get; } = Temporal<BuildEvent>.AfterUntil(
		isTrigger: e => e.Type == Start,
		isNext: (_, next) => Ends(next),
		window: TimeSpan.FromMinutes(30)
	);

	public static TemporalRuleSet<BuildEvent> All { get; } = TemporalRuleSet<BuildEvent>.From(
		("events-follow-the-build-lifecycle", EventsFollowTheBuildLifecycle),
		("every-build-ends", EveryBuildEnds),
		("builds-end-within-half-an-hour", BuildsEndWithinHalfAnHour)
	);
}

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
