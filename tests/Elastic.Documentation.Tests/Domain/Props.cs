// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Fixed-seed property checks: the same inputs on every run, a few hundred iterations, shrinking on failure. The seed is shared on purpose;
/// a failure prints the shrunk input, and the law (not the seed) is what a reader needs. This file is linked into the other test projects that
/// carry operation and loader laws, so the convention is one.
/// </summary>
internal static class Props
{
	public const string Seed = "0N0XIzNsQ0O2";

	public static Gen<string?> OptionalString { get; } = Gen.Int[0, 3].SelectMany(
		n => n == 0 ? Gen.Int.Select(_ => (string?)null) : Gen.String.Select(s => (string?)s)
	);

	public static Gen<DateOnly?> OptionalDate { get; } = Gen.Int[0, 3].SelectMany(
		n => n == 0 ? Gen.Int.Select(_ => (DateOnly?)null) : Gen.DateOnly.Select(d => (DateOnly?)d)
	);

	public static void Check<T>(this Gen<T> gen, Action<T> assert, long iter = 200) =>
		gen.Sample(assert, seed: Seed, iter: iter, threads: 1);
}
