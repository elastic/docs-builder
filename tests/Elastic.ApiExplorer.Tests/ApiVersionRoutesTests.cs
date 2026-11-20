// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionRoutesTests
{
	[Test]
	public void Suffixes_MainAndMajors_NewestMajorAtTheKeyAndMainAtNext() =>
		Table("elasticsearch", "main", "9", "8")
			.Should()
			.MatchInlineSnapshot(
				"""
			default: 9
			main -> elasticsearch/next
			9 -> elasticsearch, elasticsearch/v9
			8 -> elasticsearch/v8
			"""
			);

	[Test]
	public void Suffixes_MainOnly_MainStaysAtTheKey() =>
		Table("elasticsearch", "main").Should().MatchInlineSnapshot("""
		default: main
		main -> elasticsearch
		""");

	[Test]
	public void Suffixes_MajorsWithoutMain_NewestMajorAtTheKey() =>
		Table("elasticsearch", "9", "8")
			.Should()
			.MatchInlineSnapshot("""
		default: 9
		9 -> elasticsearch, elasticsearch/v9
		8 -> elasticsearch/v8
		""");

	[Test]
	public void Suffixes_VersionlessProduct_MainAtTheKey() =>
		Table("cloud-serverless", "main").Should().MatchInlineSnapshot("""
		default: main
		main -> cloud-serverless
		""");

	[Test]
	public void For_NoVersions_HasNoDefault() => ApiVersionRoutes.For("elasticsearch", []).DefaultMoniker.Should().BeNull();

	/// <summary>The default version, then each version with every URL suffix it renders at, the linked one first.</summary>
	private static string Table(string apiKey, params string[] monikers)
	{
		var routes = ApiVersionRoutes.For(apiKey, monikers);
		return string.Join(
			"\n",
			[$"default: {routes.DefaultMoniker}", .. monikers.Select(m => $"{m} -> {string.Join(", ", routes.Suffixes(m))}")]
		);
	}
}
