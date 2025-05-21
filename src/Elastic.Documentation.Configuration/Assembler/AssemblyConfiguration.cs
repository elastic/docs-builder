// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Serialization;
using YamlDotNet.Serialization;
using YamlStaticContext = Elastic.Documentation.Configuration.Serialization.YamlStaticContext;

namespace Elastic.Documentation.Configuration.Assembler;

public record AssemblyConfiguration
{
	public static AssemblyConfiguration Deserialize(string yaml)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
			.IgnoreUnmatchedProperties()
			.Build();

		try
		{
			var config = deserializer.Deserialize<AssemblyConfiguration>(input);
			foreach (var (name, r) in config.ReferenceRepositories)
			{
				var repository = RepositoryDefaults(r, name);
				config.ReferenceRepositories[name] = repository;
			}

			foreach (var (name, env) in config.Environments)
				env.Name = name;
			config.Narrative = RepositoryDefaults(config.Narrative, NarrativeRepository.RepositoryName);
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
		repository.Name = name;
		if (string.IsNullOrEmpty(repository.GitReferenceCurrent))
			repository.GitReferenceCurrent = "main";
		if (string.IsNullOrEmpty(repository.GitReferenceNext))
			repository.GitReferenceNext = "main";
		if (string.IsNullOrEmpty(repository.Origin))
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
			{
				var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
				repository.Origin = !string.IsNullOrEmpty(token)
					? $"https://oath2:{token}@github.com/elastic/{name}.git"
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

	[YamlMember(Alias = "environments")]
	public Dictionary<string, PublishEnvironment> Environments { get; set; } = [];

	[YamlMember(Alias = "named_git_references")]
	public Dictionary<string, string> NamedGitReferences { get; set; } = [];

	/// Returns whether the <paramref name="branchOrTag"/> is configured as an integration branch or tag for the given
	/// <paramref name="repository"/>.
	public ContentSourceMatch Match(string repository, string branchOrTag)
	{
		var repositoryName = repository.Split('/').Last();
		var match = new ContentSourceMatch(null, null);
		if (ReferenceRepositories.TryGetValue(repositoryName, out var r))
		{
			if (r.GetBranch(ContentSource.Current) == branchOrTag)
				match = match with { Current = ContentSource.Current };
			if (r.GetBranch(ContentSource.Next) == branchOrTag)
				match = match with { Next = ContentSource.Next };
			return match;
		}

		if (repositoryName != NarrativeRepository.RepositoryName)
			return match;

		if (Narrative.GetBranch(ContentSource.Current) == branchOrTag)
			match = match with { Current = ContentSource.Current };
		if (Narrative.GetBranch(ContentSource.Next) == branchOrTag)
			match = match with { Next = ContentSource.Next };

		return match;
	}

	public record ContentSourceMatch(ContentSource? Current, ContentSource? Next);
}
