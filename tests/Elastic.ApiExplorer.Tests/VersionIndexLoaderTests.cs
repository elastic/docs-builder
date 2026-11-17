// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class VersionIndexLoaderTests
{
	private const string File = "index.json";

	[Test]
	public void Load_KeepsEveryLevelInWrittenOrder()
	{
		var result = VersionIndexLoader.Load(
			"""
			{ "z/repo": { "b.json": { "9": { "version": "9.1" }, "main": { "version": "main" } }, "a.json": { "8": { "version": "8.2" } } }, "a/repo": {} }
			""",
			File
		);

		var index = result.ValueOrNull()!;
		index.Repositories.Select(r => r.Name).Should().Equal("z/repo", "a/repo");
		index.Repositories[0].Specs.Select(s => s.File).Should().Equal("b.json", "a.json");
		index.Repositories[0].Specs[0].Monikers.Should().Equal(new VersionMoniker("9", "9.1"), new VersionMoniker("main", "main"));
	}

	[Test]
	public void Load_AnIndexThatBreaksRules_IsStillLoaded_WithTheViolationsAsFindings()
	{
		var result = VersionIndexLoader.Load(
			"""{ "e/r": { "s.json": { "latest": { "version": "9.1" }, "8": { "version": "9.1" } } } }""",
			File
		);

		result.ValueOrNull().Should().NotBeNull();
		result.Findings().Select(f => f.Severity).Should().BeEquivalentTo([Severity.Warning, Severity.Error]);
	}

	[Test]
	[Arguments("{ not json")]
	[Arguments("")]
	public void Load_TextThatDoesNotParse_IsUnreadableWithTheParsersMessage(string text)
	{
		var result = VersionIndexLoader.Load(text, File);

		result.ValueOrNull().Should().BeNull();
		result.Findings().Should().ContainSingle().Which.Message.Should().NotBeNullOrEmpty();
	}

	[Test]
	public void Load_JsonNull_IsUnreadableWithTheEmptyMessage() =>
		VersionIndexLoader.Load("null", File).Findings().Should().ContainSingle().Which.Message.Should().Be(VersionIndexLoader.EmptyMessage);

	[Test]
	public void TryGetSpec_AnEmptySpecIsFoundButHoldsNoMonikers()
	{
		var index = VersionIndexLoader.Load("""{ "e/r": { "s.json": {} } }""", File).ValueOrNull()!;

		index.TryGetRepository("e/r", out var repo).Should().BeTrue();
		repo.TryGetSpec("s.json", out var monikers).Should().BeTrue();
		monikers.Should().BeEmpty();
		repo.TryGetSpec("other.json", out _).Should().BeFalse();
		index.TryGetRepository("x/y", out _).Should().BeFalse();
	}

	[Test]
	public void Load_AJsonNullAtAnyLevel_IsKeptAsWritten_NotReported()
	{
		var result = VersionIndexLoader.Load(
			"""{ "a/b": null, "c/d": { "s.json": null, "t.json": { "main": null, "9": { "version": "9.1" } } } }""",
			File
		);

		result.Findings().Should().BeEmpty();
		var index = result.ValueOrNull()!;
		index.Repositories.Select(r => r.Name).Should().Equal("a/b", "c/d");
		index.Repositories[0].Specs.Should().BeNull();
		index.Repositories[1].Specs[0].Monikers.Should().BeNull();
		index.AllMonikers().Select(m => m.Moniker.Key).Should().Equal("9");
	}
}
