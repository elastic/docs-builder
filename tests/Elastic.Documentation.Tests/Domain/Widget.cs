// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>A minimal record and constraint set, so the foundation types are tested without any area's model.</summary>
public sealed record Widget(string Name, int Size);

public static class WidgetRules
{
	public static Constraint<Widget> NameRequired { get; } = Constraints<Widget>.Always(
		w => !string.IsNullOrEmpty(w.Name),
		"Widget has no name."
	).Named("name-required");

	public static Constraint<Widget> SizeSmall { get; } = Constraints<Widget>
		.Always(w => w.Size <= 10, "Widget is large.")
		.WithSeverity(Nullean.Entities.Severity.Warning)
		.Named("size-small");

	public static Constraint<Widget> SizePositive { get; } = Constraints<Widget>
		.Always(w => w.Size > 0, "Widget has no size.")
		.WithSeverity(Nullean.Entities.Severity.Hint)
		.Named("size-positive");

	public static ConstraintSet<Widget> All { get; } = ConstraintSet.From(
		("name-required", NameRequired),
		("size-small", SizeSmall),
		("size-positive", SizePositive)
	);

	public static Widget Valid { get; } = new("ok", 5);
}
