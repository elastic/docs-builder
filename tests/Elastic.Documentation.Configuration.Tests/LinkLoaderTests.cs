// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// <see cref="LinkLoader"/> is the only way a <c>links.json</c> or <c>link-index.json</c> becomes a record, from a file, S3 text, a git clone or the
/// application cache: it reports every violation once and never throws on text.
/// </summary>
public class LinkLoaderTests
{
	// three rules broken at once: a page that is not markdown (error), a page path with two .md (warning), a prefix without a leading slash (warning)
	private const string BrokenManifest =
		"""
		{
		  "origin": { "branch": "main", "remote": "https://example.test/repo.git", "ref": "abc", "name": "repo" },
		  "url_path_prefix": "docs/",
		  "links": {
		    "index.md": { "anchors": [] },
		    "notes.txt": { "anchors": [] },
		    "a.md.md": { "anchors": [] }
		  },
		  "cross_links": []
		}
		""";

	private const string ValidManifest =
		"""
		{
		  "origin": { "branch": "main", "remote": "https://example.test/repo.git", "ref": "abc", "name": "repo" },
		  "url_path_prefix": "/docs",
		  "links": { "index.md": { "anchors": ["a"] } },
		  "cross_links": []
		}
		""";

	private const string RegistryWithoutMain =
		"""
		{
		  "repositories": {
		    "kibana": {
		      "8.x": { "repository": "kibana", "path": "elastic/kibana/8.x/links.json", "branch": "8.x", "etag": "e", "ref": "abc", "updated_at": "2026-01-01T00:00:00Z" }
		    }
		  }
		}
		""";

	[Test]
	public void LoadManifest_ReportsEveryViolationOnce_AndStillReturnsTheManifest()
	{
		var result = LinkLoader.LoadManifest(BrokenManifest, "elastic/repo/main/links.json");

		result.Value.Should().NotBeNull("a manifest that parses is returned with its findings; the caller decides what stops it");
		result.HasErrors.Should().BeTrue();
		var ruleIds = result.Diagnostics.Select(d => d.RuleId).ToList();
		ruleIds.Should().Contain(LinkManifestConstraints.LinkManifestPagePathsAreMarkdown.Name);
		ruleIds.Should().Contain(LinkManifestConstraints.UrlPathPrefixNormalized.Name);
		ruleIds.Should().Contain(LinkManifestConstraints.PagePathSingleMdExtension.Name);
		result.Diagnostics.Select(d => (d.RuleId, d.Message)).Should().OnlyHaveUniqueItems("each violation is reported exactly once");
		result.Diagnostics.Should().OnlyContain(d => d.File == "elastic/repo/main/links.json");
	}

	private const string PagesWithoutAnchors =
		"""
		{
		  "origin": { "branch": "main", "remote": "https://example.test/repo.git", "ref": "abc", "name": "repo" },
		  "url_path_prefix": "/docs",
		  "links": { "missing.md": {}, "null.md": { "anchors": null }, "empty.md": { "anchors": [] }, "some.md": { "anchors": ["a"] } },
		  "cross_links": []
		}
		""";

	[Test]
	public void LoadManifest_AMissingOrNullAnchorsListReadsAsEmpty()
	{
		var result = LinkLoader.LoadManifest(PagesWithoutAnchors, "links.json");

		result.Diagnostics.Should().BeEmpty();
		var links = result.Value!.Links;
		links["missing.md"].Anchors.Should().BeEmpty();
		links["null.md"].Anchors.Should().BeEmpty();
		links["empty.md"].Anchors.Should().BeEmpty();
		links["some.md"].Anchors.Should().Equal("a");
		links["null.md"].Should().Be(links["missing.md"]);
	}

	[Test]
	public void Serialize_AWrittenPageWithoutAnchorsHasNoAnchorsKey()
	{
		var manifest = LinkLoader.LoadManifest(PagesWithoutAnchors, "links.json").Value!;

		var json = System.Text.Json.Nodes.JsonNode.Parse(LinkJson.Serialize(manifest))!["links"]!.AsObject();

		json["missing.md"]!.AsObject().ContainsKey("anchors").Should().BeFalse();
		json["null.md"]!.AsObject().ContainsKey("anchors").Should().BeFalse();
		json["empty.md"]!.AsObject().ContainsKey("anchors").Should().BeFalse();
		json["some.md"]!["anchors"]!.AsArray().Select(n => (string?)n).Should().Equal("a");
	}

	[Test]
	public void LoadManifest_AValidManifestBreaksNothing() =>
		LinkLoader.LoadManifest(ValidManifest, "links.json").Diagnostics.Should().BeEmpty();

