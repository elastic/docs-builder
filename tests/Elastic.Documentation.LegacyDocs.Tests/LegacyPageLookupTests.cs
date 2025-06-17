// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using FluentAssertions;

namespace Elastic.Documentation.LegacyDocs.Tests;

public class LegacyPageCheckerTests
{
	[Fact]
	public void TestVersions()
	{
		var legacyPageChecker = new LegacyPageChecker();
		var expected = new Dictionary<string, bool>
		{
			["8.0"] = false,
			["8.1"] = false,
			["8.2"] = false,
			["8.3"] = false,
			["8.4"] = false,
			["8.5"] = false,
			["8.6"] = false,
			["8.7"] = false,
			["8.8"] = false,
			["8.9"] = false,
			["8.10"] = false,
			["8.11"] = false,
			["8.12"] = false,
			["8.13"] = false,
			["8.14"] = false,
			["8.15"] = true,
			["8.16"] = true,
			["8.17"] = true,
			["8.18"] = true,
		};
		foreach (var (version, value) in expected)
		{
			var result = legacyPageChecker.PathExists(
				$"/guide/en/elasticsearch/reference/{version}/elasticsearch-intro-what-is-es.html"
			);
			_ = result.Should().Be(value, $"Expected {version} to be {value}");
		}
	}
}
