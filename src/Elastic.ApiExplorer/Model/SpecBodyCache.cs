// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// Downloaded spec bodies keyed by object key. Lives longer than any one <see cref="VersionIndexClient"/>,
/// so <c>serve</c> can re-fetch the version index on each regeneration but skip re-downloading specs.
/// </summary>
public sealed class SpecBodyCache
{
	private readonly ConcurrentDictionary<string, byte[]> _bodies = new(StringComparer.Ordinal);

	internal bool TryGet(string objectKey, [NotNullWhen(true)] out byte[]? body) => _bodies.TryGetValue(objectKey, out body);

	internal byte[] GetOrAdd(string objectKey, byte[] body) => _bodies.GetOrAdd(objectKey, body);
}
