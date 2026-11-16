// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>Every rule an <see cref="AssemblyConfiguration"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class AssemblyConstraints
{
	/// <summary>The environments the runtime accepts (<c>SiteEnvironmentValidator</c>); any other name falls back to <c>dev</c> there but still reaches index names.</summary>
	private static readonly string[] RuntimeEnvironments = ["dev", "edge", "staging", "prod"];

	public static Constraint<AssemblyConfiguration> NarrativeNameReserved { get; } = Constraints<AssemblyConfiguration>.Always(
		m => !m.ReferenceRepositories.ContainsKey(NarrativeRepository.RepositoryName),
		$"An item with the same key has already been added. Key: {NarrativeRepository.RepositoryName}"
	).Describe("No referenced repository is named after the narrative repository.");

	public static Constraint<AssemblyConfiguration> GoogleTagManagerHasId { get; } = Constraints<AssemblyConfiguration>.Each(
		m => m.Environments.Values,
		e => !e.GoogleTagManager.Enabled || !string.IsNullOrEmpty(e.GoogleTagManager.Id),
		_ => "Id is required when Enabled is true."
	).Describe("An environment that enables Google Tag Manager names its container id.");

	public static Constraint<AssemblyConfiguration> EnvironmentUriAbsolute { get; } = Constraints<AssemblyConfiguration>
		.Each(
			m => m.Environments.Values,
			e => System.Uri.TryCreate(e.Uri, UriKind.Absolute, out _),
			e => $"Environment '{e.Name}' has uri '{e.Uri}', which is not an absolute URI."
		)
		.WithSeverity(Severity.Warning)
		.Describe("Every environment's uri is an absolute URI.");

	public static Constraint<AssemblyConfiguration> OptimizelyHasId { get; } = Constraints<AssemblyConfiguration>
		.Each(
			m => m.Environments.Values,
			e => !e.Optimizely.Enabled || !string.IsNullOrWhiteSpace(e.Optimizely.Id),
			e => $"Environment '{e.Name}' enables Optimizely without an id."
		)
		.WithSeverity(Severity.Warning)
		.Describe("An environment that enables Optimizely names its project id.");

	public static Constraint<AssemblyConfiguration> EnvironmentPathPrefixResolved { get; } = Constraints<AssemblyConfiguration>
		.Each(
			m => m.Environments.Values,
			e => e.PathPrefix?.Contains("${", StringComparison.Ordinal) != true,
			e => $"Environment '{e.Name}' path_prefix '{e.PathPrefix}' is an unresolved placeholder: assembler.yml is never interpolated."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"G12: An environment's path_prefix is a literal; assembler.yml is never interpolated, so a ${PLACEHOLDER} is only replaced by a CI rewrite."
		);

	public static Constraint<AssemblyConfiguration> RepositoryNameMatchesKey { get; } = Constraints<AssemblyConfiguration>
		.KeysMatch(a => a.ReferenceRepositories, r => r.Name, message: "a referenced repository is keyed by a name other than its own.")
		.WithSeverity(Severity.Warning)
		.Describe("G15: A referenced repository's name is the key it is listed under in 'references'.");

	public static Constraint<AssemblyConfiguration> PrivateKeysMatchNames { get; } = Constraints<AssemblyConfiguration>
		.KeysMatch(a => a.PrivateRepositories, r => r.Name, message: "a private repository is keyed by a name other than its own.")
		.WithSeverity(Severity.Warning)
		.Describe("G15: A private repository's name is the key it is listed under.");

	public static Constraint<AssemblyConfiguration> NarrativeNotSkipped { get; } = Constraints<AssemblyConfiguration>
		.Always(
			a => a.Narrative.Skip,
			skip => !skip,
			"Narrative repository 'docs-content' sets skip, which is ignored: the narrative is always published."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G17: 'skip' on the narrative repository is ignored; it is always published.");

	public static Constraint<AssemblyConfiguration> RepositorySparsePathsValid { get; } = Constraints<AssemblyConfiguration>
		.Each(
			AllRepositories,
			r => r.SparsePaths.Length > 0 && !r.SparsePaths.Any(string.IsNullOrWhiteSpace),
			r => $"Repository '{r.Name}' has an empty sparse_paths entry or no sparse_paths at all."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G24: A repository's sparse_paths lists at least one path and none of them is blank.");

	public static Constraint<AssemblyConfiguration> RepositoryCloneTimeoutPositive { get; } = Constraints<AssemblyConfiguration>.Each(
		AllRepositories,
		r => r.CloneTimeout is not { } timeout || timeout > TimeSpan.Zero,
		r => $"Repository '{r.Name}' has a clone_timeout of {r.CloneTimeout}, which is not a positive duration."
	).Describe("G25: A repository's clone_timeout is a positive duration.");

	public static Constraint<AssemblyConfiguration> RepositorySkipped { get; } = Constraints<AssemblyConfiguration>
		.Each(m => m.ReferenceRepositories.Values, r => !r.Skip, r => $"Repository '{r.Name}' is skipped and not built.")
		.WithSeverity(Severity.Warning)
		.Describe("G28: A skipped repository is not built; it is listed so the omission is deliberate.");

	public static Constraint<AssemblyConfiguration> RepositoryNamesUniqueIgnoringCase { get; } = Constraints<AssemblyConfiguration>
		.Unique(
			a => a.AvailableRepositories.Keys,
			k => k,
			StringComparer.OrdinalIgnoreCase,
			"available repository names collide when case is ignored, as AllPrivateRepositoryNames compares them."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G30: Repository names are unique when case is ignored.");

	public static Constraint<AssemblyConfiguration> PrivateIsAvailable { get; } = Constraints<AssemblyConfiguration>
		.References<KeyValuePair<string, Repository>, string>(
			a => a.PrivateRepositories,
			kv => [kv.Key],
			a => a.AvailableRepositories.Keys,
			message: "a private repository is not in the available repositories."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G30: Every private repository that publishes docs is an available repository.");

	public static Constraint<AssemblyConfiguration> PrivateIsListed { get; } = Constraints<AssemblyConfiguration>
		.References<KeyValuePair<string, Repository>, string>(
			a => a.PrivateRepositories,
			kv => [kv.Key],
			a => a.AllPrivateRepositoryNames,
			StringComparer.OrdinalIgnoreCase,
			"a private repository is missing from AllPrivateRepositoryNames."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G30: Every private repository is in the set of all private repository names.");

	public static Constraint<AssemblyConfiguration> EnvironmentNameInRuntimeList { get; } = Constraints<AssemblyConfiguration>
		.EachOneOf(m => m.Environments.Keys, RuntimeEnvironments)
		.WithSeverity(Severity.Warning)
		.Describe("G36: An environment is named one of dev, edge, staging or prod, the names the runtime accepts.");

	public static Constraint<AssemblyConfiguration> FeatureFlagKeysNormalized { get; } = Constraints<AssemblyConfiguration>
		.Each(
			m => m.Environments.Values.SelectMany(e => e.FeatureFlags.Keys.Select(key => (Environment: e.Name, Key: key))),
			f => string.Equals(f.Key, f.Key.ToLowerInvariant(), StringComparison.Ordinal) && !f.Key.Contains('_', StringComparison.Ordinal),
			f =>
				$"Environment '{f.Environment}' feature flag '{f.Key}' is not lower-case with '-', and is read as '{f.Key.ToLowerInvariant().Replace('_', '-')}'."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G37: A feature flag key is written lower-case with '-' for '_', the form it is read as.");

	public static Constraint<AssemblyConfiguration> EnvironmentWebsiteSearchUrlAbsolute { get; } = Constraints<AssemblyConfiguration>
		.Each(
			m => m.Environments.Values,
			e => e.WebsiteSearchScriptUrl is not { } url || System.Uri.TryCreate(url, UriKind.Absolute, out _),
			e => $"Environment '{e.Name}' has website_search_url '{e.WebsiteSearchScriptUrl}', which is not an absolute URI."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G39: An environment's website_search_url is an absolute URI.");

	public static Constraint<AssemblyConfiguration> RepositoryNameSingleSegment { get; } = Constraints<AssemblyConfiguration>.Each(
		AllRepositories,
		r => !string.IsNullOrWhiteSpace(r.Name) && r.Name is not ("." or "..") && !r.Name.Contains('/', StringComparison.Ordinal) &&
			!r.Name.Contains('\\', StringComparison.Ordinal),
		r => $"Repository name '{r.Name}' must be a single relative path segment."
	).Describe("P16: A repository name is a single path segment: it is the name of its checkout folder.");

	public static Constraint<AssemblyConfiguration> EnvironmentNameStoreSafe { get; } = Constraints<AssemblyConfiguration>
		.Each(
			m => m.Environments.Values,
			e => e.Name.Length > 0 && e.Name.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'),
			e =>
				$"Environment '{e.Name}' cannot be used in the redirects store name '{{prefix}}-{e.Name}-redirects-kvs': use letters, digits and '-'."
		)
		.WithSeverity(Severity.Warning)
		.Describe("P26: An environment name is letters, digits and '-', so it can be part of a redirects key-value store name.");

	public static Constraint<AssemblyConfiguration> EnvironmentNameLowercase { get; } = Constraints<AssemblyConfiguration>
		.Each(
			m => m.Environments.Values,
			e => string.Equals(e.Name, e.Name.ToLowerInvariant(), StringComparison.Ordinal),
			e => $"Environment '{e.Name}' is used in index names and must be lower-case."
		)
		.WithSeverity(Severity.Warning)
		.Describe("S1: An environment name is lower-case, because it becomes part of an Elasticsearch index name.");

	private static IEnumerable<Repository> AllRepositories(AssemblyConfiguration assembly) =>
		[.. assembly.ReferenceRepositories.Values, assembly.Narrative];
}
