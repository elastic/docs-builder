// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration.Assembler;

namespace Elastic.Documentation.Build.Tests;

public class AssemblerBuildStampTests
{
	// ── helper factories ───────────────────────────────────────────────────────

	private static readonly IReadOnlyDictionary<string, string> BaseCheckouts = new Dictionary<string, string>
	{
		{ "docs-content", "abc1234" },
		{ "elastic-docs", "def5678" }
	};

	private static readonly IReadOnlyDictionary<string, string> BaseAssemblies = new Dictionary<string, string>
	{
		{ "Elastic.Markdown", "11111111-1111-1111-1111-111111111111" }
	};

	private static readonly IReadOnlyCollection<string> BaseExporters = ["Html", "LinkMetadata"];

	private static AssemblerBuildStamp MakeInMemory(
		int schemaVersion = 1,
		string environment = "dev",
		IReadOnlyDictionary<string, string>? checkouts = null,
		string configHash = "deadbeef",
		IReadOnlyDictionary<string, string>? assemblies = null,
		IReadOnlyCollection<string>? exporters = null
	) =>
		new()
		{
			SchemaVersion = schemaVersion,
			Environment = environment,
			Checkouts = checkouts ?? BaseCheckouts,
			ConfigurationHash = configHash,
			Assemblies = assemblies ?? BaseAssemblies,
			Exporters = exporters ?? BaseExporters
		};

	// ── IsUpToDate ─────────────────────────────────────────────────────────────

	[Fact]
	public void IsUpToDate_NullExisting_ReturnsMiss()
	{
		var current = MakeInMemory();
		var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(null, current);
		isUpToDate.Should().BeFalse();
		reason.Should().Contain("no stamp");
	}

	[Fact]
	public void IsUpToDate_NullCurrent_ReturnsMiss()
	{
		var existing = MakeInMemory().ToRecord();
		var (isUpToDate, _) = AssemblerBuildStampService.IsUpToDate(existing, null);
		isUpToDate.Should().BeFalse();
	}

	[Fact]
	public void IsUpToDate_IdenticalStamps_ReturnsHit()
	{
		var stamp = MakeInMemory();
		var (isUpToDate, _) = AssemblerBuildStampService.IsUpToDate(stamp.ToRecord(), stamp);
		isUpToDate.Should().BeTrue();
	}

	[Fact]
	public void IsUpToDate_SchemaVersionBump_ReturnsMiss()
	{
		var existing = MakeInMemory(schemaVersion: 1).ToRecord();
		var current = MakeInMemory(schemaVersion: 2);
		var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(existing, current);
		isUpToDate.Should().BeFalse();
		reason.Should().Contain("schema");
	}

	[Fact]
	public void IsUpToDate_EnvironmentChanged_ReturnsMiss()
	{
		var existing = MakeInMemory(environment: "dev").ToRecord();
		var current = MakeInMemory(environment: "staging");
		var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(existing, current);
		isUpToDate.Should().BeFalse();
		reason.Should().Contain("environment");
	}

	[Fact]
	public void IsUpToDate_CheckoutShaChanged_ReturnsMiss()
	{
		var existing = MakeInMemory(checkouts: new Dictionary<string, string> { { "docs-content", "aaa" } }).ToRecord();
		var current = MakeInMemory(checkouts: new Dictionary<string, string> { { "docs-content", "bbb" } });
		var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(existing, current);
		isUpToDate.Should().BeFalse();
		reason.Should().Contain("checkout");
	}

	[Fact]
	public void IsUpToDate_ConfigHashChanged_ReturnsMiss()
	{
		var existing = MakeInMemory(configHash: "aaaa").ToRecord();
		var current = MakeInMemory(configHash: "bbbb");
		var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(existing, current);
		isUpToDate.Should().BeFalse();
		reason.Should().Contain("configuration");
	}

	[Fact]
	public void IsUpToDate_AssemblyMvidChanged_ReturnsMiss()
	{
		var existing = MakeInMemory(assemblies: new Dictionary<string, string>
		{
			{ "Elastic.Markdown", "11111111-1111-1111-1111-111111111111" }
		}).ToRecord();
		var current = MakeInMemory(assemblies: new Dictionary<string, string>
		{
			{ "Elastic.Markdown", "22222222-2222-2222-2222-222222222222" }
		});
		var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(existing, current);
		isUpToDate.Should().BeFalse();
		reason.Should().Contain("MVID");
	}

	[Fact]
	public void IsUpToDate_ExportersChanged_ReturnsMiss()
	{
		var existing = MakeInMemory(exporters: ["Html"]).ToRecord();
		var current = MakeInMemory(exporters: ["Html", "Elasticsearch"]);
		var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(existing, current);
		isUpToDate.Should().BeFalse();
		reason.Should().Contain("exporter");
	}

