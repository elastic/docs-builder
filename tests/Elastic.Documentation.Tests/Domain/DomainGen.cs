// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Generators for the domain aggregates, built from their types: <c>GenAuto</c> fills the members, and the <c>Normalize</c> functions say what a
/// loaded aggregate promises that a generator cannot guess (a derived member, a key that is its value's own id). A normalizer is the model's invariant
/// written once; the constraint set then checks it independently, so a normalizer that forgets a promise shows up as a generated value that breaks a rule.
/// </summary>
internal static class DomainGen
{
	/// <summary>A version with small components, so ordering and equality are exercised without prerelease and metadata noise.</summary>
	public static Gen<SemVersion> SemVersions { get; } = Gen.Select(
		Gen.Int[0, 20],
		Gen.Int[0, 9],
		Gen.Int[0, 9],
		(major, minor, patch) => new SemVersion(major, minor, patch)
	);

	/// <summary>Plausible values of <typeparamref name="T"/>, with the shared generators and the catalog invariants applied; <paramref name="configure"/> adds the aggregate's own.</summary>
	public static Gen<T> Of<T>(Action<AutoOptions>? configure = null) =>
		GenAuto.Of<T>(AutoMode.Plausible, o =>
		{
			o.Use(SemVersions).Catalogs();
			configure?.Invoke(o);
		});

	public static VersioningSystem DefaultVersioning(VersioningSystemId id) =>
		new() { Id = id, Base = new SemVersion(8, 0, 0), Current = new SemVersion(9, 1, 0) };

	/// <summary>
	/// What a loaded <c>versions.yml</c> and <c>products.yml</c> promise: a versioning system for every id, never based after its current version;
	/// products keyed by their own id, each with a display name and a versioning system defined in the catalog it sits in.
	/// </summary>
	public static AutoOptions Catalogs(this AutoOptions o) =>
		o
			.Normalize<VersioningSystem>(v => v.Base > v.Current ? v with { Base = v.Current, Current = v.Base } : v)
			.Normalize<VersioningSystemCatalog>(
				c => new VersioningSystemCatalog
				{
					VersioningSystems = VersioningSystemIdExtensions
						.GetValues()
						.Where(id => id != VersioningSystemId.None)
						.ToDictionary(
							id => id,
							id => c.VersioningSystems.TryGetValue(id, out var v) ? v with { Id = id } : DefaultVersioning(id)
						)
				}
			)
			.Normalize<Product>(
				p =>
					p with
					{
						DisplayName = string.IsNullOrWhiteSpace(p.DisplayName) ? p.Id : p.DisplayName,
						VersioningSystem = p.VersioningSystem is null || p.VersioningSystem.Id == VersioningSystemId.None
							? DefaultVersioning(VersioningSystemId.Stack)
							: p.VersioningSystem
					}
			)
			.Normalize<ProductCatalog>(
				c => ProductCatalog.FromPublicProducts(
					c.Products.Values.GroupBy(p => p.Id).Select(g => g.First()).Select(p => KeyValuePair.Create(p.Id, p))
				)
			);
}
