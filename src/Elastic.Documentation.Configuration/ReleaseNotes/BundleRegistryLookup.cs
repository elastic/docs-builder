// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// The answer of the published changelog registry to "is this bundle there?". A caller that writes a bundle
/// matches all cases, so an unreachable registry is never mistaken for a bundle that is not published.
/// </summary>
public readonly union BundleRegistryLookup(BundlePublished, BundleNotPublished, BundleRegistryUnavailable);

/// <summary>The registry lists the bundle. <paramref name="AmendFiles"/> are the file names of its amend sidecars.</summary>
public sealed record BundlePublished(IReadOnlyList<string> AmendFiles);

/// <summary>The registry is readable and does not list the bundle, or the product has no registry yet (404).</summary>
public sealed record BundleNotPublished;

/// <summary>The registry could not be read, so nothing is known about the bundle.</summary>
public sealed record BundleRegistryUnavailable(string Reason);
