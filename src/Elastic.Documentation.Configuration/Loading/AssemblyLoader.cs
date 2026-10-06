// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
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
	public static Result<AssemblyConfiguration> Load(ConfigurationFileProvider provider, IEnvironmentVariables? environment = null) =>
		LoadYaml(provider.AssemblerFile.ReadToEnd(), provider.AssemblerFile.FullName, provider.SkipPrivateRepositories, environment);

	/// <summary>Checked load of <c>assembler.yml</c> text (an embedded copy, for instance). Parse + Map + Rules through <see cref="LoaderPipeline"/>: a parser failure becomes an error diagnostic, an error violation means no configuration.</summary>
	public static Result<AssemblyConfiguration> LoadYaml(
		string yaml,
		string sourceFile,
		bool skipPrivateRepositories = false,
		IEnvironmentVariables? environment = null
	)
	{
		environment ??= SystemEnvironmentVariables.Instance;
		var findings = new List<Diagnostic>();
		return LoaderPipeline.Run(
			sourceFile,
			parse: () => Parse(yaml),
			map: dto => Map(dto, sourceFile, skipPrivateRepositories, environment, findings),
			rules: AssemblyConstraints.All,
			extra: findings
		);
	}

	private static readonly ConditionalWeakTable<ConfigurationFileProvider, Result<AssemblyConfiguration>> LoadedByProvider = [];

	/// <summary>
	/// <see cref="Load"/>, once per provider: a provider's configuration files are fixed for its lifetime, so every
	/// caller in a build (one per <c>{changelog}</c> directive, for instance) shares a single parse.
	/// </summary>
	public static Result<AssemblyConfiguration> LoadOnce(ConfigurationFileProvider provider) =>
		LoadedByProvider.GetValue(provider, p => Load(p));

	/// <summary>For composition roots, where an invalid <c>assembler.yml</c> must stop the process with the parser's exception.</summary>
	public static AssemblyConfiguration Create(ConfigurationFileProvider provider, IEnvironmentVariables? environment = null) =>
		Load(provider, environment).OrThrow();

	/// <summary>Text in, checked configuration out or a <see cref="ConfigurationException"/>: for tests that hold the YAML, not a provider.</summary>
	internal static AssemblyConfiguration Deserialize(
		string yaml,
		bool skipPrivateRepositories = false,
		IEnvironmentVariables? environment = null
	)
	{
		// A document the parser rejects throws its own exception here (LoadYaml reports it as a diagnostic instead); the rules are checked once.
		var findings = new List<Diagnostic>();
		var configuration = Map(
			Parse(yaml),
			string.Empty,
			skipPrivateRepositories,
			environment ?? SystemEnvironmentVariables.Instance,
			findings
		);
		return LoaderPipeline.Check(configuration, string.Empty, AssemblyConstraints.All, findings).OrThrow();
	}

	private static AssemblyConfigurationDto Parse(string yaml)
	{
		using var input = new StringReader(yaml);
		return ConfigurationFileProvider.Deserializer.Deserialize<AssemblyConfigurationDto>(input) ?? new AssemblyConfigurationDto();
	}

	private static AssemblyConfiguration Map(
		AssemblyConfigurationDto dto,
		string sourceFile,
		bool skipPrivateRepositories,
		IEnvironmentVariables environment,
		List<Diagnostic> findings
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

		var narrative = (NarrativeRepository)RepositoryDefaults(ToNarrative(dto.Narrative), environment);

		var environments = new Dictionary<string, PublishEnvironment>();
		foreach (var (name, dtoEnvironment) in dto.Environments)
		{
			if (dtoEnvironment.ContentSource is null)
			{
				findings.Add(new Diagnostic
				{
					Severity = Severity.Warning,
					File = sourceFile,
					Message =
						$"Environment '{name}' does not declare content_source, so it publishes 'next'. Set content_source to current, next or edge."
				});
			}
			environments[name] = ToEnvironment(dtoEnvironment, name);
		}

		return new AssemblyConfiguration
		{
			Narrative = narrative,
			ReferenceRepositories = references,
			AvailableRepositories = AvailableByName(references.Values.Where(r => !r.Skip).Concat([narrative])),
			PrivateRepositories = privateRepositories.Where(r => !r.Value.Skip).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
			AllPrivateRepositoryNames = new HashSet<string>(privateRepositories.Select(r => r.Key), StringComparer.OrdinalIgnoreCase),
			Environments = environments,
			SharedConfigurations = dto.SharedConfigurations.ToDictionary(kv => kv.Key, kv => ToRepository(kv.Value, string.Empty))
		};
	}

	// Last wins, so a model with colliding names can be built and reported by AssemblyConstraints.
	private static Dictionary<string, Repository> AvailableByName(IEnumerable<Repository> repositories)
	{
		var byName = new Dictionary<string, Repository>();
		foreach (var repository in repositories)
			byName[repository.Name] = repository;
		return byName;
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
			ContentSource = dto.ContentSource ?? ContentSource.Next,
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
