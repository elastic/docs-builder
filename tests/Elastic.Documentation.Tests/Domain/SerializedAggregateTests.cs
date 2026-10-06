// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Links;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Serialization;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The aggregates that are read as JSON have <c>internal init</c> setters (the compile-time guard). System.Text.Json skips a property it
/// cannot set, silently, so each serialized type is deserialized from a sample here and every field is checked: a setter the serializer
/// cannot reach would otherwise be data loss nobody sees.
/// </summary>
public class SerializedAggregateTests
{
	[Test]
	public void LinkManifest_DeserializesEveryField()
	{
		const string json =
			"""
			{
			  "origin": { "branch": "main", "remote": "elastic/docs-builder", "ref": "abc123", "name": "docs-builder", "github_ref": "refs/pull/1/merge" },
			  "url_path_prefix": "/docs/x",
			  "links": {
			    "index.md": { "anchors": ["a", "b"], "hidden": true },
			    "other.md": { "anchors": [] }
			  },
			  "cross_links": ["kibana", "es"],
			  "redirects": {
			    "old.md": { "to": "new.md", "anchors": { "x": "y" } },
			    "many.md": { "many": [ { "to": "one.md" }, { "to": "two.md", "anchors": { "!": "!" } } ] }
			  }
			}
			""";

		var manifest = LinkJson.DeserializeManifest(json)!;

		manifest.Origin.Branch.Should().Be("main");
		manifest.Origin.Remote.Should().Be("elastic/docs-builder");
		manifest.Origin.Ref.Should().Be("abc123");
		manifest.Origin.RepositoryName.Should().Be("docs-builder");
		manifest.Origin.GitHubRef.Should().Be("refs/pull/1/merge");
		manifest.UrlPathPrefix.Should().Be("/docs/x");
		manifest.Links.Keys.Should().BeEquivalentTo("index.md", "other.md");
		manifest.Links["index.md"].Anchors.Should().Equal("a", "b");
		manifest.Links["index.md"].Hidden.Should().BeTrue();
		manifest.Links["other.md"].Anchors.Should().BeEmpty();
		manifest.Links["other.md"].Hidden.Should().BeFalse();
		manifest.CrossLinks.Should().Equal("kibana", "es");
		manifest.Redirects.Should().NotBeNull();
		manifest.Redirects!["old.md"].To.Should().Be("new.md");
		manifest.Redirects["old.md"].Anchors.Should().ContainKey("x").WhoseValue.Should().Be("y");
		manifest.Redirects["many.md"].Many.Should().HaveCount(2);
		manifest.Redirects["many.md"].Many![0].To.Should().Be("one.md");
		manifest.Redirects["many.md"].Many![1].Anchors.Should().ContainKey("!");
	}

	[Test]
	public void LinkManifest_MissingRequiredMember_StillFailsToParse()
	{
		// `required` could not stay on an internal-init property; [JsonRequired] keeps the parse failure for a missing key
		const string json = """{ "url_path_prefix": null, "links": {}, "cross_links": [] }""";

		var parse = () => LinkJson.DeserializeManifest(json);

		_ = parse.Should().Throw<JsonException>();
	}

	[Test]
	public void LinkRegistry_DeserializesEveryField()
	{
		const string json =
			"""
			{
			  "repositories": {
			    "kibana": {
			      "main": { "repository": "kibana", "path": "elastic/kibana/main/links.json", "branch": "main", "etag": "e1", "ref": "r1", "updated_at": "2026-01-02T03:04:05Z" },
			      "9.0": { "repository": "kibana", "path": "elastic/kibana/9.0/links.json", "branch": "9.0", "etag": "e2" }
			    }
			  }
			}
			""";

		var registry = LinkJson.DeserializeRegistry(json)!;

		registry.Repositories.Keys.Should().Equal("kibana");
		var main = registry.Repositories["kibana"]["main"];
		main.Repository.Should().Be("kibana");
		main.Path.Should().Be("elastic/kibana/main/links.json");
		main.Branch.Should().Be("main");
		main.ETag.Should().Be("e1");
		main.GitReference.Should().Be("r1");
		main.UpdatedAt.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
		var older = registry.Repositories["kibana"]["9.0"];
		older.ETag.Should().Be("e2");
		older.UpdatedAt.Should().Be(DateTime.MinValue);
	}

