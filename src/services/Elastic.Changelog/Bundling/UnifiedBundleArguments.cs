// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.ReleaseSchedule;

namespace Elastic.Changelog.Bundling;

/// <summary>Builds the bundler arguments for one product of <c>release unified bundle</c>.</summary>
public static class UnifiedBundleArguments
{
	/// <summary>The GitHub organization of every product repository in the DRA pipeline.</summary>
	public const string Owner = "elastic";

	/// <summary>
	/// The profile shapes the bundle, the version fills its placeholders, and the commit range replaces the
	/// filter: the pull requests are those between the two manifest commits.
	/// </summary>
	public static BundleChangelogsArguments Create(
		ProductBundleRange range,
		string profileName,
		string configPath,
		string outputDirectory
	) =>
		new()
		{
			Profile = profileName,
			ProfileArgument = range.Version,
			OutputDirectory = outputDirectory,
			Config = configPath,
			StartGitRef = range.StartCommit,
			EndGitRef = range.EndCommit,
			Owner = Owner,
			Repo = range.RepoKey
		};
}
