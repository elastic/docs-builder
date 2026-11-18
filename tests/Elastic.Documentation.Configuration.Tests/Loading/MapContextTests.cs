// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Tests.Loading;

public class MapContextTests
{
	[Test]
	public void WarnAndHint_AreRecordedInOrder_AgainstTheFileWithTheirPosition()
	{
		var context = new MapContext("a.yml");

		context.Warn("w", line: 3, column: 4);
		context.Hint("h");

		context.File.Should().Be("a.yml");
		context
			.Findings
			.Select(d => (d.Severity, d.File, d.Message, d.Line, d.Column))
			.Should()
			.Equal((Severity.Warning, "a.yml", "w", 3, 4), (Severity.Hint, "a.yml", "h", null, null));
	}
}
