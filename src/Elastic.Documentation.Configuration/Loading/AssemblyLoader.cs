// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Loads <c>assembler.yml</c> into an <see cref="AssemblyConfiguration"/>: applies repository defaults, derives each
/// repository's git remote from the CI environment, and drops private repositories when asked to.
/// </summary>
public static class AssemblyLoader
{
	internal static Loader<AssemblyConfigurationDto, AssemblyConfiguration> Yaml(
		bool skipPrivateRepositories,
		IEnvironmentVariables environment
	) => Loader.For(parse: (text, file) => ReadDto(text, file), map: (dto, _) => ToRecord(dto, skipPrivateRepositories, environment));

	/// <summary>
	/// Loads the provider's <c>assembler.yml</c>. A file that cannot be read fails with the exception the parser raises, as it always has,
	/// and the process prints it first.
	/// </summary>
	public static AssemblyConfiguration Create(ConfigurationFileProvider provider, IEnvironmentVariables? environment = null) =>
		Deserialize(
			provider.AssemblerFile.ReadToEnd(),
			skipPrivateRepositories: provider.SkipPrivateRepositories,
			sourceName: provider.AssemblerFile.FullName,
			environment: environment
		);

	/// <summary>The same for <c>assembler.yml</c> text (an embedded copy, for instance).</summary>
	public static AssemblyConfiguration Deserialize(
		string yaml,
		bool skipPrivateRepositories = false,
		string? sourceName = null,
		IEnvironmentVariables? environment = null
	)
	{
		environment ??= SystemEnvironmentVariables.Instance;
		var result = Yaml(skipPrivateRepositories, environment).Load(yaml, sourceName ?? string.Empty);
		if (result is Loaded<AssemblyConfiguration> loaded)
			return loaded.Value;

		// the loader reports a failing step as a finding with the exception's message; running the steps again raises the exception itself
		try
		{
			_ = ToRecord(ReadDto(yaml, sourceName), skipPrivateRepositories, environment);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			Console.WriteLine(e.InnerException);
			throw;
		}

		return result.OrThrow();
	}

	private static AssemblyConfigurationDto ReadDto(string yaml, string? sourceName) =>
		ConfigurationFileProvider.Deserialize<AssemblyConfigurationDto>(yaml, sourceName);

	private static AssemblyConfiguration ToRecord(
		AssemblyConfigurationDto dto,
		bool skipPrivateRepositories,
		IEnvironmentVariables environment
	)
	{
		var references = new Dictionary<string, Repository>();
		foreach (var (name, r) in dto.ReferenceRepositories)
			references[name] = RepositoryDefaults(ToRepository(r, name), environment);

		// If we are skipping private repositories, and we can locate the solution directory. include the local docs-content repository
		// this allows us to test new docset features as part of the assembler build
		if (
			skipPrivateRepositories
			&& references.TryGetValue("docs-builder", out var docsContentRepository)
			&& Paths.GetSolutionDirectory() is { } solutionDir
		)
		{
			var docsRepositoryPath = Path.Join(solutionDir.FullName, "docs");
			references["docs-builder"] = docsContentRepository with { Skip = false, Path = docsRepositoryPath };
		}

		var privateRepositories = references.Where(r => r.Value.Private).ToList();
		if (skipPrivateRepositories)
		{
			foreach (var (name, _) in privateRepositories)
				_ = references.Remove(name);
		}

		// an environment without a body is null here and fails on its first read, as it always has
		var environments = new Dictionary<string, PublishEnvironment>();
		foreach (var (name, environmentDto) in dto.Environments)
			environments[name] = ToEnvironment(environmentDto, name);

		var narrative = (NarrativeRepository)RepositoryDefaults(ToNarrative(dto.Narrative), environment);
		var configuration = new AssemblyConfiguration
		{
			Narrative = narrative,
			ReferenceRepositories = references,
			PrivateRepositories = privateRepositories.Where(r => !r.Value.Skip).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
			AllPrivateRepositoryNames = new HashSet<string>(privateRepositories.Select(r => r.Key), StringComparer.OrdinalIgnoreCase),
			Environments = environments,
			SharedConfigurations = dto.SharedConfigurations.ToDictionary(kv => kv.Key, kv => ToRepository(kv.Value, string.Empty))
		};
		// derives the available repositories now: a referenced repository named like the narrative fails the load
		_ = configuration.AvailableRepositories;
		return configuration;
	}

	private static Repository ToRepository(RepositoryDto? dto, string name) =>
		dto is null ? new Repository { Name = name } : Copy(dto, new Repository()) with { Name = name };

	private static NarrativeRepository ToNarrative(RepositoryDto? dto) =>
		dto is null ? new NarrativeRepository() : Copy(dto, new NarrativeRepository());

	private static TRepository Copy<TRepository>(RepositoryDto dto, TRepository empty) where TRepository : Repository =>
		empty with
		{
			Origin = dto.Origin,
			CheckoutStrategy = dto.CheckoutStrategy,
			Skip = dto.Skip,
			Path = dto.Path,
			GitReferenceCurrent = dto.GitReferenceCurrent,
			GitReferenceNext = dto.GitReferenceNext,
			GitReferenceEdge = dto.GitReferenceEdge,
			SparsePaths = dto.SparsePaths,
			Private = dto.Private,
			CloneTimeout = dto.CloneTimeout
		};

	private static PublishEnvironment ToEnvironment(PublishEnvironmentDto dto, string name) =>
		new()
		{
			Name = name,
			Uri = dto.Uri,
			PathPrefix = dto.PathPrefix,
			AllowIndexing = dto.AllowIndexing,
			ContentSource = dto.ContentSource,
			GoogleTagManager = new GoogleTagManager
			{
				Enabled = dto.GoogleTagManager.Enabled,
				Id = dto.GoogleTagManager.Id,
				Auth = dto.GoogleTagManager.Auth,
				Preview = dto.GoogleTagManager.Preview,
				CookiesWin = dto.GoogleTagManager.CookiesWin
			},
			Optimizely = new Optimizely { Enabled = dto.Optimizely.Enabled, Id = dto.Optimizely.Id },
			FeatureFlags = dto.FeatureFlags,
			WebsiteSearchScriptUrl = dto.WebsiteSearchScriptUrl,
			WafIntegrationUrl = dto.WafIntegrationUrl
		};

	private static Repository RepositoryDefaults(Repository repository, IEnvironmentVariables environment)
	{
		var name = repository.Name;
		repository = repository with
		{
			GitReferenceCurrent = string.IsNullOrEmpty(repository.GitReferenceCurrent) ? "main" : repository.GitReferenceCurrent,
			GitReferenceNext = string.IsNullOrEmpty(repository.GitReferenceNext) ? "main" : repository.GitReferenceNext,
			GitReferenceEdge = string.IsNullOrEmpty(repository.GitReferenceEdge) ? "main" : repository.GitReferenceEdge,
		};
		// ensure we always null path if we are running in CI
		if (!string.IsNullOrWhiteSpace(environment.GetEnvironmentVariable("CI")))
			repository = repository with { Path = null };

		if (!string.IsNullOrEmpty(repository.Origin))
			return repository;

		if (!string.IsNullOrEmpty(environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
		{
			var token = environment.GetEnvironmentVariable("GITHUB_TOKEN");
			return repository with
			{
				Origin = !string.IsNullOrEmpty(token)
					? $"https://oauth2:{token}@github.com/elastic/{name}.git"
					: $"https://github.com/elastic/{name}.git"
			};
		}

		return repository with { Origin = $"git@github.com:elastic/{name}.git" };
	}
}
