// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The seed of generated tests that take one explicitly (<c>AutoTest.Seed</c>, whose default is a fixed seed): random per run, or the one named by the
/// <c>CsCheck_Seed</c> environment variable, which CsCheck itself reads (it consumes <see cref="Check.Seed"/> on first use, so this reads the variable directly).
/// A failure prints its seed; replay it with <c>CsCheck_Seed=&lt;seed&gt; mise run test</c>.
/// </summary>
internal static class RunSeed
{
	public static string Value { get; } = Environment.GetEnvironmentVariable("CsCheck_Seed") is { Length: > 0 } fromEnvironment
		? fromEnvironment
		: Gen.Seed.Single();
}
