// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Generator.Equals;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Publishing;

/// <summary>
/// The assembled site's sources and targets from <c>assembler.yml</c>: the narrative repository, the referenced
/// repositories with their branch per content source, and the publish environments. Loaded once per process by
/// <c>AssemblyLoader</c>, with repository defaults applied and private repositories filtered when requested.
/// </summary>
[Equatable]
public sealed partial record AssemblyConfiguration
{
	[DefaultEquality]
	public required NarrativeRepository Narrative { get; init; }

	/// <summary>The <c>references</c> repositories, keyed by name, after private-repository filtering.</summary>
	[UnorderedEquality]
	public required IReadOnlyDictionary<string, Repository> ReferenceRepositories { get; init; }

	/// <summary>Every repository that publishes docs: references not marked <c>skip</c>, plus the narrative.</summary>
	[UnorderedEquality]
	public required IReadOnlyDictionary<string, Repository> AvailableRepositories { get; init; }

	/// <summary>Private repositories that publish docs (<c>private: true</c> and not <c>skip</c>).</summary>
	[UnorderedEquality]
	public required IReadOnlyDictionary<string, Repository> PrivateRepositories { get; init; }

	/// <summary>
	/// All repos marked <c>private: true</c>, regardless of <c>skip</c>. Skip means "does not publish docs", not
	/// "is not private"; this set drives render-time link visibility, so entries like a private, skipped team
	/// repository still have their links hidden.
	/// </summary>
	[SetEquality]
	public required IReadOnlySet<string> AllPrivateRepositoryNames { get; init; }

	[UnorderedEquality]
	public required IReadOnlyDictionary<string, PublishEnvironment> Environments { get; init; }

	[UnorderedEquality]
	public required IReadOnlyDictionary<string, Repository> SharedConfigurations { get; init; }

	/// <summary>Which content sources a push of <c>branchOrTag</c> to a repository publishes to.</summary>
	public record ContentSourceMatch(ContentSource? Current, ContentSource? Next, ContentSource? Edge, bool Speculative);
}

/// <summary>A repository the assembler clones, with the branch it publishes per content source.</summary>
[Equatable]
public partial record Repository
{
	public string Name { get; init; } = string.Empty;

	/// <summary>The git remote; defaults to the elastic GitHub organization when <c>repo</c> is omitted.</summary>
	public string Origin { get; init; } = string.Empty;

	public CheckoutStrategy CheckoutStrategy { get; init; } = CheckoutStrategy.Partial;

	/// <summary>Whether the repository does not publish docs.</summary>
	public bool Skip { get; init; }

	/// <summary> Allows you to override the path to the repository, but only during local builds. </summary>
	public string? Path { get; init; }

	public string GitReferenceCurrent { get; init; } = "main";
	public string GitReferenceNext { get; init; } = "main";
	public string GitReferenceEdge { get; init; } = "main";

	[OrderedEquality]
	public string[] SparsePaths { get; init; } = ["docs"];

	public bool Private { get; init; }

	/// <summary>
	/// Per-attempt timeout for network git operations (fetch, pull) in CI.
	/// When omitted the global default applies (10 minutes in CI, unbounded locally).
	/// </summary>
	public TimeSpan? CloneTimeout { get; init; }

	public string GetBranch(ContentSource contentSource) => contentSource switch
	{
		ContentSource.Current => GitReferenceCurrent,
		ContentSource.Next => GitReferenceNext,
		ContentSource.Edge => GitReferenceEdge,
		_ => throw new ArgumentException($"The content source {contentSource} is not supported.", nameof(contentSource))
	};
}

/// <summary>The narrative repository (<c>docs-content</c>): the site's own content, always published.</summary>
public sealed record NarrativeRepository : Repository
{
	public static string RepositoryName { get; } = "docs-content";

	public NarrativeRepository() => Name = RepositoryName;
}

public enum CheckoutStrategy
{
	[EnumMember(Value = "partial")]
	Partial,

	[EnumMember(Value = "full")]
	Full
}

/// <summary>Which branch of every repository an environment publishes.</summary>
[EnumExtensions]
public enum ContentSource
{
	[Display(Name = "next")]
	[JsonStringEnumMemberName("next")]
	Next,

	[JsonStringEnumMemberName("current")]
	[Display(Name = "current")]
	Current,

	[JsonStringEnumMemberName("edge")]
	[Display(Name = "edge")]
	Edge
}

/// <summary>A deploy target of the assembled site, from <c>assembler.yml</c>'s <c>environments</c>.</summary>
[Equatable]
public sealed partial record PublishEnvironment
{
	public string Name { get; init; } = string.Empty;
	public string Uri { get; init; } = string.Empty;
	public string? PathPrefix { get; init; } = string.Empty;
	public bool AllowIndexing { get; init; }
	public ContentSource ContentSource { get; init; }
	public GoogleTagManager GoogleTagManager { get; init; } = new();
	public Optimizely Optimizely { get; init; } = new();

	/// <summary>Feature flag overrides for builds targeting this environment, as authored.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, bool> FeatureFlags { get; init; } = new Dictionary<string, bool>();

	public string? WebsiteSearchScriptUrl { get; init; }
}

public sealed record GoogleTagManager
{
	public bool Enabled { get; init; }
	public string? Id { get; init; }
	public string? Auth { get; init; }
	public string? Preview { get; init; }
	public string? CookiesWin { get; init; }
}

public sealed record Optimizely
{
	public bool Enabled { get; init; }
	public string? Id { get; init; }
}
