// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// The explicit reconcile request `changelog registry reconcile` sends to the scrubber queue
/// (elastic/docs-eng-team#688 Phase 2). A versioned, discriminated envelope: the scrubber treats
/// any body whose <c>kind</c> is <c>reconcile</c> as one of these and performs a <em>full group
/// heal</em> — object-level reconcile over the union of both buckets' group listings, then the
/// group reconcile — which is what makes a lost or DLQ-expired scrub event recoverable by message.
/// </summary>
public sealed record ReconcileQueueMessage
{
	/// <summary>The discriminator value marking a body as a reconcile request.</summary>
	public const string ReconcileKind = "reconcile";

	/// <summary>The envelope version this producer writes and the consumer accepts.</summary>
	public const int CurrentVersion = 1;

	/// <summary>Body discriminator; always <see cref="ReconcileKind"/> for this type.</summary>
	public string? Kind { get; init; }

	/// <summary>Envelope version; the consumer rejects anything but <see cref="CurrentVersion"/>.</summary>
	public int Version { get; init; }

	/// <summary>The scope family: <c>bundle</c> or <c>changelog</c>.</summary>
	public string? Scope { get; init; }

	/// <summary>The group inside the scope: a product, or <c>{org}/{repo}/{branch}</c>.</summary>
	public string? Group { get; init; }

	/// <summary>Caller-chosen id tying this message to a cutover ledger entry.</summary>
	public string? CorrelationId { get; init; }

	/// <summary>True when <paramref name="body"/> parses as JSON whose <c>kind</c> is <see cref="ReconcileKind"/> — regardless of whether the rest validates.</summary>
	public static bool TryRead(string body, [NotNullWhen(true)] out ReconcileQueueMessage? message)
	{
		message = null;
		if (!body.Contains("\"kind\"", StringComparison.OrdinalIgnoreCase))
			return false;
		try
		{
			var parsed = JsonSerializer.Deserialize(body, ReconcileQueueMessageJsonContext.Default.ReconcileQueueMessage);
			if (parsed is null || !string.Equals(parsed.Kind, ReconcileKind, StringComparison.Ordinal))
				return false;
			message = parsed;
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	/// <summary>
	/// Validates the envelope strictly — version, scope family, and every group segment (via
	/// <see cref="ChangelogScope"/>, hence <c>ChangelogKeys</c>) — before anything derives an S3
	/// listing from it. False with a reason when the message must be rejected.
	/// </summary>
	public bool TryResolveScope([NotNullWhen(true)] out ChangelogScope? scope, [NotNullWhen(false)] out string? error)
	{
		scope = null;
		if (Version != CurrentVersion)
		{
			error = $"unsupported reconcile message version {Version} (supported: {CurrentVersion})";
			return false;
		}

		switch (Scope)
		{
			case "bundle":
				if (!ChangelogScope.TryCreateBundle(Group, out scope))
				{
					error = $"invalid bundle group \"{Group}\"";
					return false;
				}
				break;
			case "changelog":
				var segments = Group?.Split('/') ?? [];
				if (segments.Length < 3
					|| !ChangelogScope.TryCreateChangelog(segments[0], segments[1], string.Join('/', segments[2..]), out scope))
				{
					error = $"invalid changelog group \"{Group}\" (expected {{org}}/{{repo}}/{{branch}})";
					return false;
				}
				break;
			default:
				error = $"unknown reconcile scope \"{Scope}\"";
				return false;
		}

		error = null;
		return true;
	}

	/// <summary>Builds the message for one scope, stamped with the caller's correlation id.</summary>
	public static ReconcileQueueMessage For(ChangelogScope scope, string correlationId) => new()
	{
		Kind = ReconcileKind,
		Version = CurrentVersion,
		Scope = scope.Kind == ChangelogScopeKind.Bundle ? "bundle" : "changelog",
		Group = scope.Group,
		CorrelationId = correlationId
	};

	/// <summary>Serializes with the same source-generated context the consumer parses with.</summary>
	public string ToJson() =>
		JsonSerializer.Serialize(this, ReconcileQueueMessageJsonContext.Default.ReconcileQueueMessage);
}

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(ReconcileQueueMessage))]
public sealed partial class ReconcileQueueMessageJsonContext : JsonSerializerContext;
