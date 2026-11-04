// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Shared gate for repo-scoped CDN vs local changelog-entry sourcing.
/// Used by <c>changelog bundle</c> and <c>changelog bundle-amend</c>.
/// </summary>
internal static class ChangelogEntrySourcing
{
	public const string DefaultOwner = "elastic";
	public const string DefaultBranch = "main";

	/// <summary>
	/// True when the authoring repo resolves, local sourcing is not forced
	/// (<c>bundle.use_local_changelogs</c> / <c>--force-local</c> / <c>--directory</c>),
	/// and a CDN base is configured.
	/// </summary>
	public static bool ShouldSourceFromCdn(string? authoringRepo, bool useLocalChangelogs, bool explicitDirectory = false)
	{
		if (useLocalChangelogs || explicitDirectory || string.IsNullOrWhiteSpace(authoringRepo))
			return false;
		return ChangelogCdn.ResolveBaseUri() is not null;
	}
}
