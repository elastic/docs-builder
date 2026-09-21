// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Elastic.Changelog.AllowlistIdentity;

/// <summary>
/// Identifies exactly which link allowlist a changelog-scrubber Lambda deployment is running with.
/// The allowlist is embedded from <c>config/assembler.yml</c> at the release tag the Lambda was
/// built from, so the deployed identity is fully determined by that tag. The release pipeline
/// attaches this document as a release asset (<see cref="AssetName"/>) only after the Lambda
/// deploy succeeded, which makes "the newest release carrying the asset" the identity of the
/// most recent gated deploy.
/// </summary>
public sealed partial record ScrubberAllowlistIdentity
{
	/// <summary>The name of the release asset this document is published as.</summary>
	public const string AssetName = "changelog-scrubber-allowlist.json";

	/// <summary>The artifact discriminator every identity document must carry.</summary>
	public const string ArtifactKind = "scrubber-allowlist-identity";

	/// <summary>The schema version this reader understands.</summary>
	public const int CurrentSchemaVersion = 1;

	[GeneratedRegex("^sha256:[0-9a-f]{64}$")]
	private static partial Regex Sha256Format();

	[GeneratedRegex("^[0-9a-f]{40}$")]
	private static partial Regex CommitFormat();

	/// <summary>Version of this document's shape; readers reject versions they don't understand.</summary>
	[JsonPropertyName("schema_version")]
	public required int SchemaVersion { get; init; }

	/// <summary>What kind of document this is; always <see cref="ArtifactKind"/>.</summary>
	[JsonPropertyName("artifact")]
	public required string Artifact { get; init; }

	/// <summary>Hash of the embedded <c>config/assembler.yml</c> bytes, as <c>sha256:</c> + 64 lower-case hex characters.</summary>
	[JsonPropertyName("allowlist_sha256")]
	public required string AllowlistSha256 { get; init; }

	/// <summary>The docs-builder commit the deployed scrubber was built from (full 40-character SHA).</summary>
	[JsonPropertyName("deployment_commit")]
	public required string DeploymentCommit { get; init; }

	/// <summary>The git ref (release tag) the scrubber build checked out.</summary>
	[JsonPropertyName("git_ref")]
	public string? GitRef { get; init; }

	/// <summary>When the scrubber binary embedding this allowlist was built, in UTC.</summary>
	[JsonPropertyName("built_at")]
	public DateTimeOffset? BuiltAt { get; init; }

	/// <summary>Adds a plain-English description of every problem in this identity to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (SchemaVersion != CurrentSchemaVersion)
			problems.Add(
				$"Unsupported allowlist identity schema version {SchemaVersion}; this reader understands version {CurrentSchemaVersion}."
			);
		if (!string.Equals(Artifact, ArtifactKind, StringComparison.Ordinal))
			problems.Add($"Expected artifact '{ArtifactKind}' but found '{Artifact}'.");
		if (string.IsNullOrWhiteSpace(AllowlistSha256) || !Sha256Format().IsMatch(AllowlistSha256))
			problems.Add($"The allowlist hash must look like sha256: plus 64 lower-case hex characters, but found '{AllowlistSha256}'.");
		if (string.IsNullOrWhiteSpace(DeploymentCommit) || !CommitFormat().IsMatch(DeploymentCommit))
			problems.Add($"The deployment commit must be a full 40-character lower-case hex SHA, but found '{DeploymentCommit}'.");
	}

	/// <summary>
	/// Parses an identity document from JSON. Returns false with the reasons in
	/// <paramref name="problems"/> when the document is malformed or fails validation.
	/// </summary>
	public static bool TryParse(
		string json,
		[NotNullWhen(true)] out ScrubberAllowlistIdentity? identity,
		out IReadOnlyList<string> problems
	)
	{
		var found = new List<string>();
		identity = null;
		try
		{
			identity = JsonSerializer.Deserialize(json, ScrubberAllowlistIdentityJsonContext.Default.ScrubberAllowlistIdentity);
		}
		catch (JsonException e)
		{
			found.Add($"The identity document is not valid JSON: {e.Message}");
		}

		if (identity is null && found.Count == 0)
			found.Add("The identity document deserialized to null.");

		identity?.Validate(found);
		if (found.Count > 0)
			identity = null;

		problems = found;
		return identity is not null;
	}

	/// <summary>
	/// Computes the identity hash of an allowlist source (the raw bytes of <c>config/assembler.yml</c>),
	/// as <c>sha256:</c> + 64 lower-case hex characters — the same value <c>sha256sum</c> reports in CI.
	/// </summary>
	public static string ComputeSha256(Stream content)
	{
		var hash = SHA256.HashData(content);
		return $"sha256:{Convert.ToHexStringLower(hash)}";
	}
}

[JsonSerializable(typeof(ScrubberAllowlistIdentity))]
internal sealed partial class ScrubberAllowlistIdentityJsonContext : JsonSerializerContext;
