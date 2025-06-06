// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
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
		var match = new ContentSourceMatch(null, null, false);
		var tokens = repository.Split('/');
		var repositoryName = tokens.Last();
		var owner = tokens.First();

		if (tokens.Length < 2 || owner != "elastic")
			return match;

		if (ReferenceRepositories.TryGetValue(repositoryName, out var r))
		{
			var current = r.GetBranch(ContentSource.Current);
			var next = r.GetBranch(ContentSource.Next);
			var isVersionBranch = ContentSourceRegex.MatchVersionBranch().IsMatch(branchOrTag);
			if (current == branchOrTag)
				match = match with { Current = ContentSource.Current };
			if (next == branchOrTag)
				match = match with { Next = ContentSource.Next };
			if (isVersionBranch && SemVersion.TryParse(branchOrTag + ".0", out var v))
			{
				// if the current branch is a version, only speculatively match if branch is actually a new version
				if (SemVersion.TryParse(current + ".0", out var currentVersion))
				{
					if (v >= currentVersion)
						match = match with { Speculative = true };
				}
				// assume we are newly onboarding the repository to current/next
				else
					match = match with { Speculative = true };
			}
			return match;
		}

		if (repositoryName != NarrativeRepository.RepositoryName)
		{
			// this is an unknown new elastic repository
			var isVersionBranch = ContentSourceRegex.MatchVersionBranch().IsMatch(branchOrTag);
			if (isVersionBranch || branchOrTag == "main" || branchOrTag == "master")
				return match with { Speculative = true };
		}

		if (Narrative.GetBranch(ContentSource.Current) == branchOrTag)
			match = match with { Current = ContentSource.Current };
		if (Narrative.GetBranch(ContentSource.Next) == branchOrTag)
			match = match with { Next = ContentSource.Next };

		return match;
	}

	public record ContentSourceMatch(ContentSource? Current, ContentSource? Next, bool Speculative);

}

internal static partial class ContentSourceRegex
{
	[GeneratedRegex(@"^\d+\.\d+$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchVersionBranch();
}
