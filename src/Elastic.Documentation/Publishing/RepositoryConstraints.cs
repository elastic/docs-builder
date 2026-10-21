// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>The laws of the operations on a <see cref="Repository"/>: not about a loaded file, so they are not part of <see cref="AssemblyConstraints"/>.</summary>
[Constraints]
public static partial class RepositoryConstraints
{
	public static Constraint<Repository> ConfigurationCheckoutIsIdempotent { get; } = Constraints<Repository>.Idempotent(
		r => r.AsConfigurationCheckout(),
		"Checking out only the configuration of a configuration checkout must change nothing."
	).Describe("The configuration checkout of a repository is idempotent: doing it twice is doing it once.");
}
