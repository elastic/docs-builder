// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Markdown.Tests.Tracking;

/// <summary>
/// Groups tests that mutate process-wide environment variables
/// (GITHUB_ACTIONS, ADDED_FILES, etc.) so they run sequentially and
/// never observe each other's state.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
	Justification = "xUnit collection-definition marker classes idiomatically end with 'Collection'.")]
public sealed class TrackingTestCollection
{
	public const string Name = "Tracking";
}
