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
	/// <summary>Loads the provider's <c>assembler.yml</c>. Invalid input fails the load with the parser's message.</summary>
	public static Result<AssemblyConfiguration> Load(ConfigurationFileProvider provider, IEnvironmentVariables? environment = null)
	{
		try
		{
			return Result.Success(Create(provider, environment));
		}
		catch (Exception e)
		{
			return Result.Failure<AssemblyConfiguration>([
				new Diagnostic { Severity = Severity.Error, File = provider.AssemblerFile.FullName, Message = e.Message }
			]);
		}
	}

	/// <summary>For composition roots, where an invalid <c>assembler.yml</c> must stop the process with the parser's exception.</summary>
	public static AssemblyConfiguration Create(ConfigurationFileProvider provider, IEnvironmentVariables? environment = null) =>
		Deserialize(provider.AssemblerFile.ReadToEnd(), provider.SkipPrivateRepositories, environment);

	public static AssemblyConfiguration Deserialize(
		string yaml,
		bool skipPrivateRepositories = false,
		IEnvironmentVariables? environment = null
	)
	{
		environment ??= SystemEnvironmentVariables.Instance;
		var input = new StringReader(yaml);

		try
		{
			var dto = ConfigurationFileProvider.Deserializer.Deserialize<AssemblyConfigurationDto>(input) ?? new AssemblyConfigurationDto();
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

			var narrative = (NarrativeRepository)RepositoryDefaults(ToNarrative(dto.Narrative), environment);

			return new AssemblyConfiguration
			{
				Narrative = narrative,
				ReferenceRepositories = references,
				AvailableRepositories = references.Values.Where(r => !r.Skip).Concat([narrative]).ToDictionary(kvp => kvp.Name, kvp => kvp),
				PrivateRepositories = privateRepositories.Where(r => !r.Value.Skip).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
				AllPrivateRepositoryNames = new HashSet<string>(privateRepositories.Select(r => r.Key), StringComparer.OrdinalIgnoreCase),
				Environments = dto.Environments.ToDictionary(kv => kv.Key, kv => ToEnvironment(kv.Value, kv.Key)),
				SharedConfigurations = dto.SharedConfigurations.ToDictionary(kv => kv.Key, kv => ToRepository(kv.Value, string.Empty))
			};
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			Console.WriteLine(e.InnerException);
			throw;
		}
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
			WebsiteSearchScriptUrl = dto.WebsiteSearchScriptUrl
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
