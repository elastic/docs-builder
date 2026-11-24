// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.RelatedLearning;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What <c>related-learning.yml</c> loads into today.</summary>
public class RelatedLearningGoldenTests
{
	private static string Load(string yaml) =>
		GoldenConfig.Outcome(GoldenConfig.RelatedLearning, yaml, p => p.CreateRelatedLearningConfiguration());

	[Test]
	public void Baseline_Loaded_MatchesSnapshot() => Load(GoldenConfig.BaselineText(GoldenConfig.RelatedLearning)).Should().MatchSnapshot();

	[Test]
	public void EmptyFile_Loaded_Outcome() =>
		Load("")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.RelatedLearning.RelatedLearningConfigDto'.
		"""
			);

	[Test]
	public void EmptyLinks_Loaded_Outcome() =>
		Load("links: {}\n").Should().MatchInlineSnapshot("""
		model <RelatedLearningConfiguration>:
		  Links: {}
		""");

	[Test]
	public void UnknownTopLevelKey_Loaded_Outcome() =>
		Load("bogus: 1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/related-learning.yml: (Lin: 0, Col: 0, Chr: 0) - (Lin: 0, Col: 7, Chr: 7): (1,1) Unknown key 'bogus'. Valid keys: links.
				"""
			);

	[Test]
	public void UnknownLinkKey_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: https://example.com\n    icon: x\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/related-learning.yml: (Lin: 4, Col: 4, Chr: 58) - (Lin: 4, Col: 10, Chr: 64): (5,5) Unknown key 'icon' in $.links.a. Valid keys: title, url.
				"""
			);

	[Test]
	public void MissingTitle_Loaded_Outcome() =>
		Load("links:\n  a:\n    url: https://example.com\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: related-learning.yml link 'a' is missing required 'title'.
		"""
			);

	[Test]
	public void BlankTitle_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: '  '\n    url: https://example.com\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			InvalidOperationException: related-learning.yml link 'a' is missing required 'title'.
			"""
			);

	[Test]
	public void MissingUrl_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: related-learning.yml link 'a' is missing required 'url'.
		"""
			);

	[Test]
	public void BlankUrl_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: related-learning.yml link 'a' is missing required 'url'.
		"""
			);

	[Test]
	public void RelativeUrl_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: /training/a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: related-learning.yml link 'a' has invalid url '/training/a'. Every url must be an absolute http or https URL.
		"""
			);

	[Test]
	public void SchemeRelativeUrl_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: //example.com/a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			InvalidOperationException: related-learning.yml link 'a' has invalid url '//example.com/a'. Every url must be an absolute http or https URL.
			"""
			);

	[Test]
	public void FtpUrl_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: ftp://example.com/a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			InvalidOperationException: related-learning.yml link 'a' has invalid url 'ftp://example.com/a'. Every url must be an absolute http or https URL.
			"""
			);

	[Test]
	public void JavascriptUrl_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: 'javascript:alert(1)'\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			InvalidOperationException: related-learning.yml link 'a' has invalid url 'javascript:alert(1)'. Every url must be an absolute http or https URL.
			"""
			);

	[Test]
	public void UppercaseSchemeAndPlainHttp_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: HTTP://example.com/a\n  b:\n    title: B\n    url: http://example.com/b\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <RelatedLearningConfiguration>:
				  Links:
				    a <RelatedLearningLink>:
				      Id: "a"
				      Title: "A"
				      Url: "HTTP://example.com/a"
				    b <RelatedLearningLink>:
				      Id: "b"
				      Title: "B"
				      Url: "http://example.com/b"
				"""
			);

	[Test]
	public void UrlWithSpacesAndFragment_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: 'https://example.com/a b#frag'\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <RelatedLearningConfiguration>:
			  Links:
			    a <RelatedLearningLink>:
			      Id: "a"
			      Title: "A"
			      Url: "https://example.com/a b#frag"
			"""
			);

	[Test]
	public void DuplicateId_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: https://example.com/a\n  a:\n    title: B\n    url: https://example.com/b\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/related-learning.yml: (Lin: 5, Col: 4, Chr: 65) - (Lin: 5, Col: 4, Chr: 65): Duplicate mapping key 'a'.
				"""
			);

	[Test]
	public void IdsDifferingByCase_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: A\n    url: https://example.com/a\n  A:\n    title: B\n    url: https://example.com/b\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <RelatedLearningConfiguration>:
				  Links:
				    A <RelatedLearningLink>:
				      Id: "A"
				      Title: "B"
				      Url: "https://example.com/b"
				    a <RelatedLearningLink>:
				      Id: "a"
				      Title: "A"
				      Url: "https://example.com/a"
				"""
			);

	[Test]
	public void BlankId_Loaded_Outcome() =>
		Load("links:\n  '':\n    title: A\n    url: https://example.com/a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <RelatedLearningConfiguration>:
			  Links:
			     <RelatedLearningLink>:
			      Id: ""
			      Title: "A"
			      Url: "https://example.com/a"
			"""
			);

	[Test]
	public void LinksAsList_Loaded_Outcome() =>
		Load("links:\n  - title: A\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/related-learning.yml: (Lin: 1, Col: 2, Chr: 9) - (Lin: 1, Col: 2, Chr: 9): Expected a StartMapping token but found 'StartSequence'.
				"""
			);

	[Test]
	public void LinkWithoutBody_Loaded_Outcome() =>
		Load("links:\n  a:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		NullReferenceException: Object reference not set to an instance of an object.
		"""
			);

	[Test]
	public void TitleAsNumber_Loaded_Outcome() =>
		Load("links:\n  a:\n    title: 42\n    url: https://example.com/a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <RelatedLearningConfiguration>:
			  Links:
			    a <RelatedLearningLink>:
			      Id: "a"
			      Title: "42"
			      Url: "https://example.com/a"
			"""
			);

	[Test]
	public void LinkOrderIsKept_Loaded_Outcome() =>
		Load("links:\n  z:\n    title: Z\n    url: https://example.com/z\n  a:\n    title: A\n    url: https://example.com/a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <RelatedLearningConfiguration>:
				  Links:
				    a <RelatedLearningLink>:
				      Id: "a"
				      Title: "A"
				      Url: "https://example.com/a"
				    z <RelatedLearningLink>:
				      Id: "z"
				      Title: "Z"
				      Url: "https://example.com/z"
				"""
			);
}
