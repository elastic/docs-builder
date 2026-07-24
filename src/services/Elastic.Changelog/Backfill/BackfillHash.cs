// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Computes and checks the hashes that give backfill documents a stable identity.
/// A hash is SHA-256 over the UTF-8 bytes of a document's canonical form (see
/// <see cref="CanonicalJson"/>), written as <c>sha256:</c> followed by 64 lower-case
/// hex characters. Because the canonical form is stable, the same content always
/// produces the same hash — which is what lets a plan be pinned to the exact inputs
/// it was computed from.
/// </summary>
public static class BackfillHash
{
	/// <summary>Every hash value starts with this, so a reader can tell the algorithm at a glance.</summary>
	public const string Prefix = "sha256:";

	private const int HexLength = 64;

	/// <summary>
	/// Hashes <paramref name="canonicalText"/> and returns e.g.
	/// <c>sha256:9f86d08…</c>. The caller is responsible for passing canonical text;
	/// to hash a document, prefer <see cref="BackfillDocuments.ComputeHash{T}(T)"/>,
	/// which canonicalizes first.
	/// </summary>
	public static string Compute(string canonicalText)
	{
		ArgumentNullException.ThrowIfNull(canonicalText);
		var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
		return Prefix + Convert.ToHexStringLower(digest);
	}

	/// <summary>True when <paramref name="value"/> is a well-formed hash: <c>sha256:</c> plus 64 lower-case hex characters.</summary>
	public static bool IsWellFormed(string? value)
	{
		if (value is null || value.Length != Prefix.Length + HexLength)
			return false;
		if (!value.StartsWith(Prefix, StringComparison.Ordinal))
			return false;

		for (var i = Prefix.Length; i < value.Length; i++)
		{
			var c = value[i];
			if (c is (< '0' or > '9') and (< 'a' or > 'f'))
				return false;
		}
		return true;
	}
}
