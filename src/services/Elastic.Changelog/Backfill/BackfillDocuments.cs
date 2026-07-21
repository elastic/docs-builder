// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// The one place to read, write, and hash backfill documents. Writing wraps the document
/// in its envelope (artifact name + schema version) and validates it first; reading checks
/// the envelope <em>before</em> parsing the payload and fails with a clear
/// <see cref="BackfillDocumentException"/> on anything unexpected — a wrong document kind,
/// an unsupported schema version, invalid JSON, or a missing/invalid field. There is no
/// silent best-effort mode: a document either reads cleanly or not at all.
/// </summary>
public static class BackfillDocuments
{
	/// <summary>
	/// Safety limit on document size (in characters). Backfill documents are review-sized
	/// artifacts; anything this large is a bug or garbage input, and refusing it early
	/// keeps a corrupt file from exhausting memory.
	/// </summary>
	public const int MaxDocumentCharacters = 64 * 1024 * 1024;

	/// <summary>
	/// Validates <paramref name="document"/>, wraps it in its envelope, and returns
	/// indented JSON ready to persist. The indentation is for human review only — it has
	/// no effect on the document's hash, which is computed over the canonical form.
	/// </summary>
	public static string Serialize<T>(T document) where T : class, IBackfillDocument
	{
		ArgumentNullException.ThrowIfNull(document);
		ThrowIfInvalid(document);

		var envelope = new BackfillEnvelope<T>
		{
			Artifact = BackfillArtifactKinds.Name(T.Kind),
			SchemaVersion = BackfillSchemaVersions.Current(T.Kind),
			Payload = document
		};
		return JsonSerializer.Serialize(envelope, EnvelopeTypeInfo<T>());
	}

	/// <summary>
	/// Reads a document of type <typeparamref name="T"/> from <paramref name="json"/>.
	/// Throws <see cref="BackfillDocumentException"/> — never returns a half-parsed
	/// document — when the text is not valid JSON, contains a different kind of document,
	/// was written with a schema version this code does not support, or fails validation.
	/// </summary>
	public static T Deserialize<T>(string json) where T : class, IBackfillDocument
	{
		ArgumentNullException.ThrowIfNull(json);
		ThrowIfTooLarge(json);

		var expectedName = BackfillArtifactKinds.Name(T.Kind);
		CheckEnvelopeHeader(json, expectedName, BackfillSchemaVersions.Current(T.Kind));

		BackfillEnvelope<T>? envelope;
		try
		{
			envelope = JsonSerializer.Deserialize(json, EnvelopeTypeInfo<T>());
		}
		catch (JsonException e)
		{
			throw new BackfillDocumentException($"The '{expectedName}' document could not be parsed: {e.Message}", e);
		}

		if (envelope?.Payload is not { } document)
			throw new BackfillDocumentException($"The '{expectedName}' document has no payload.");

		ThrowIfInvalid(document);
		return document;
	}

	/// <summary>
	/// The document's stable identity: SHA-256 over the canonical form of its envelope
	/// (see <see cref="CanonicalJson"/>), as <c>sha256:</c> + 64 hex characters. The same
	/// content always produces the same hash, regardless of formatting or the order
	/// fields were assembled in — this is what makes plans content-addressed.
	/// </summary>
	public static string ComputeHash<T>(T document) where T : class, IBackfillDocument =>
		BackfillHash.Compute(CanonicalJson.Canonicalize(Serialize(document)));

	/// <summary>
	/// Hashes an already-serialized document exactly as <see cref="ComputeHash{T}(T)"/>
	/// would. Useful for hashing a file as it sits on disk without knowing its type.
	/// </summary>
	public static string ComputeHash(string json)
	{
		ArgumentNullException.ThrowIfNull(json);
		ThrowIfTooLarge(json);
		return BackfillHash.Compute(CanonicalJson.Canonicalize(json));
	}

	private static void ThrowIfTooLarge(string json)
	{
		if (json.Length > MaxDocumentCharacters)
			throw new BackfillDocumentException(
				$"The document is {json.Length} characters long, over the {MaxDocumentCharacters} character safety limit for backfill documents.");
	}

	private static void ThrowIfInvalid<T>(T document) where T : class, IBackfillDocument
	{
		var problems = new List<string>();
		document.Validate(problems);
		if (problems.Count == 0)
			return;

		var name = BackfillArtifactKinds.Name(T.Kind);
		throw new BackfillDocumentException(
			$"The '{name}' document is invalid:\n - {string.Join("\n - ", problems)}");
	}

	/// <summary>
	/// Checks the two header fields before any payload parsing, so the errors for "wrong
	/// file" and "wrong version" are specific instead of a generic parse failure.
	/// </summary>
	private static void CheckEnvelopeHeader(string json, string expectedName, int expectedVersion)
	{
		JsonDocument parsed;
		try
		{
			parsed = JsonDocument.Parse(json);
		}
		catch (JsonException e)
		{
			throw new BackfillDocumentException($"The document is not valid JSON: {e.Message}", e);
		}

		using (parsed)
		{
			var root = parsed.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
				throw new BackfillDocumentException(
					"This is not a backfill document: the top level must be a JSON object with 'artifact' and 'schema_version' fields.");

			CheckArtifactField(root, expectedName);
			CheckSchemaVersionField(root, expectedName, expectedVersion);
		}
	}

	private static void CheckArtifactField(JsonElement root, string expectedName)
	{
		if (!root.TryGetProperty("artifact", out var artifact) || artifact.ValueKind != JsonValueKind.String)
			throw new BackfillDocumentException(
				"This is not a backfill document: the 'artifact' field naming the document kind is missing.");

		var name = artifact.GetString() ?? "";
		if (!BackfillArtifactKinds.TryParse(name, out _))
			throw new BackfillDocumentException(
				$"Unknown document kind '{name}'. Expected one of: inventory, overrides, semantic-model, plan, provenance, ledger.");

		if (!string.Equals(name, expectedName, StringComparison.Ordinal))
			throw new BackfillDocumentException(
				$"This file contains a '{name}' document, but a '{expectedName}' document was requested. Check that the right file is being read.");
	}

	private static void CheckSchemaVersionField(JsonElement root, string expectedName, int expectedVersion)
	{
		if (!root.TryGetProperty("schema_version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var value))
			throw new BackfillDocumentException(
				$"This '{expectedName}' document is missing the 'schema_version' field, so it cannot be read safely.");

		if (value != expectedVersion)
			throw new BackfillDocumentException(
				$"This '{expectedName}' document was written with schema version {value}, but this code only understands version {expectedVersion}. " +
				"Regenerate the document with matching tooling, or upgrade to a version of this code that understands it.");
	}

	private static JsonTypeInfo<BackfillEnvelope<T>> EnvelopeTypeInfo<T>() where T : IBackfillDocument =>
		BackfillJsonContext.Default.GetTypeInfo(typeof(BackfillEnvelope<T>)) as JsonTypeInfo<BackfillEnvelope<T>>
		?? throw new InvalidOperationException(
			$"BackfillEnvelope<{typeof(T).Name}> is not registered on BackfillJsonContext; add a [JsonSerializable] attribute for it.");
}