	[Test]
	public void LinkRegistryEntry_DeserializesEveryField()
	{
		const string json =
			"""{ "repository": "r", "path": "p", "branch": "b", "etag": "t", "ref": "g", "updated_at": "2026-05-06T07:08:09Z" }""";

		var entry = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkRegistryEntry)!;

		entry.Repository.Should().Be("r");
		entry.Path.Should().Be("p");
		entry.Branch.Should().Be("b");
		entry.ETag.Should().Be("t");
		entry.GitReference.Should().Be("g");
		entry.UpdatedAt.Should().Be(new DateTime(2026, 5, 6, 7, 8, 9, DateTimeKind.Utc));
	}

	[Test]
	public void GitCheckoutInformation_DeserializesEveryField()
	{
		const string json =
			"""{ "branch": "b", "remote": "git@github.com:elastic/x.git", "ref": "r", "name": "x", "github_ref": "refs/heads/b" }""";

		var info = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.GitCheckoutInformation)!;

		info.Branch.Should().Be("b");
		info.Remote.Should().Be("git@github.com:elastic/x.git");
		info.Ref.Should().Be("r");
		info.RepositoryName.Should().Be("x");
		info.GitHubRef.Should().Be("refs/heads/b");
		info.GitHubRepository.Should().Be("elastic/x");
	}

	[Test]
	public void CreateRules_DeserializesEveryFieldThroughItsConverter()
	{
		// the changelog PR-evaluation artifact is serialized by another assembly, which cannot reach the internal setters
		const string json =
			"""
			{ "labels": ["skip", "wip"], "mode": "Include", "match": "All", "by_product": { "kibana": { "labels": ["k"], "mode": "Exclude", "match": "Any", "by_product": null } } }
			""";
		var options = new JsonSerializerOptions { WriteIndented = false };

		var rules = JsonSerializer.Deserialize<CreateRules>(json, options)!;

		rules.Labels.Should().Equal("skip", "wip");
		rules.Mode.Should().Be(FieldMode.Include);
		rules.Match.Should().Be(MatchMode.All);
		rules.ByProduct.Should().ContainKey("kibana").WhoseValue.Labels.Should().Equal("k");
		JsonSerializer.Deserialize<CreateRules>(JsonSerializer.Serialize(rules, options), options).Should().Be(rules);
	}

	[Test]
	public void ApplicableTo_DeserializesEveryShapeThroughItsConverter()
	{
		const string json =
			"""
			[
			  { "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "9.0" },
			  { "type": "deployment", "sub_type": "ece", "lifecycle": "beta", "version": "9.1" },
			  { "type": "deployment", "sub_type": "self", "lifecycle": "ga", "version": "all" },
			  { "type": "serverless", "sub_type": "elasticsearch", "lifecycle": "preview", "version": "all" },
			  { "type": "product", "sub_type": "product", "lifecycle": "deprecated", "version": "2.0" },
			  { "type": "product", "sub_type": "edot-dotnet", "lifecycle": "ga", "version": "1.2" }
			]
			""";

		var applicableTo = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ApplicableTo)!;

		applicableTo.Stack.Should().ContainSingle().Which.Lifecycle.Should().Be(ProductLifecycle.GenerallyAvailable);
		applicableTo.Stack!.Single().Version.Should().NotBeNull();
		applicableTo.Deployment!.Ece.Should().ContainSingle().Which.Lifecycle.Should().Be(ProductLifecycle.Beta);
		applicableTo.Deployment.Self.Should().ContainSingle();
		applicableTo.Deployment.Eck.Should().BeNull();
		applicableTo.Serverless!.Elasticsearch.Should().ContainSingle().Which.Lifecycle.Should().Be(ProductLifecycle.TechnicalPreview);
		applicableTo.Product.Should().ContainSingle().Which.Lifecycle.Should().Be(ProductLifecycle.Deprecated);
		applicableTo.ProductApplicability!
			.EdotDotnet
			.Should()
			.ContainSingle()
			.Which
			.Lifecycle
			.Should()
			.Be(ProductLifecycle.GenerallyAvailable);
	}
}
