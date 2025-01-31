// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.CodeBlocks;

public abstract class CodeBlockCallOutTests(
	ITestOutputHelper output,
	string language,
	[LanguageInjection("csharp")] string code,
	[LanguageInjection("markdown")] string? markdown = null
)
	: BlockTest<EnhancedCodeBlock>(output,
$$"""
```{{language}}
{{code}}
```
{{markdown}}
"""
)
{
	[Fact]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsLanguage() => Block!.Language.Should().Be("csharp");

}

public class MagicCalOuts(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; // this is a callout
//this is not a callout
var y = x - 2;
var z = y - 2; // another callout
"""
	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.NotContain(c => c.Text.Contains("not a callout"));

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ClassicCallOutsRequiresContent(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
"""
	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().HaveCount(1)
		.And.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by any content"));
}

public class ClassicCallOutsNotFollowedByList(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
"""
## hello world
"""

	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().HaveCount(1)
		.And.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by a list"));
}


public class ClassicCallOutsFollowedByAListWithOneParagraph(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
"""

**OUTPUT:**

1. Marking the first callout
2. Marking the second callout
"""

	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void AllowsAParagraphInBetween() => Collector.Diagnostics.Should().BeEmpty();
}

public class ClassicCallOutsFollowedByListButWithTwoParagraphs(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
"""

**OUTPUT:**

BLOCK TWO

1. Marking the first callout
2. Marking the second callout
"""

	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().HaveCount(1)
		.And.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by a list"));
}



public class ClassicCallOutsFollowedByListWithWrongCoung(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
"""
1. Only marking the first callout
"""

	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().HaveCount(1)
		.And.OnlyContain(c => c.Message.StartsWith("Code block has 2 callouts but the following list only has 1"));
}

public class ClassicCallOutsReuseHighlights(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2; <2>
var z = y - 2; <2>
""",
"""
1. The first
2. The second appears twice
"""

	)
{
	[Fact]
	public void SeesTwoUniqueCallouts() => Block!.UniqueCallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void ParsesAllForLineInformation() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(3)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().BeEmpty();
}

public class ClassicCallOutWithTheRightListItems(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
receivers: <1>
  # ...
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318
processors: <2>
  # ...
  memory_limiter:
    check_interval: 1s
    limit_mib: 2000
  batch:

exporters:
  debug:
    verbosity: detailed <3>
  otlp: <4>
    # Elastic APM server https endpoint without the "https://" prefix
    endpoint: "${env:ELASTIC_APM_SERVER_ENDPOINT}" <5> <7>
    headers:
      # Elastic APM Server secret token
      Authorization: "Bearer ${env:ELASTIC_APM_SECRET_TOKEN}" <6> <7>

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [..., memory_limiter, batch]
      exporters: [debug, otlp]
    metrics:
      receivers: [otlp]
      processors: [..., memory_limiter, batch]
      exporters: [debug, otlp]
    logs: <8>
      receivers: [otlp]
      processors: [..., memory_limiter, batch]
      exporters: [debug, otlp]
""",
"""
1. The receivers, like the OTLP receiver, that forward data emitted by APM agents, or the host metrics receiver.
2. We recommend using the Batch processor and the memory limiter processor. For more information, see recommended processors.
3. The debug exporter is helpful for troubleshooting, and supports configurable verbosity levels: basic (default), normal, and detailed.
4. Elastic {observability} endpoint configuration. APM Server supports a ProtoBuf payload via both the OTLP protocol over gRPC transport (OTLP/gRPC) and the OTLP protocol over HTTP transport (OTLP/HTTP). To learn more about these exporters, see the OpenTelemetry Collector documentation: OTLP/HTTP Exporter or OTLP/gRPC exporter. When adding an endpoint to an existing configuration an optional name component can be added, like otlp/elastic, to distinguish endpoints as described in the OpenTelemetry Collector Configuration Basics.
5. Hostname and port of the APM Server endpoint. For example, elastic-apm-server:8200.
6. Credential for Elastic APM secret token authorization (Authorization: "Bearer a_secret_token") or API key authorization (Authorization: "ApiKey an_api_key").
7. Environment-specific configuration parameters can be conveniently passed in as environment variables documented here (e.g. ELASTIC_APM_SERVER_ENDPOINT and ELASTIC_APM_SECRET_TOKEN).
8. [preview] To send OpenTelemetry logs to {stack} version 8.0+, declare a logs pipeline.
"""

	)
{
	[Fact]
	public void ParsesClassicCallouts()
	{
		Block!.CallOuts
			.Should().NotBeNullOrEmpty()
			.And.HaveCount(9)
			.And.OnlyContain(c => c.Text.StartsWith("<"));

		Block!.UniqueCallOuts
			.Should().NotBeNullOrEmpty()
			.And.HaveCount(8);
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MultipleCalloutsInOneLine(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
	"""
	var x = 1; // <1>
	var y = x - 2;
	var z = y - 2; // <1> <2>
	""",
	"""
	1. First callout
	2. Second callout
	"""
)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(3)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class CodeBlockWithChevronInsideCode(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
	"""
	app.UseFilter<StopwatchFilter>(); <1>
	app.UseFilter<CatchExceptionFilter>(); <2>

	var x = 1; <1>
	var y = x - 2;
	var z = y - 2; <1> <2>
	""",
	"""
	1. First callout
	2. Second callout
	"""
)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(5)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