	// ── Compute ────────────────────────────────────────────────────────────────

	[Fact]
	public void Compute_EmptyHeadReference_ReturnsNull()
	{
		var fileSystem = new MockFileSystem();
		var configContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var checkouts = new[]
		{
			new Checkout
			{
				Repository = new Repository { Name = "docs-content" },
				HeadReference = "", // empty → unresolved

				Directory = fileSystem.DirectoryInfo.New("/checkouts/docs-content")
			}
		};
		var result = AssemblerBuildStampService.Compute(
			"dev",
			checkouts,
			configContext.ConfigurationFileProvider,
			new HashSet<Exporter> { Exporter.Html }
		);
		result.Should().BeNull();
	}

	[Fact]
	public void Compute_ValidCheckouts_ReturnsStamp()
	{
		var fileSystem = new MockFileSystem();
		var configContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var checkouts = new[]
		{
			new Checkout
			{
				Repository = new Repository { Name = "docs-content" },
				HeadReference = "abc1234",
				Directory = fileSystem.DirectoryInfo.New("/checkouts/docs-content")
			}
		};
		var stamp = AssemblerBuildStampService.Compute(
			"dev",
			checkouts,
			configContext.ConfigurationFileProvider,
			new HashSet<Exporter> { Exporter.Html }
		);
		stamp.Should().NotBeNull();
		stamp.Environment.Should().Be("dev");
		stamp.Checkouts["docs-content"].Should().Be("abc1234");
		stamp.SchemaVersion.Should().BeGreaterThan(0);
	}

	// ── ToRecord — no sensitive values on disk ─────────────────────────────────

	[Fact]
	public void ToRecord_DoesNotExposeCheckoutShas()
	{
		var stamp = MakeInMemory(checkouts: new Dictionary<string, string> { { "private-repo", "supersecretsha" } });
		var record = stamp.ToRecord();
		// The record must not contain the raw SHA anywhere
		record.CheckoutsHash.Should().NotContain("supersecretsha");
		record.CheckoutsHash.Should().HaveLength(64); // hex SHA-256
	}

	[Fact]
	public void ToRecord_DoesNotExposeAssemblyMvids()
	{
		var stamp = MakeInMemory(assemblies: new Dictionary<string, string>
		{
			{ "Elastic.Markdown", "deadbeef-dead-beef-dead-beefdeadbeef" }
		});
		var record = stamp.ToRecord();
		record.AssembliesHash.Should().NotContain("deadbeef-dead-beef-dead-beefdeadbeef");
		record.AssembliesHash.Should().HaveLength(64);
	}

	[Fact]
	public void ToRecord_DifferentInputs_ProduceDifferentHashes()
	{
		var stampA = MakeInMemory(checkouts: new Dictionary<string, string> { { "repo", "sha-a" } });
		var stampB = MakeInMemory(checkouts: new Dictionary<string, string> { { "repo", "sha-b" } });
		stampA.ToRecord().CheckoutsHash.Should().NotBe(stampB.ToRecord().CheckoutsHash);
	}

	// ── JSON round-trip ────────────────────────────────────────────────────────

	[Fact]
	public async Task ReadWrite_RoundTrip_PreservesFields()
	{
		var stamp = MakeInMemory(environment: "staging");
		using var dir = new ScopedTempDirectory(new System.IO.Abstractions.FileSystem(), "stamp-test");
		var path = System.IO.Path.Join(dir.FullName, AssemblerBuildStampService.StampFileName);

		var ct = TestContext.Current.CancellationToken;
		await AssemblerBuildStampService.WriteAsync(path, stamp, ct);
		var read = await AssemblerBuildStampService.ReadAsync(path, ct);

		read.Should().NotBeNull();
		read.Environment.Should().Be("staging");
		read.CheckoutsHash.Should().Be(stamp.ToRecord().CheckoutsHash);
		read.AssembliesHash.Should().Be(stamp.ToRecord().AssembliesHash);
	}

	[Fact]
	public async Task Read_MissingFile_ReturnsNull()
	{
		var result = await AssemblerBuildStampService.ReadAsync("/nonexistent/path/.stamp.json", TestContext.Current.CancellationToken);
		result.Should().BeNull();
	}

	[Fact]
	public async Task Read_CorruptFile_ReturnsNull()
	{
		using var dir = new ScopedTempDirectory(new System.IO.Abstractions.FileSystem(), "stamp-corrupt");
		var path = System.IO.Path.Join(dir.FullName, AssemblerBuildStampService.StampFileName);
		var ct = TestContext.Current.CancellationToken;
		await System.IO.File.WriteAllTextAsync(path, "not valid json", ct);

		var result = await AssemblerBuildStampService.ReadAsync(path, ct);
		result.Should().BeNull();
	}
}
