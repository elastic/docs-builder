// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Assembler.Deploying.Redirects;

/// <summary>
/// Pure set-arithmetic helpers for diffing the locally-built redirects file against
/// the live CloudFront KeyValueStore so we can decide which keys to put and which to
/// delete. Kept independent of the AWS CLI wrapper so the logic is unit-testable.
/// </summary>
internal static class RedirectKvsDiff
{
	/// <summary>
	/// Produces the put/delete batches needed to reconcile the live KVS with the
	/// freshly-built redirects file.
	/// </summary>
	/// <param name="sourcedRedirects">Key/value pairs from the build's <c>redirects.json</c> (desired state).</param>
	/// <param name="existingRedirects">Keys currently present in the live KVS.</param>
	public static (PutKeyRequestListItem[] ToPut, DeleteKeyRequestListItem[] ToDelete) ComputeBatchUpdates(
		IReadOnlyDictionary<string, string> sourcedRedirects,
		IReadOnlyCollection<string> existingRedirects)
	{
		var toPut = sourcedRedirects
			.Select(kvp => new PutKeyRequestListItem { Key = kvp.Key, Value = kvp.Value })
			.ToArray();

		// Stale entries = keys in KVS that no longer appear in the new sourced file.
		// Operand order matters: it must be `existingRedirects.Except(sourcedRedirects.Keys)`.
		// The reverse (sourcedRedirects.Keys.Except(existingRedirects)) computes the
		// brand-new keys we are about to PUT, which makes the DELETE batch a no-op and
		// causes stale redirects to live in the KVS forever.
		var toDelete = existingRedirects
			.Except(sourcedRedirects.Keys)
			.Select(k => new DeleteKeyRequestListItem { Key = k })
			.ToArray();

		return (toPut, toDelete);
	}

	/// <summary>
	/// Returns true when applying the sourced redirects would wipe every existing entry
	/// from the KVS. Used as a sanity guard: an empty <c>redirects.json</c> almost certainly
	/// signals a broken build rather than an intentional reset.
	/// </summary>
	public static bool WouldWipeAllExisting(
		IReadOnlyDictionary<string, string> sourcedRedirects,
		IReadOnlyCollection<string> existingRedirects) =>
		sourcedRedirects.Count == 0 && existingRedirects.Count > 0;
}
