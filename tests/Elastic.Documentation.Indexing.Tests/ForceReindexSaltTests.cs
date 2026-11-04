// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;

namespace Elastic.Documentation.Indexing.Tests;

public class ForceReindexSaltTests
{
	[Test]
	public void Create_NotForced_ReturnsEmpty() => ForceReindexSalt.Create(forceReindex: false).Should().BeEmpty();

	[Test]
	public void Create_Forced_ReturnsDistinctNonEmptyValuePerRun()
	{
		var first = ForceReindexSalt.Create(forceReindex: true);
		var second = ForceReindexSalt.Create(forceReindex: true);

		first.Should().NotBeEmpty();
		first.Should().NotBe(second);
	}

	[Test]
	public void Apply_EmptySalt_LeavesHashUnchanged() => ForceReindexSalt.Apply("abc123", string.Empty).Should().Be("abc123");

	[Test]
	public void Apply_Salt_ChangesHashDeterministically()
	{
		var salted = ForceReindexSalt.Apply("abc123", "salt");

		salted.Should().NotBe("abc123");
		salted.Should().Be(ForceReindexSalt.Apply("abc123", "salt"));
		salted.Should().NotBe(ForceReindexSalt.Apply("abc123", "other"));
	}
}
