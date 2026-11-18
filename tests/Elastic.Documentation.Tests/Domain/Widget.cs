// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The template an aggregate follows: a record that implements <see cref="IHaveConstraints{TSelf}"/> with one static set, declared in a
/// <c>*Constraints</c> class, and operations that are plain public methods returning the record (<c>AutoTest</c> runs each against every rule).
/// </summary>
public sealed record Widget(string Name, int Size) : IHaveConstraints<Widget>
{
	public static ConstraintSet<Widget> Constraints => WidgetConstraints.All;

	public Widget Grow() => this with { Size = Size + 1 };

	public Widget Rename(string name) => this with { Name = name };

	public Widget Tidy() => this with { Name = Name.Trim() };
}

[Constraints<Widget>]
public static partial class WidgetConstraints
{
	public const int MaxSize = 10;

	public static readonly Constraint<Widget> NameRequired = Constraints<Widget>.Always(
		w => !string.IsNullOrEmpty(w.Name),
		"Widget has no name."
	);

	public static readonly Constraint<Widget> SizeSmall = Constraints<Widget>.Always(
		w => w.Size <= MaxSize,
		"Widget is large."
	).WithSeverity(Severity.Warning);

	public static readonly Constraint<Widget> SizePositive = Constraints<Widget>.Always(
		w => w.Size > 0,
		"Widget has no size."
	).WithSeverity(Severity.Hint);

	public static readonly Constraint<Widget> TidyIsIdempotent = Constraints<Widget>.Idempotent(w => w.Tidy());

	public static Widget Valid { get; } = new("ok", 5);
}

/// <summary>A widget that is still being written: only a name is required.</summary>
public sealed record DraftWidget(string Name) : IHaveConstraints<DraftWidget>
{
	public static ConstraintSet<DraftWidget> Constraints => DraftWidgetConstraints.All;
}

[Constraints<DraftWidget>]
public static partial class DraftWidgetConstraints
{
	public static readonly Constraint<DraftWidget> NameRequired = Constraints<DraftWidget>.Always(
		w => !string.IsNullOrEmpty(w.Name),
		"A draft needs a name."
	);
}

/// <summary>The same widget once it is published: it also needs a description. A rule that holds at one stage is a type, not an option on a loader.</summary>
public sealed record PublishedWidget(string Name, string Description) : IHaveConstraints<PublishedWidget>
{
	public static ConstraintSet<PublishedWidget> Constraints => PublishedWidgetConstraints.All;
}

[Constraints<PublishedWidget>]
public static partial class PublishedWidgetConstraints
{
	public static readonly Constraint<PublishedWidget> NameRequired = Constraints<PublishedWidget>.Always(
		w => !string.IsNullOrEmpty(w.Name),
		"A published widget needs a name."
	);

	public static readonly Constraint<PublishedWidget> DescriptionRequired = Constraints<PublishedWidget>.Always(
		w => !string.IsNullOrEmpty(w.Description),
		"A published widget needs a description."
	);
}

/// <summary>The stages of a widget. A switch over it must name every stage: a missing one is warning CS8509, which is an error in the src projects.</summary>
public readonly union WidgetStage(DraftWidget, PublishedWidget);

public static class WidgetStageExtensions
{
	public static IReadOnlyList<string> RuleIds(this WidgetStage stage) => stage switch
	{
		DraftWidget draft => [.. DraftWidget.Constraints.Report(draft).Violations.Select(v => v.ConstraintId!)],
		PublishedWidget published => [.. PublishedWidget.Constraints.Report(published).Violations.Select(v => v.ConstraintId!)]
	};
}
