// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Codex.Building;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using FakeItEasy;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class CodexRedirectCollectionTests
{
	[Test]
	[Arguments("/docs/my-repo/docs/redirects.yml")]
	[Arguments("/docs/my-repo/docs/_redirects.yml")]
	public void CollectRedirects_UnresolvableCrossLink_ReportsErrorOnRedirectsFile(string redirectsFile)
	{
		var collector = A.Fake<IDiagnosticsCollector>();
		var resolver = A.Fake<ICrossLinkResolver>();
		Uri? resolved = null;
		A
			.CallTo(() => resolver.TryResolve(A<Action<string>>._, A<Uri>._, out resolved))
			.Invokes((Action<string> emit, Uri _, Uri? _) => emit("link not found"))
			.Returns(false);
		A.CallTo(() => resolver.UriResolver.Resolve(A<Uri>._, A<string>._)).Returns(new Uri("https://example.test/docs/old"));
		var redirects = new Dictionary<string, LinkRedirect> { ["old.md"] = new() { To = "other-repo://missing/page.md" } };

		CodexBuildService.CollectRedirects(
			[],
			redirects,
			new CodexBuildService.RedirectSource("my-repo", resolver, redirectsFile),
			collector
		);

		A.CallTo(
			() => collector.EmitError(redirectsFile, A<string>.That.Contains("other-repo://missing/page.md"), "link not found")
		).MustHaveHappenedOnceExactly();
	}
}
