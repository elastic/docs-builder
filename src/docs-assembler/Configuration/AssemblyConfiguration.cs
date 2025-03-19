// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Documentation.Assembler.Configuration;

public record AssemblyConfiguration
{
	public static AssemblyConfiguration Deserialize(string yaml)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new Assembler.YamlStaticContext())
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
		if (string.IsNullOrEmpty(repository.CurrentBranch))
			repository.CurrentBranch = "main";
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
}

public record PublishEnvironment
{
	[YamlIgnore]
	public string Name { get; set; } = string.Empty;

	[YamlMember(Alias = "uri")]
	public string Uri { get; set; } = string.Empty;

	[YamlMember(Alias = "path_prefix")]
	public string? PathPrefix { get; set; } = string.Empty;

	[YamlMember(Alias = "allow_indexing")]
	public bool AllowIndexing { get; set; }
}
