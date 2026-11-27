// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.AppliesTo;

/// <summary>
/// How a feature moves through its lifecycle as the versions of a product go by: planned, in development, a preview or an experiment, beta, generally
/// available, deprecated, removed. A stage may be skipped, never revisited. <c>unavailable</c> (a feature that is not there and not coming) and
/// <c>discontinued</c> (an old spelling of removed) are outside the progression. The diagram is generated from this declaration
/// (<c>Machine.ToMermaid()</c>); <see cref="Rank"/> is what the applies_to rule reads, and a test holds the two to each other.
/// </summary>
public static class ProductLifecycleMachine
{
	public static StateMachine<ProductLifecycle, string> Machine { get; } = StateMachine
		.Define<ProductLifecycle, string>(ProductLifecycle.Planned)
		.Permit("Develop", ProductLifecycle.Planned, ProductLifecycle.Development)
		.Permit("Preview", [ProductLifecycle.Planned, ProductLifecycle.Development], ProductLifecycle.TechnicalPreview)
		.Permit("Experiment", [ProductLifecycle.Planned, ProductLifecycle.Development], ProductLifecycle.Experimental)
		.Permit(
			"Beta",
			[ProductLifecycle.Development, ProductLifecycle.TechnicalPreview, ProductLifecycle.Experimental],
			ProductLifecycle.Beta
		)
		.Permit(
			"Release",
			[ProductLifecycle.Development, ProductLifecycle.TechnicalPreview, ProductLifecycle.Experimental, ProductLifecycle.Beta],
			ProductLifecycle.GenerallyAvailable
		)
		.Permit("Deprecate", ProductLifecycle.GenerallyAvailable, ProductLifecycle.Deprecated)
		.Permit("Remove", [ProductLifecycle.GenerallyAvailable, ProductLifecycle.Deprecated], ProductLifecycle.Removed)
		.Build();

	/// <summary>How far along a lifecycle is; a preview and an experiment are the same stage. Null for one outside the progression.</summary>
	public static int? Rank(ProductLifecycle lifecycle) => lifecycle switch
	{
		ProductLifecycle.Planned => 0,
		ProductLifecycle.Development => 1,
		ProductLifecycle.TechnicalPreview or ProductLifecycle.Experimental => 2,
		ProductLifecycle.Beta => 3,
		ProductLifecycle.GenerallyAvailable => 4,
		ProductLifecycle.Deprecated => 5,
		ProductLifecycle.Removed => 6,
		_ => null
	};
}
