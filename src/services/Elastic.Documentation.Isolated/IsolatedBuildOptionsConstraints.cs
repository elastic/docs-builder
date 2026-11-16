// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Isolated;

/// <summary>
/// What a command line may not say with the options of <see cref="IsolatedBuildOptions"/>, checked by Argh before the command runs (registered with
/// <c>ArghConstraints.Register</c>): a flag that would be silently ignored is reported, not left to puzzle over.
/// </summary>
public static class IsolatedBuildOptionsConstraints
{
	/// <summary>The services read <c>--metadata-only</c> only when no exporters were given (<c>exporters ??= metadataOnly ? MetadataOnly : Default</c>).</summary>
	public static Constraint<IsolatedBuildOptions> MetadataOnlyAndExportersAreAlternatives { get; } = Constraints<IsolatedBuildOptions>
		.AtMostOne(("--metadata-only", o => o.MetadataOnly == true), ("--exporters", o => o.Exporters is not null))
		.WithSeverity(Severity.Warning)
		.Describe("--metadata-only is ignored when --exporters is given: say which exporters to run, or ask for metadata only.");

	public static ConstraintSet<IsolatedBuildOptions> All { get; } = ConstraintSet.From(
		("metadata-only-and-exporters-are-alternatives", MetadataOnlyAndExportersAreAlternatives)
	);
}
