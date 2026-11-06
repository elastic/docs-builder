// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Markdown.Tests.Inline;

// The fake link index of TestCrossLinkResolver has one API section, kibana, with the page operation/operation-exists.
public static class ApiLinkAssertions
{
	public static void HasOneApiWarning(this TestDiagnosticsCollector collector, string url) =>
		collector
			.Diagnostics
			.Should()
			.ContainSingle(d => d.Severity == Severity.Warning && d.Message.Contains("API link") && d.Message.Contains(url));

	public static void HasApiWarning(this TestDiagnosticsCollector collector, string url) =>
		collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("API link") && d.Message.Contains(url));

	public static void HasNoApiWarningFor(this TestDiagnosticsCollector collector, string url) =>
		collector.Diagnostics.Should().NotContain(d => d.Message.Contains("API link") && d.Message.Contains(url));

	public static void HasNoApiWarning(this TestDiagnosticsCollector collector) =>
		collector.Diagnostics.Should().NotContain(d => d.Message.Contains("API link"));
}

public class ApiInlineLinkMissingTests() : InlineTest(
	"""
[Gone](https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing#responses)
"""
)
{
	[Test]
	public void WarnsAtTheLink() => Collector.HasOneApiWarning("operation-missing");

	[Test]
	public void WarnsWithoutFailingTheBuild() => Collector.Errors.Should().Be(0);
}

public class ApiInlineLinkExistsTests() : InlineTest(
	"""
[Here](https://www.elastic.co/docs/api/doc/kibana/operation/operation-exists) and [there](https://www.elastic.co/docs/api/doc/kibana/v8/operation/operation-only-in-8).
"""
)
{
	[Test]
	public void DoesNotWarn() => Collector.HasNoApiWarning();
}

public class ApiInlineLinkVersionMissingTests() : InlineTest(
	"""
[Gone](https://www.elastic.co/docs/api/doc/kibana/operation/operation-only-in-8)
"""
)
{
	[Test]
	public void WarnsWhenThePageIsOnlyInAnotherVersion() => Collector.HasOneApiWarning("operation-only-in-8");
}

public class ApiLinkWithoutASectionTests() : InlineTest(
	"""
[Unknown API](https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search) and [guide](https://www.elastic.co/docs/solutions/search).
"""
)
{
	[Test]
	public void DoesNotWarnWhenTheLinkIndexHasNoSectionForTheApi() => Collector.HasNoApiWarning();
}

public class ApiBareAutoLinkMissingTests() : InlineTest(
	"""
See https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing for details.
"""
)
{
	[Test]
	public void WarnsAtTheBareUrl() => Collector.HasOneApiWarning("operation-missing");
}

public class ApiAngleBracketAutoLinkMissingTests() : InlineTest(
	"""
See <https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing> for details.
"""
)
{
	[Test]
	public void WarnsAtTheAngleBracketLink() => Collector.HasOneApiWarning("operation-missing");
}

public class ApiLinksInCommentsTests() : InlineTest(
	"""
% https://www.elastic.co/docs/api/doc/kibana/operation/old-bare
% <https://www.elastic.co/docs/api/doc/kibana/operation/old-angle>
% [old](https://www.elastic.co/docs/api/doc/kibana/operation/old-inline)

Text.
"""
)
{
	[Test]
	public void DoNotWarn() => Collector.HasNoApiWarning();
}

public class ApiRedirectedPageTests() : InlineTest(
	"""
[Old root](https://www.elastic.co/docs/api/doc/kibana/operation/operation-exists) and [Servers](https://www.elastic.co/docs/api/doc/kibana/topic/topic-kibana-spaces).
"""
)
{
	[Test]
	public void ChecksTheTargetOfAnEdgeRedirect() =>
		// topic-kibana-spaces redirects to kibana-api-overview, which the fake section does not hold, so that one warns.
		Collector.HasOneApiWarning("topic-kibana-spaces");
}
