// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Extensions;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

public record AssemblyConfiguration
{
	public static AssemblyConfiguration Create(ConfigurationFileProvider provider) =>
		Deserialize(provider.AssemblerFile.ReadToEnd(), skipPrivateRepositories: provider.SkipPrivateRepositories);

	public static AssemblyConfiguration Deserialize(string yaml, bool skipPrivateRepositories = false)
	{
		var input = new StringReader(yaml);

		try
		{
			var config = ConfigurationFileProvider.Deserializer.Deserialize<AssemblyConfiguration>(input);
			foreach (var (name, r) in config.ReferenceRepositories)
			{
				var repository = RepositoryDefaults(r, name);
				config.ReferenceRepositories[name] = repository;
			}

			// If we are skipping private repositories, and we can locate the solution directory. include the local docs-content repository
			// this allows us to test new docset features as part of the assembler build
			if (skipPrivateRepositories
				&& config.ReferenceRepositories.TryGetValue("docs-builder", out var docsContentRepository)
				&& Paths.GetSolutionDirectory() is { } solutionDir
			   )
			{
				var docsRepositoryPath = Path.Combine(solutionDir.FullName, "docs");
				config.ReferenceRepositories["docs-builder"] = docsContentRepository with
				{
					Skip = false,
					Path = docsRepositoryPath
				};
			}


			var privateRepositories = config.ReferenceRepositories.Where(r => r.Value.Private).ToList();
			foreach (var (name, _) in privateRepositories)
			{
				if (skipPrivateRepositories)
					_ = config.ReferenceRepositories.Remove(name);
			}

			foreach (var (name, env) in config.Environments)
				env.Name = name;
			config.Narrative = RepositoryDefaults(config.Narrative, NarrativeRepository.RepositoryName);

			config.AvailableRepositories = config.ReferenceRepositories.Values
				.Where(r => !r.Skip)
				.Concat([config.Narrative]).ToDictionary(kvp => kvp.Name, kvp => kvp);

			config.PrivateRepositories = privateRepositories
				.Where(r => !r.Value.Skip)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			return config;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			Console.WriteLine(e.InnerException);
			throw;
		}
	}

	private static TRepository RepositoryDefaults<TRepository>(TRepository r, string name)
		where TRepository : Repository, new()
	{
		// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		var repository = r ?? new TRepository();
		// ReSharper restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		repository = repository with
		{
			Name = name,
			GitReferenceCurrent = string.IsNullOrEmpty(repository.GitReferenceCurrent) ? "main" : repository.GitReferenceCurrent,
			GitReferenceNext = string.IsNullOrEmpty(repository.GitReferenceNext) ? "main" : repository.GitReferenceNext,
			GitReferenceEdge = string.IsNullOrEmpty(repository.GitReferenceEdge) ? "main" : repository.GitReferenceEdge,
		};
		// ensure we always null path if we are running in CI
		if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")))
		{
			repository = repository with
			{
				Path = null
			};
		}

		if (string.IsNullOrEmpty(repository.Origin))
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
			{
				var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
				repository.Origin = !string.IsNullOrEmpty(token)
					? $"https://oauth2:{token}@github.com/elastic/{name}.git"
					: $"https://github.com/elastic/{name}.git";
			}
			else
				repository.Origin = $"git@github.com:elastic/{name}.git";
		}

