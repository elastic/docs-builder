// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;

namespace Elastic.Documentation.Search;

/// <summary>Creates a short hex hash from one or more string components.</summary>
public static class ContentHash
{
	/// <summary>
	/// Concatenates all components, computes SHA-256, and returns the first 16 hex characters (lowercased).
	/// Compatible with <c>HashedBulkUpdate.CreateHash</c>.
	/// </summary>
	public static string Create(params string[] components) =>
		Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("", components))))[..16].ToLowerInvariant();
}
