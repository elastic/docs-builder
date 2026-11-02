// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Build.Tests;

public class ApiLinkValidatorTests
{
	private static readonly IReadOnlySet<string> Generated = new HashSet<string>
	{
		"/docs/api/doc/kibana",
		"/docs/api/doc/kibana/v8/operation/operation-upgrade",
		"/docs/api/doc/kibana/operation/operation-alias"
	};

	private static ApiLinkReference Link(string url, string file = "a.md", int line = 1) => new(url, file, line, 1, url.Length);

	[Test]
	public void FindBroken_ExistingPage_IsNotReported() =>
		ApiLinkValidator.FindBroken([Link("https://www.elastic.co/docs/api/doc/kibana/")], Generated).Should().BeEmpty();

	[Test]
	public void FindBroken_PageOnlyInOlderVersion_ReportsMainTreeLink()
	{
		var broken = ApiLinkValidator.FindBroken(
			[
				Link("https://www.elastic.co/docs/api/doc/kibana/operation/operation-upgrade"),
				Link("https://www.elastic.co/docs/api/doc/kibana/v8/operation/operation-upgrade#responses")
			],
			Generated
		);
		broken.Should().ContainSingle().Which.Url.Should().EndWith("/kibana/operation/operation-upgrade");
	}

	[Test]
	public void FindBroken_AliasUrl_Resolves() =>
		ApiLinkValidator
			.FindBroken([Link("https://www.elastic.co/docs/api/doc/kibana/operation/operation-alias")], Generated)
			.Should()
			.BeEmpty();

	[Test]
	public void FindBroken_MultipleBroken_AreOrderedByUrlThenSource()
	{
		var broken = ApiLinkValidator.FindBroken(
			[
				Link("https://www.elastic.co/docs/api/doc/kibana/zzz", "b.md"),
				Link("https://www.elastic.co/docs/api/doc/kibana/aaa", "b.md"),
				Link("https://www.elastic.co/docs/api/doc/kibana/aaa", "a.md")
			],
			Generated
		);
		broken.Select(b => (b.Url.Split('/')[^1], b.File)).Should().Equal(("aaa", "a.md"), ("aaa", "b.md"), ("zzz", "b.md"));
	}
}
