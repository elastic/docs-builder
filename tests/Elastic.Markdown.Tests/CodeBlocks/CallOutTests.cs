// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using JetBrains.Annotations;

namespace Elastic.Markdown.Tests.CodeBlocks;

public abstract class CodeBlockCallOutTests(
	string language,
	[LanguageInjection("csharp")] string code,
	[LanguageInjection("markdown")] string? markdown = null
) : BlockTest<EnhancedCodeBlock>(
	$$"""
```{{language}}
{{code}}
```
{{markdown}}
"""
)
{
	[Test]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsLanguage() => Block!.Language.Should().Be("csharp");
}

public class MagicCalOuts() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; // this is a callout
//this is not a callout
var y = x - 2;
var z = y - 2; // another callout
"""
)
{
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.NotContain(c => c.Text.Contains("not a callout"));

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MagicCallOutWithFormatting() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; // this uses `formatting` and a [link](testing/req.md)
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/testing/req.md", new MockFileData("# Requirements"));

	[Test]
	public void RendersFormattedInlineMarkdown() =>
		Html.ShouldContainHtml(
			"""
			<ol class="code-callouts">
				<li>this uses <code>formatting</code> and a <a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">link</a></li>
			</ol>
			"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ClassicCallOutsRequiresContent() : CodeBlockCallOutTests("csharp", """
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""")
{
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void RequiresContentToFollow() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by any content"));
}

public class ClassicCallOutsNotFollowedByList() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void RequiresContentToFollow() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by a list"));
}

public class ClassicCallOutsFollowedByAListWithOneParagraph() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void RendersExpectedHtml() =>
		Html.ShouldBeHtml(
			"""
			<div class="highlight-csharp notranslate">
				<div class="highlight">
					<pre><code class="language-csharp">var x = 1;<span style="display: inline-block; width: 1ch"></span><span class="code-callout" data-index="1"></span>
					  var y = x - 2;
					  var z = y - 2;<span style="display: inline-block; width: 1ch"></span><span class="code-callout" data-index="2"></span>
					</code></pre>
			</div>
			</div>
			<p><strong>OUTPUT:</strong></p>
			<ol class="code-callouts">
			  <li>Marking the first callout</li>
			  <li>Marking the second callout</li>
			</ol>
			"""
		);

	[Test]
	public void AllowsAParagraphInBetween() => Collector.Diagnostics.Should().BeEmpty();
}

public class ClassicCallOutsFollowedByListButWithTwoParagraphs() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void RequiresContentToFollow() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.OnlyContain(c => c.Message.StartsWith("More than one content block between code block with annotations and its list"));
}

public class ClassicCallOutsFollowedByListWithWrongCoung() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void RequiresContentToFollow() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.OnlyContain(c => c.Message.StartsWith("Code block has 2 callouts but the following list only has 1"));
}

public class ClassicCallOutsReuseHighlights() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void SeesTwoUniqueCallouts() =>
		Block!.UniqueCallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void ParsesAllForLineInformation() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(3).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().BeEmpty();
}

public class ClassicCallOutWithTheRightListItems() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void ParsesClassicCallouts()
	{
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(9).And.OnlyContain(c => c.Text.StartsWith('<'));

		Block!.UniqueCallOuts.Should().NotBeNullOrEmpty().And.HaveCount(8);
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MultipleCalloutsInOneLine() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(3).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class CodeBlockWithChevronInsideCode() : CodeBlockCallOutTests(
	"csharp",
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
	[Test]
	public void ParsesMagicCallOuts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(5).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class CodeBlockWithCommentBlocksThenList() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
	"""
%  TEST[s/"basque_keywords",//]
%  TEST[s/\n$/\nstartyaml\n  - compare_analyzers: {index: basque_example, first: basque, second: rebuilt_basque}\nendyaml\n/]

1. First callout
2. Second callout
"""
)
{
	[Test]
	public void ParsesCallouts() =>
		Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2).And.OnlyContain(c => c.Text.StartsWith('<'));

	[Test]
	public void HandlesCommentBlocksCorrectly() => Collector.Diagnostics.Should().BeEmpty();

	[Test]
	public void RenderedHtmlContainsCallouts() =>
		Html.ShouldContainHtml(
			"""
			<ol class="code-callouts">
			<li>First callout</li>
			<li>Second callout</li>
			</ol>
			"""
		);
}

public class CodeBlockWithMultipleCommentTypesThenList() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
	"""
% This is an HTML-style comment that starts with %
%  TEST[catch:bad_request]

1. First callout
2. Second callout
"""
)
{
	[Test]
	public void ParsesCallouts() => Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2);

	[Test]
	public void HandlesCommentBlocksCorrectly() => Collector.Diagnostics.Should().BeEmpty();
}

public class CodeBlockWithCommentBlocksParagraphThenList() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
	"""
%  TEST[s/"basque_keywords",//]
%  TEST[catch:bad_request]

**This is an intermediate paragraph**

1. First callout
2. Second callout
"""
)
{
	[Test]
	public void ParsesCallouts() => Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2);

	[Test]
	public void HandlesCommentBlocksAndParagraphCorrectly() => Collector.Diagnostics.Should().BeEmpty();

	[Test]
	public void RendersIntermediateParagraph() =>
		Html.ShouldContainHtml(
			"""
			<p><strong>This is an intermediate paragraph</strong></p>
			<ol class="code-callouts">
				<li>First callout</li>
			    <li>Second callout</li>
			</ol>
			"""
		);
}

public class CodeBlockWithCommentBlocksTwoParagraphsThenList() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
	"""
%  TEST[s/"basque_keywords",//]

**This is an intermediate paragraph**

**This is a second paragraph which should cause an error**

1. First callout
2. Second callout
"""
)
{
	[Test]
	public void ParsesCallouts() => Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2);

	[Test]
	public void EmitsErrorForTooManyParagraphs() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.OnlyContain(c => c.Message.StartsWith("More than one content block between code block with annotations and its list"));
}

public class CodeBlockWithManyCommentBlocksNoList() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
	"""
%  TEST[s/"basque_keywords",//]
%  TEST[s/\n$/\nstartyaml\n  - compare_analyzers: {index: basque_example, first: basque, second: rebuilt_basque}\nendyaml\n/]
%  TEST[catch:bad_request]
"""
)
{
	[Test]
	public void ParsesCallouts() => Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2);

	[Test]
	public void EmitsErrorForNoList() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by a list"));
}

public class CodeBlockWithCommentsAfterList() : CodeBlockCallOutTests(
	"csharp",
	"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
	"""
1. First callout
2. Second callout

%  TEST[s/"basque_keywords",//]
"""
)
{
	[Test]
	public void ParsesCallouts() => Block!.CallOuts.Should().NotBeNullOrEmpty().And.HaveCount(2);

	[Test]
	public void HandlesCommentsCorrectly() => Collector.Diagnostics.Should().BeEmpty();

	[Test]
	public void RenderedHtmlDoesNotContainComments() => Html.Should().NotContain("basque_keywords");
}
