// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using AwesomeAssertions;
using Elastic.Changelog.AllowlistIdentity;

namespace Elastic.Changelog.Tests.AllowlistIdentity;

public class ScrubberAllowlistIdentityTests
{
	private const string ValidSha = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
	private const string ValidCommit = "0123456789abcdef0123456789abcdef01234567";

	private static string ValidJson(
		int schemaVersion = ScrubberAllowlistIdentity.CurrentSchemaVersion,
		string artifact = ScrubberAllowlistIdentity.ArtifactKind,
		string sha = ValidSha,
		string commit = ValidCommit
	) =>
		$$"""
		{
			"schema_version": {{schemaVersion}},
			"artifact": "{{artifact}}",
			"allowlist_sha256": "{{sha}}",
			"deployment_commit": "{{commit}}",
			"git_ref": "v1.2.3",
			"built_at": "2026-08-01T12:00:00Z"
		}
		""";

	[Fact]
	public void TryParse_ValidDocument_ReturnsIdentity()
	{
		var result = ScrubberAllowlistIdentity.TryParse(ValidJson(), out var identity, out var problems);

		result.Should().BeTrue();
		problems.Should().BeEmpty();
		identity!.AllowlistSha256.Should().Be(ValidSha);
		identity.DeploymentCommit.Should().Be(ValidCommit);
		identity.GitRef.Should().Be("v1.2.3");
		identity.BuiltAt.Should().Be(DateTimeOffset.Parse("2026-08-01T12:00:00Z", CultureInfo.InvariantCulture));
	}

	[Fact]
	public void TryParse_UnsupportedSchemaVersion_Fails()
	{
		var result = ScrubberAllowlistIdentity.TryParse(ValidJson(schemaVersion: 2), out var identity, out var problems);

		result.Should().BeFalse();
		identity.Should().BeNull();
		problems.Should().ContainSingle(p => p.Contains("schema version 2"));
	}

	[Fact]
	public void TryParse_WrongArtifactKind_Fails()
	{
		var result = ScrubberAllowlistIdentity.TryParse(ValidJson(artifact: "something-else"), out _, out var problems);

		result.Should().BeFalse();
		problems.Should().ContainSingle(p => p.Contains("something-else"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[InlineData("sha256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
	[InlineData("sha256:abc")]
	public void TryParse_MalformedSha256_Fails(string sha)
	{
		var result = ScrubberAllowlistIdentity.TryParse(ValidJson(sha: sha), out _, out var problems);

		result.Should().BeFalse();
		problems.Should().Contain(p => p.Contains("sha256:"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("abc123")]
	[InlineData("0123456789ABCDEF0123456789ABCDEF01234567")]
	public void TryParse_MalformedCommit_Fails(string commit)
	{
		var result = ScrubberAllowlistIdentity.TryParse(ValidJson(commit: commit), out _, out var problems);

		result.Should().BeFalse();
		problems.Should().Contain(p => p.Contains("40-character"));
	}

	[Fact]
	public void TryParse_InvalidJson_FailsWithoutThrowing()
	{
		var result = ScrubberAllowlistIdentity.TryParse("not json at all {", out var identity, out var problems);

		result.Should().BeFalse();
		identity.Should().BeNull();
		problems.Should().ContainSingle(p => p.Contains("not valid JSON"));
	}

	[Fact]
	public void ComputeSha256_KnownContent_MatchesSha256Sum()
	{
		// printf 'hello\n' | sha256sum
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello\n"));

		var hash = ScrubberAllowlistIdentity.ComputeSha256(stream);

		hash.Should().Be("sha256:5891b5b522d5df086d0ff0b110fbd9d21bb4fc7163af34d08286a2e846f6be03");
	}
}