		return repository;
	}

	[YamlMember(Alias = "narrative")]
	public NarrativeRepository Narrative { get; set; } = new();

	[YamlMember(Alias = "references")]
	public Dictionary<string, Repository> ReferenceRepositories { get; set; } = [];

	/// All available repositories, combines <see cref="ReferenceRepositories"/> and <see cref="Narrative"/> and will filter private repositories if `skip-private-repositories`
	/// is specified
	[YamlIgnore]
	public IReadOnlyDictionary<string, Repository> AvailableRepositories { get; private set; } = new Dictionary<string, Repository>();

	/// Repositories marked as private, these are listed under <see cref="AvailableRepositories"/> if `--skip-private-repositories` is not specified
	[YamlIgnore]
	public IReadOnlyDictionary<string, Repository> PrivateRepositories { get; private set; } = new Dictionary<string, Repository>();

	[YamlMember(Alias = "environments")]
	public Dictionary<string, PublishEnvironment> Environments { get; set; } = [];

	[YamlMember(Alias = "shared_configuration")]
	public Dictionary<string, Repository> SharedConfigurations { get; set; } = [];

	/// Returns whether the <paramref name="branchOrTag"/> is configured as an integration branch or tag for the given
	/// <paramref name="repository"/>.
	public ContentSourceMatch Match(ILoggerFactory logFactory, string repository, string branchOrTag, Product? product)
	{
		var logger = logFactory.CreateLogger<ContentSourceMatch>();
		var match = new ContentSourceMatch(null, null, null, false);
		var tokens = repository.Split('/');
		var repositoryName = tokens.Last();
		var owner = tokens.First();
		var isVersionBranch = ContentSourceRegex.MatchVersionBranch().IsMatch(branchOrTag);

		if (tokens.Length < 2 || owner != "elastic")
		{
			logger.LogInformation("Repository {Repository} is not a valid elastic repository but {Owner}", repository, owner);
			return match;
		}

		// Check for new repositories
		if (!AvailableRepositories.TryGetValue(repositoryName, out var r))
		{
			logger.LogInformation("Repository {Repository} has not yet been onboarded into assembler.yml", repository);
			// this is an unknown new elastic repository
			if (isVersionBranch || branchOrTag == "main" || branchOrTag == "master")
			{
				logger.LogInformation("Speculatively building {Repository} since it looks like an integration branch", repository);
				return match with
				{
					Speculative = true
				};
			}
			logger.LogInformation("{Repository} on '{Branch}' does not look like it needs a speculative build", repository, branchOrTag);
			return match;
		}

		var current = r.GetBranch(ContentSource.Current);
		var next = r.GetBranch(ContentSource.Next);
		var edge = r.GetBranch(ContentSource.Edge);
		logger.LogInformation("Active content-sources for {Repository}. current: {Current}, next: {Next}, edge: {Edge}' ", repository, current, next, edge);
		if (current == branchOrTag)
		{
			logger.LogInformation("Content-Source current: {Current} matches: {Branch}", current, branchOrTag);
			match = match with
			{
				Current = ContentSource.Current
			};
		}

		if (next == branchOrTag)
		{
			logger.LogInformation("Content-Source next: {Next} matches: {Branch}", next, branchOrTag);
			match = match with
			{
				Next = ContentSource.Next
			};
		}

		if (edge == branchOrTag)
		{
			logger.LogInformation("Content-Source edge: {Edge} matches: {Branch}", edge, branchOrTag);
			match = match with
			{
				Edge = ContentSource.Edge
			};
		}

		// check version branches
		if (isVersionBranch && SemVersion.TryParse(branchOrTag + ".0", out var v))
		{
			logger.LogInformation("Branch or tag {Branch} is a versioned branch", branchOrTag);
			// if the current branch is a version, only speculatively match if branch is actually a new version
			if (SemVersion.TryParse(current + ".0", out var currentVersion))
			{
				logger.LogInformation("Current is already using versioned branches {Current}", currentVersion);
				if (v >= currentVersion)
				{
					logger.LogInformation("Speculative build because {Branch} is gte current {Current}", branchOrTag, currentVersion);
					match = match with
					{
						Speculative = true
					};
				}
				else
					logger.LogInformation("NO speculative build because {Branch} is lt {Current}", branchOrTag, currentVersion);
			}
			// assume we are newly onboarding the repository to current/next
			else if (product?.VersioningSystem is { } versioningSystem)
			{
				logger.LogInformation("Current is not using versioned branches checking product info");
				var productVersion = versioningSystem.Current;
				var previousMinorVersion = new SemVersion(productVersion.Major, Math.Max(productVersion.Minor - 1, 0), 0);
				if (v >= productVersion)
				{
					logger.LogInformation("Speculative build {Branch} is gte product current '{ProductCurrent}'", branchOrTag, productVersion);
					match = match with
					{
						Speculative = true
					};
				}
				else if (v == previousMinorVersion)
				{
					logger.LogInformation("Speculative build {Branch} is gte product current previous minor '{ProductPreviousMinor}'", branchOrTag, previousMinorVersion);
					match = match with
					{
						Speculative = true
					};
				}
				else
					logger.LogInformation("NO speculative build {Branch} is lte product current '{ProductCurrent}'", branchOrTag, productVersion);
			}
			else
				logger.LogInformation("No versioning system found for {Repository} on {Branch}", repository, branchOrTag);
		}

		// if we haven't matched anything yet, and the branch is 'main' or 'master' always build
		if (match is { Current: null, Next: null, Edge: null, Speculative: false } && branchOrTag is "main" or "master")
		{
			return match with
			{
				Speculative = true
			};
		}

		return match;
	}

	public record ContentSourceMatch(ContentSource? Current, ContentSource? Next, ContentSource? Edge, bool Speculative);
}

internal static partial class ContentSourceRegex
{
	[GeneratedRegex(@"^\d+\.\d+$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchVersionBranch();
}