	[Test]
	public void LoadManifest_ReadsAStreamLikeText()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(BrokenManifest));

		var fromStream = LinkLoader.LoadManifest(stream, "links.json");
		var fromText = LinkLoader.LoadManifest(BrokenManifest, "links.json");

		fromStream.Value.Should().Be(fromText.Value);
		fromStream.Diagnostics.Select(d => d.RuleId).Should().BeEquivalentTo(fromText.Diagnostics.Select(d => d.RuleId));
	}

	[Test]
	[Arguments("{ not json")]
	[Arguments("null")]
	[Arguments("")]
	public void LoadManifest_TextThatDoesNotParseIsAnErrorDiagnostic_NeverAnException(string text)
	{
		var result = LinkLoader.LoadManifest(text, "links.json");

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.File == "links.json");
	}

	[Test]
	public void LoadRegistry_ReportsTheViolation_AndKeepsTheETag()
	{
		var result = LinkLoader.LoadRegistry(RegistryWithoutMain, "link-index.json", "\"etag-1\"");

		result.Value!.ETag.Should().Be("\"etag-1\"");
		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Count(d => d.RuleId == LinkRegistryConstraints.RepositoryHasMainOrMaster.Name).Should().Be(1);
	}

	[Test]
	public void LoadRegistry_ReadsAStreamLikeText()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(RegistryWithoutMain));

		var result = LinkLoader.LoadRegistry(stream, "link-index.json", "e");

		result.Value!.ETag.Should().Be("e");
		result.Diagnostics.Should().Contain(d => d.RuleId == LinkRegistryConstraints.RepositoryHasMainOrMaster.Name);
	}

	[Test]
	public void LoadRegistry_TextThatDoesNotParseIsAnErrorDiagnostic()
	{
		var result = LinkLoader.LoadRegistry("[]", "link-index.json");

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error);
	}

	[Test]
	public void SerializeThenLoad_RoundTrips()
	{
		var manifest = LinkLoader.LoadManifest(ValidManifest, "links.json").Value!;

		LinkLoader.LoadManifest(LinkLoader.Serialize(manifest), "links.json").Value.Should().Be(manifest);
	}

	[Test]
	public void Unwrap_LogsTheFindingsAtTheirMappedLevel_AndThrowsOnlyWhenThereIsNoValue()
	{
		var logger = new CapturingLogger();

		var manifest = LinkLoader.LoadManifest(BrokenManifest, "links.json").Unwrap(logger, "links.json of 'repo'");

		manifest.Should().NotBeNull();
		// an error rule of another repository is a warning here, a warning rule is information: neither stops the build
		logger
			.Entries
			.Should()
			.Contain(e => e.Level == LogLevel.Warning && e.Message.Contains(LinkManifestConstraints.LinkManifestPagePathsAreMarkdown.Name));
		logger
			.Entries
			.Should()
			.Contain(e => e.Level == LogLevel.Information && e.Message.Contains(LinkManifestConstraints.UrlPathPrefixNormalized.Name));
		logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Error);

		var unparsable = () => LinkLoader.LoadManifest("{", "links.json").Unwrap(logger, "links.json of 'repo'");
		unparsable.Should().Throw<InvalidOperationException>();
	}

	/// <summary>
	/// The raw wire-format readers are not public: outside the domain's friend assembly (Configuration) the only way from text to a link record is
	/// the loader. The writers (<c>Serialize</c>) stay public.
	/// </summary>
	[Test]
	public void LinkJsonRawReadersAreNotPublic()
	{
		const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var readers = typeof(LinkJson).GetMethods(all).Where(m => m.Name.StartsWith("Deserialize", StringComparison.Ordinal)).ToList();

		readers.Should().NotBeEmpty("the readers exist, behind the loader");
		readers.Should().OnlyContain(m => !m.IsPublic, "a public LinkJson.Deserialize* would hand out an unchecked record");
		typeof(LinkJson)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Select(m => m.Name)
			.Should()
			.Contain("Serialize");
	}

	/// <summary>No public static method of the domain assembly deserializes into a domain record: raw deserializers are internal everywhere.</summary>
	[Test]
	public void NoPublicDeserializerOfTheDomainReturnsARecord()
	{
		var domain = typeof(LinkJson).Assembly;
		var offenders = domain
			.GetExportedTypes()
			.SelectMany(
				t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
				(t, m) => (Type: t, Method: m)
			)
			.Where(x => x.Method.Name.StartsWith("Deserialize", StringComparison.Ordinal))
			.Select(x => $"{x.Type.Name}.{x.Method.Name}")
			.ToList();

		offenders.Should().BeEmpty();
	}

	/// <summary><see cref="LinkLoader"/>'s public surface hands out a record only inside a <see cref="Result{T}"/>.</summary>
	[Test]
	public void LinkLoaderReturnsCheckedResults()
	{
		var domain = typeof(Result<>).Assembly;
		var offenders = typeof(LinkLoader)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(
				m => m.ReturnType.Assembly == domain &&
					!(m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Result<>))
			)
			.Select(m => $"{m.Name} returns {m.ReturnType.Name}")
			.ToList();

		offenders.Should().BeEmpty();
	}

	private sealed class CapturingLogger : ILogger
	{
		public List<(LogLevel Level, string Message)> Entries { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		) => Entries.Add((logLevel, formatter(state, exception)));
	}
}
