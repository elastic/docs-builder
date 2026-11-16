// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Bundling;

/// <summary>Where a release is written: its own bundle file, or the next amend of a bundle that is already published.</summary>
internal readonly union BundleWriteTarget(NewBundleTarget, AmendTarget, TargetLookupFailed);

/// <summary>No bundle of that name is published: write the bundle file at the resolved output path.</summary>
internal sealed record NewBundleTarget;

/// <summary>A bundle of that name is already published: write <paramref name="Path"/>, its next <c>.amend-N</c> sidecar.</summary>
internal sealed record AmendTarget(string Path);

/// <summary>The published registry could not be read. The error is already reported; nothing is written.</summary>
internal sealed record TargetLookupFailed;
