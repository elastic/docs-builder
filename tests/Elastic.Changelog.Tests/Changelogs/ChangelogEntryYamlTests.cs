// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs;

public class ChangelogEntryYamlTests
{
	[Test]
	public void WithoutSchemaComments_StripsTheFieldDocumentation_WhateverTheLineEnding()
	{
		var lf = WithoutSchemaComments("\n");
		lf.Should().MatchInlineSnapshot("""
			title: Feature
			type: feature
			""");

		// The snapshot key ignores line endings, so the CRLF run cannot be a second snapshot of its own.
		WithoutSchemaComments("\r\n").Should().Be(lf);
	}

	private static string WithoutSchemaComments(string newline) =>
		string.Join(newline, "# subtype: one of", "#   a", "", "title: Feature", "type: feature", "").WithoutSchemaComments();
}
