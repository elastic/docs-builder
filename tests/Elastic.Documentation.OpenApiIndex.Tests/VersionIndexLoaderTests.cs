// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Text;
using AwesomeAssertions;

namespace Elastic.Documentation.OpenApiIndex.Tests;

/// <summary>
/// <see cref="VersionIndexLoader"/> is the one definition of a checked version index: the read side (the API explorer) and the write side (the publisher)
/// go through the same gate, and a read reports every violation, not the first.
/// </summary>
public class VersionIndexLoaderTests
{
	// 'latest' is neither main nor a major (a warning); '8' resolving to 9.1 is a key that does not match its version (an error)
	private const string BrokenIndex =
		"""
		{
		  "elastic/elasticsearch": {
		    "openapi.json": {
		      "main": { "version": "main" },
		      "latest": { "version": "9.1" },
		      "8": { "version": "9.1" }
		    }
		  }
		}
		""";

	private const string ValidIndex =
		"""
		{ "elastic/elasticsearch": { "openapi.json": { "main": { "version": "main" }, "9": { "version": "9.5" } } } }
		""";

	[Test]
	public void Load_AValidIndexBreaksNothing()
	{
		var load = VersionIndexLoader.Load(ValidIndex);

		load.Index.Should().NotBeNull();
		load.HasErrors.Should().BeFalse();
		load.Report!.Violations.Should().BeEmpty();
	}

	[Test]
	public void Load_ReportsEveryViolation_AndStillReturnsTheIndex()
	{
		var load = VersionIndexLoader.Load(BrokenIndex);

		load.Index.Should().NotBeNull();
		load.HasErrors.Should().BeTrue();
		var ids = load.Report!.Violations.Select(v => v.ConstraintId).ToList();
		ids.Should().Contain(VersionIndexConstraints.VersionMonikerMainOrMajor.Name);
		ids.Should().Contain(VersionIndexConstraints.VersionKeyMatchesVersion.Name);
	}

	[Test]
	public async Task LoadAsync_ReadsAStreamLikeText()
	{
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(BrokenIndex));

		var fromStream = await VersionIndexLoader.LoadAsync(stream, Cancel.None);
		var fromText = VersionIndexLoader.Load(BrokenIndex);

		fromStream.Report!
			.Violations
			.Select(v => v.ConstraintId)
			.Should()
			.BeEquivalentTo(fromText.Report!.Violations.Select(v => v.ConstraintId));
	}

	[Test]
	[Arguments("{ not json")]
	[Arguments("null")]
	public async Task TextThatDoesNotParseIsAnError_NeverAnException(string text)
	{
		var fromText = VersionIndexLoader.Load(text);
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var fromStream = await VersionIndexLoader.LoadAsync(stream, Cancel.None);

		foreach (var load in new[] { fromText, fromStream })
		{
			load.Index.Should().BeNull();
			load.Report.Should().BeNull();
			load.Error.Should().NotBeNullOrEmpty();
			load.HasErrors.Should().BeTrue();
		}
		fromText.Error.Should().Be(fromStream.Error);
	}

	[Test]
	public void TheWriteSideChecksThroughTheSameGate()
	{
		var index = VersionIndexLoader.Load(BrokenIndex).Index!;

		VersionIndexLoader
			.Check(index)
			.Violations
			.Select(v => (v.ConstraintId, v.Message))
			.Should()
			.BeEquivalentTo(VersionIndexConstraints.All.Report(index).Violations.Select(v => (v.ConstraintId, v.Message)));
	}

	[Test]
	public void EveryPublicEntryReturnsTheLoadOrTheReport() =>
		typeof(VersionIndexLoader)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Select(m => m.ReturnType.Name)
			.Should()
			.OnlyContain(
				n => n.StartsWith("VersionIndexLoad", StringComparison.Ordinal) || n.StartsWith(
					"Task",
					StringComparison.Ordinal
				) || n.StartsWith("ConstraintReport", StringComparison.Ordinal)
			);
}
