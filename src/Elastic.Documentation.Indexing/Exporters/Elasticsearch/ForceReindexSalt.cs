// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Ingest.Elasticsearch.Indices;

namespace Elastic.Documentation.Indexing.Exporters.Elasticsearch;

/// <summary>
/// A fresh salt per forced run makes every stored document hash mismatch, so the scripted
/// hash upsert replaces unchanged documents too. Without a salt hashes are left untouched.
/// </summary>
internal static class ForceReindexSalt
{
	public static string Create(bool forceReindex) => forceReindex ? Guid.NewGuid().ToString("N") : string.Empty;

	public static string Apply(string hash, string salt) => salt.Length == 0 ? hash : HashedBulkUpdate.CreateHash(hash, salt);
}

/// <summary>Index-wide values that join the per-document fields in the change-detection hash.</summary>
internal sealed record DocumentHashInputs(string SemanticHash, string LexicalHash, string FixedSynonymsHash, string ForceReindexSalt);
