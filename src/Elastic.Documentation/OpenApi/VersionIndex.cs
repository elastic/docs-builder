// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.OpenApi;

/// <summary>One version moniker of a spec file: <c>main</c> or a major number, and the version it resolves to (<c>main</c>, <c>9.5</c>).</summary>
public sealed record VersionMoniker(string Key, string Version);

/// <summary>
/// The monikers published for one spec file of a repository, in the order the index wrote them. A JSON null in the index is kept as it was written
/// (a null <paramref name="Monikers"/>, a null element), so reading it fails where it always did.
/// </summary>
public sealed record VersionIndexSpec(string File, IReadOnlyList<VersionMoniker> Monikers)
{
	public bool Equals(VersionIndexSpec? other) =>
		other is not null && File == other.File && (Monikers ?? []).SequenceEqual(other.Monikers ?? []);

	public override int GetHashCode() => HashCode.Combine(File, Monikers?.Count);
}

/// <summary>
/// The spec files published for one <c>org/repo</c>, in the order the index wrote them. A JSON null in the index is kept as written
/// (a null <paramref name="Specs"/>), so looking the repository up fails where it always did.
/// </summary>
public sealed record VersionIndexRepository(string Name, IReadOnlyList<VersionIndexSpec> Specs)
{
	public bool Equals(VersionIndexRepository? other) =>
		other is not null && Name == other.Name && (Specs ?? []).SequenceEqual(other.Specs ?? []);

	public override int GetHashCode() => HashCode.Combine(Name, Specs?.Count);

	/// <summary>Looks a spec file up. Throws <see cref="NullReferenceException"/> when the index wrote this repository as null.</summary>
	public bool TryGetSpec(string file, out IReadOnlyList<VersionMoniker> monikers)
	{
		for (var i = 0; i < Specs.Count; i++)
		{
			if (Specs[i].File != file)
				continue;
			monikers = Specs[i].Monikers;
			return true;
		}
		monikers = [];
		return false;
	}
}

/// <summary>
/// The version index published at the root of the OpenAPI spec bucket, keyed by <c>org/repo</c>, then spec file name, then version moniker.
/// Every level keeps the order the index wrote it in.
/// </summary>
public sealed record VersionIndex(IReadOnlyList<VersionIndexRepository> Repositories) : IHaveConstraints<VersionIndex>
{
	public static ConstraintSet<VersionIndex> Constraints => VersionIndexConstraints.All;

	public bool Equals(VersionIndex? other) => other is not null && Repositories.SequenceEqual(other.Repositories);

	public override int GetHashCode() => Repositories.Count;

	public bool TryGetRepository(string name, out VersionIndexRepository repository)
	{
		repository = Repositories.FirstOrDefault(r => r.Name == name)!;
		return repository is not null;
	}

	/// <summary>Every moniker with the repository and spec file it sits under, in written order. Nulls the index wrote are skipped.</summary>
	public IEnumerable<(string Repository, string File, VersionMoniker Moniker)> AllMonikers() =>
		Repositories.SelectMany(
			r => (r.Specs ?? []).SelectMany(s => (s.Monikers ?? []).OfType<VersionMoniker>().Select(m => (r.Name, s.File, m)))
		);
}
