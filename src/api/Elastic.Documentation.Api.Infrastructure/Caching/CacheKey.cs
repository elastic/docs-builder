// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;

namespace Elastic.Documentation.Api.Infrastructure.Caching;

/// <summary>
/// Represents a cache key with automatic hashing of sensitive identifiers.
/// Prevents exposing sensitive data in cache keys (CodeQL security requirement).
/// </summary>
public sealed class CacheKey
{
	/// <summary>
	/// Gets the hashed key string for use in cache operations.
	/// </summary>
	public string Value { get; }

	private CacheKey(string category, string identifier)
	{
		// Hash the identifier to prevent exposing sensitive data (CodeQL security requirement)
		var bytes = Encoding.UTF8.GetBytes(identifier);
		var hash = SHA256.HashData(bytes);
		var hashBase64 = Convert.ToBase64String(hash);
		// Use base64url encoding for cache key (URL-safe)
		var hashBase64Url = hashBase64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
		Value = $"{category}:{hashBase64Url}";
	}

	/// <summary>
	/// Creates a cache key from a category and identifier.
	/// The identifier is automatically hashed to prevent exposing sensitive data.
	/// </summary>
	/// <param name="category">Cache category (e.g., "idtoken", "search")</param>
	/// <param name="identifier">Identifier that may contain sensitive data (will be hashed)</param>
	/// <returns>A CacheKey instance with the hashed key</returns>
	public static CacheKey Create(string category, string identifier) => new(category, identifier);

	/// <summary>
	/// Implicit conversion to string for convenience.
	/// </summary>
	public static implicit operator string(CacheKey key) => key.Value;
}
