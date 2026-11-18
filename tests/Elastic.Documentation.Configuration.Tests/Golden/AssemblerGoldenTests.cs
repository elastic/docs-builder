// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Publishing;
using Nullean.Snapshots;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What <c>assembler.yml</c> loads into today, with and without skipping private repositories.</summary>
public partial class AssemblerGoldenTests
{
	private static string Load(string yaml, bool skipPrivateRepositories = false) =>
		DefaultOrigin().Replace(
			GoldenConfig.Outcome(
				GoldenConfig.Assembler,
				yaml,
				p => AssemblyLoader.Create(p),
				null,
				skipPrivateRepositories,
				"AvailableRepositories",
				"PrivateRepositories",
				"AllPrivateRepositoryNames"
			),
			"Origin: <default origin>"
		);

	// the origin a repository without one gets depends on GITHUB_ACTIONS and GITHUB_TOKEN
	[GeneratedRegex("Origin: \"(?:git@github\\.com:elastic/|https://(?:oauth2:[^@]*@)?github\\.com/elastic/)[^\"]*\\.git\"", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex DefaultOrigin();

	private static string BaselineSection(Func<AssemblyConfiguration, object?> select, bool skipPrivateRepositories = false) =>
		DefaultOrigin().Replace(ModelDump.ToText(select(GoldenConfig.LoadAssembler(skipPrivateRepositories))), "Origin: <default origin>");

	[Test]
	public void Baseline_Environments_MatchSnapshot() => BaselineSection(c => c.Environments).Should().MatchSnapshot();

	[Test]
	public void Baseline_NarrativeAndSharedConfiguration_MatchSnapshot() =>
		(BaselineSection(c => c.Narrative) + BaselineSection(c => c.SharedConfigurations)).Should().MatchSnapshot();

	[Test]
	public void Baseline_References_MatchSnapshot() => BaselineSection(c => c.ReferenceRepositories).Should().MatchSnapshot();

	[Test]
	public void Baseline_DerivedRepositorySets_MatchSnapshot()
	{
		var sb = new StringBuilder();
		foreach (var skip in new[] { false, true })
		{
			var config = GoldenConfig.LoadAssembler(skip);
			_ = sb.Append("== skipPrivateRepositories: ").AppendLine(skip.ToString());
			_ = sb.AppendLine("references:");
			foreach (var name in config.ReferenceRepositories.Keys)
				_ = sb.Append("  ").AppendLine(name);
			AppendNames(sb, "available", config.AvailableRepositories.Keys);
			AppendNames(sb, "private (not skipped)", config.PrivateRepositories.Keys);
			AppendNames(sb, "all private names", config.AllPrivateRepositoryNames.Order(StringComparer.Ordinal));
			var docsBuilder = config.ReferenceRepositories["docs-builder"];
			_ = sb
				.Append("docs-builder: skip=")
				.Append(docsBuilder.Skip)
				.Append(" path=")
				.AppendLine(docsBuilder.Path is null ? "null" : "set");
		}
		sb.ToString().Should().MatchSnapshot(options: new SnapshotOptions().Scrub(PathRegex(), "{path}"));
	}

	[GeneratedRegex("path=set", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex PathRegex();

	private static void AppendNames(StringBuilder sb, string title, IEnumerable<string> names)
	{
		_ = sb.Append(title).AppendLine(":");
		foreach (var name in names)
			_ = sb.Append("  ").AppendLine(name);
	}

	[Test]
	public void EmptyFile_Loaded_Outcome() =>
		Load("")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.Assembler.AssemblyConfigurationDto'.
				"""
			);

	[Test]
	public void OnlyComments_Loaded_Outcome() =>
		Load("# nothing here\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.Assembler.AssemblyConfigurationDto'.
				"""
			);

	[Test]
	public void UnknownTopLevelKey_Loaded_Outcome() =>
		Load("bogus: 1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		YamlException: {config-runtime}/assembler.yml: (Lin: 0, Col: 0, Chr: 0) - (Lin: 0, Col: 7, Chr: 7): (1,1) Unknown key 'bogus'. Valid keys: environments, narrative, references, shared_configuration.
		"""
			);

	[Test]
	public void EmptySections_Loaded_Outcome() =>
		Load("environments: {}\nreferences: {}\nnarrative: {}\nshared_configuration: {}\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories: {}
			  Environments: {}
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void MinimalEnvironment_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories: {}
			  Environments:
			    prod <PublishEnvironment>:
			      Name: "prod"
			      Uri: "https://example.com"
			      PathPrefix: ""
			      AllowIndexing: False
			      ContentSource: Next
			      GoogleTagManager <GoogleTagManager>:
			        Enabled: False
			        Id: null
			        Auth: null
			        Preview: null
			        CookiesWin: null
			      Optimizely <Optimizely>:
			        Enabled: False
			        Id: null
			      FeatureFlags: {}
			      WebsiteSearchScriptUrl: null
			      WafIntegrationUrl: null
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void EnvironmentWithoutBody_Loaded_Outcome() =>
		Load("environments:\n  prod:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		NullReferenceException: Object reference not set to an instance of an object.
		"""
			);

	[Test]
	public void EnvironmentUnknownKey_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    colour: red\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 3, Col: 4, Chr: 55) - (Lin: 3, Col: 12, Chr: 63): (4,5) Unknown key 'colour' in $.environments.prod. Valid keys: allow_indexing, content_source, feature_flags, google_tag_manager, optimizely, path_prefix, uri, waf_integration_url, website_search_url.
			"""
			);

	[Test]
	public void EnvironmentWithEveryKey_Loaded_Outcome() =>
		Load(
			"""
			environments:
			  prod:
			    uri: https://example.com
			    path_prefix: docs
			    allow_indexing: true
			    content_source: edge
			    google_tag_manager:
			      enabled: true
			      id: GTM-TEST
			      auth: a
			      preview: p
			      cookies_win: c
			    optimizely:
			      enabled: true
			      id: "123"
			    feature_flags:
			      SOME_FLAG: true
			      other-flag: false
			    website_search_url: https://example.com/search.js
			    waf_integration_url: https://example.com/waf.js
			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories: {}
			  Environments:
			    prod <PublishEnvironment>:
			      Name: "prod"
			      Uri: "https://example.com"
			      PathPrefix: "docs"
			      AllowIndexing: True
			      ContentSource: Edge
			      GoogleTagManager <GoogleTagManager>:
			        Enabled: True
			        Id: "GTM-TEST"
			        Auth: "a"
			        Preview: "p"
			        CookiesWin: "c"
			      Optimizely <Optimizely>:
			        Enabled: True
			        Id: "123"
			      FeatureFlags:
			        SOME_FLAG: True
			        other-flag: False
			      WebsiteSearchScriptUrl: "https://example.com/search.js"
			      WafIntegrationUrl: "https://example.com/waf.js"
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void EnvironmentContentSourceNotAnEnumValue_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    content_source: sideways\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 3, Col: 20, Chr: 71) - (Lin: 3, Col: 28, Chr: 79): Invalid enum scalar 'sideways'.
			"""
			);

	[Test]
	public void EnvironmentContentSourceInOtherCase_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    content_source: Current\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories: {}
			  Environments:
			    prod <PublishEnvironment>:
			      Name: "prod"
			      Uri: "https://example.com"
			      PathPrefix: ""
			      AllowIndexing: False
			      ContentSource: Current
			      GoogleTagManager <GoogleTagManager>:
			        Enabled: False
			        Id: null
			        Auth: null
			        Preview: null
			        CookiesWin: null
			      Optimizely <Optimizely>:
			        Enabled: False
			        Id: null
			      FeatureFlags: {}
			      WebsiteSearchScriptUrl: null
			      WafIntegrationUrl: null
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void EnvironmentAllowIndexingNotABoolean_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    allow_indexing: maybe\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 3, Col: 20, Chr: 71) - (Lin: 3, Col: 25, Chr: 76): Invalid boolean scalar 'maybe'.
			"""
			);

	[Test]
	public void EnvironmentFeatureFlagNotABoolean_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    feature_flags:\n      FLAG: sometimes\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/assembler.yml: (Lin: 4, Col: 12, Chr: 82) - (Lin: 4, Col: 21, Chr: 91): Invalid boolean scalar 'sometimes'.
				"""
			);

	[Test]
	public void EnvironmentBlankUri_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <AssemblyConfiguration>:
		  Narrative <NarrativeRepository>:
		    Origin: <default origin>
		    CheckoutStrategy: Partial
		    Skip: False
		    Path: null
		    GitReferenceCurrent: "main"
		    GitReferenceNext: "main"
		    GitReferenceEdge: "main"
		    SparsePaths:
		      - "docs"
		    Private: False
		    CloneTimeout: null
		    Name: "docs-content"
		  ReferenceRepositories: {}
		  Environments:
		    prod <PublishEnvironment>:
		      Name: "prod"
		      Uri: ""
		      PathPrefix: ""
		      AllowIndexing: False
		      ContentSource: Next
		      GoogleTagManager <GoogleTagManager>:
		        Enabled: False
		        Id: null
		        Auth: null
		        Preview: null
		        CookiesWin: null
		      Optimizely <Optimizely>:
		        Enabled: False
		        Id: null
		      FeatureFlags: {}
		      WebsiteSearchScriptUrl: null
		      WafIntegrationUrl: null
		  SharedConfigurations: {}
		"""
			);

	[Test]
	public void EnvironmentUriThatIsNotAUri_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: 'not a uri'\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories: {}
			  Environments:
			    prod <PublishEnvironment>:
			      Name: "prod"
			      Uri: "not a uri"
			      PathPrefix: ""
			      AllowIndexing: False
			      ContentSource: Next
			      GoogleTagManager <GoogleTagManager>:
			        Enabled: False
			        Id: null
			        Auth: null
			        Preview: null
			        CookiesWin: null
			      Optimizely <Optimizely>:
			        Enabled: False
			        Id: null
			      FeatureFlags: {}
			      WebsiteSearchScriptUrl: null
			      WafIntegrationUrl: null
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void EnvironmentPathPrefixNull_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    path_prefix: ~\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories: {}
			  Environments:
			    prod <PublishEnvironment>:
			      Name: "prod"
			      Uri: "https://example.com"
			      PathPrefix: null
			      AllowIndexing: False
			      ContentSource: Next
			      GoogleTagManager <GoogleTagManager>:
			        Enabled: False
			        Id: null
			        Auth: null
			        Preview: null
			        CookiesWin: null
			      Optimizely <Optimizely>:
			        Enabled: False
			        Id: null
			      FeatureFlags: {}
			      WebsiteSearchScriptUrl: null
			      WafIntegrationUrl: null
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void EnvironmentPathPrefixFromUnsetVariable_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    path_prefix: ${GOLDEN_TEST_SURELY_UNSET_VARIABLE}\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: <default origin>
				    CheckoutStrategy: Partial
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "main"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: False
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories: {}
				  Environments:
				    prod <PublishEnvironment>:
				      Name: "prod"
				      Uri: "https://example.com"
				      PathPrefix: "${GOLDEN_TEST_SURELY_UNSET_VARIABLE}"
				      AllowIndexing: False
				      ContentSource: Next
				      GoogleTagManager <GoogleTagManager>:
				        Enabled: False
				        Id: null
				        Auth: null
				        Preview: null
				        CookiesWin: null
				      Optimizely <Optimizely>:
				        Enabled: False
				        Id: null
				      FeatureFlags: {}
				      WebsiteSearchScriptUrl: null
				      WafIntegrationUrl: null
				  SharedConfigurations: {}
				"""
			);

	[Test]
	public void GoogleTagManagerEnabledWithoutId_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    google_tag_manager:\n      enabled: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: <default origin>
				    CheckoutStrategy: Partial
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "main"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: False
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories: {}
				  Environments:
				    prod <PublishEnvironment>:
				      Name: "prod"
				      Uri: "https://example.com"
				      PathPrefix: ""
				      AllowIndexing: False
				      ContentSource: Next
				      GoogleTagManager <GoogleTagManager>:
				        Enabled: True
				        Id: null
				        Auth: null
				        Preview: null
				        CookiesWin: null
				      Optimizely <Optimizely>:
				        Enabled: False
				        Id: null
				      FeatureFlags: {}
				      WebsiteSearchScriptUrl: null
				      WafIntegrationUrl: null
				  SharedConfigurations: {}
				"""
			);

	[Test]
	public void GoogleTagManagerIdBeforeEnabled_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    google_tag_manager:\n      id: GTM-1\n      enabled: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: <default origin>
				    CheckoutStrategy: Partial
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "main"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: False
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories: {}
				  Environments:
				    prod <PublishEnvironment>:
				      Name: "prod"
				      Uri: "https://example.com"
				      PathPrefix: ""
				      AllowIndexing: False
				      ContentSource: Next
				      GoogleTagManager <GoogleTagManager>:
				        Enabled: True
				        Id: "GTM-1"
				        Auth: null
				        Preview: null
				        CookiesWin: null
				      Optimizely <Optimizely>:
				        Enabled: False
				        Id: null
				      FeatureFlags: {}
				      WebsiteSearchScriptUrl: null
				      WafIntegrationUrl: null
				  SharedConfigurations: {}
				"""
			);

	[Test]
	public void GoogleTagManagerEnabledBeforeBlankId_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://example.com\n    google_tag_manager:\n      enabled: true\n      id: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/assembler.yml: (Lin: 6, Col: 0, Chr: 108) - (Lin: 6, Col: 0, Chr: 108): Id is required when Enabled is true.
				inner ArgumentException: Id is required when Enabled is true.
				"""
			);

	[Test]
	public void DuplicateEnvironment_Loaded_Outcome() =>
		Load("environments:\n  prod:\n    uri: https://a.example.com\n  prod:\n    uri: https://b.example.com\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/assembler.yml: (Lin: 5, Col: 0, Chr: 92) - (Lin: 5, Col: 0, Chr: 92): Duplicate mapping key 'prod'.
				"""
			);

	[Test]
	public void EnvironmentsAsList_Loaded_Outcome() =>
		Load("environments:\n  - prod\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		YamlException: {config-runtime}/assembler.yml: (Lin: 1, Col: 2, Chr: 16) - (Lin: 1, Col: 2, Chr: 16): Expected a StartMapping token but found 'StartSequence'.
		"""
			);

	[Test]
	public void ReferenceWithoutBody_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <AssemblyConfiguration>:
		  Narrative <NarrativeRepository>:
		    Origin: <default origin>
		    CheckoutStrategy: Partial
		    Skip: False
		    Path: null
		    GitReferenceCurrent: "main"
		    GitReferenceNext: "main"
		    GitReferenceEdge: "main"
		    SparsePaths:
		      - "docs"
		    Private: False
		    CloneTimeout: null
		    Name: "docs-content"
		  ReferenceRepositories:
		    some-repo <Repository>:
		      Name: "some-repo"
		      Origin: <default origin>
		      CheckoutStrategy: Partial
		      Skip: False
		      Path: null
		      GitReferenceCurrent: "main"
		      GitReferenceNext: "main"
		      GitReferenceEdge: "main"
		      SparsePaths:
		        - "docs"
		      Private: False
		      CloneTimeout: null
		  Environments: {}
		  SharedConfigurations: {}
		"""
			);

	// `path` is left out: the loader drops it when CI is set, so it reads differently on a build server
	[Test]
	public void ReferenceWithEveryKey_Loaded_Outcome() =>
		Load(
			"""
			references:
			  some-repo:
			    repo: git@github.com:example/some-repo.git
			    checkout_strategy: full
			    skip: true
			    current: '9.1'
			    next: '9.2'
			    edge: main
			    sparse_paths: ["docs", "config"]
			    private: false
			    clone_timeout: 30s
			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: <default origin>
				    CheckoutStrategy: Partial
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "main"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: False
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories:
				    some-repo <Repository>:
				      Name: "some-repo"
				      Origin: "git@github.com:example/some-repo.git"
				      CheckoutStrategy: Full
				      Skip: True
				      Path: null
				      GitReferenceCurrent: "9.1"
				      GitReferenceNext: "9.2"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				        - "config"
				      Private: False
				      CloneTimeout: 00:00:30
				  Environments: {}
				  SharedConfigurations: {}
				"""
			);

	[Test]
	public void ReferenceUnknownKey_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n    colour: red\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		YamlException: {config-runtime}/assembler.yml: (Lin: 2, Col: 4, Chr: 29) - (Lin: 2, Col: 12, Chr: 37): (3,5) Unknown key 'colour' in $.references.some-repo. Valid keys: checkout_strategy, clone_timeout, current, edge, next, path, private, repo, skip, sparse_paths.
		"""
			);

	[Test]
	public void ReferenceCheckoutStrategyNotAnEnumValue_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n    checkout_strategy: shallow\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 2, Col: 23, Chr: 48) - (Lin: 2, Col: 30, Chr: 55): Invalid enum scalar 'shallow'.
			"""
			);

	[Test]
	public void ReferenceBlankBranches_Loaded_FallBackToMain() =>
		Load("references:\n  some-repo:\n    current: ''\n    next: ~\n    edge: '  '\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories:
			    some-repo <Repository>:
			      Name: "some-repo"
			      Origin: <default origin>
			      CheckoutStrategy: Partial
			      Skip: False
			      Path: null
			      GitReferenceCurrent: "main"
			      GitReferenceNext: "main"
			      GitReferenceEdge: "  "
			      SparsePaths:
			        - "docs"
			      Private: False
			      CloneTimeout: null
			  Environments: {}
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void ReferenceNumericBranches_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n    current: 9.0\n    next: 10\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories:
			    some-repo <Repository>:
			      Name: "some-repo"
			      Origin: <default origin>
			      CheckoutStrategy: Partial
			      Skip: False
			      Path: null
			      GitReferenceCurrent: "9.0"
			      GitReferenceNext: "10"
			      GitReferenceEdge: "main"
			      SparsePaths:
			        - "docs"
			      Private: False
			      CloneTimeout: null
			  Environments: {}
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void ReferenceSparsePathsAsScalar_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n    sparse_paths: docs\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 2, Col: 18, Chr: 43) - (Lin: 2, Col: 22, Chr: 47): Expected a StartSequence token but found 'Scalar'.
			"""
			);

	[Test]
	public void ReferenceEmptySparsePaths_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n    sparse_paths: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories:
			    some-repo <Repository>:
			      Name: "some-repo"
			      Origin: <default origin>
			      CheckoutStrategy: Partial
			      Skip: False
			      Path: null
			      GitReferenceCurrent: "main"
			      GitReferenceNext: "main"
			      GitReferenceEdge: "main"
			      SparsePaths: []
			      Private: False
			      CloneTimeout: null
			  Environments: {}
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void ReferenceCloneTimeouts_Loaded_Outcome() =>
		Load("references:\n  a:\n    clone_timeout: 90s\n  b:\n    clone_timeout: 15m\n  c:\n    clone_timeout: 2h\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/assembler.yml: (Lin: 6, Col: 19, Chr: 92) - (Lin: 6, Col: 21, Chr: 94): Invalid duration '2h'. Expected a positive integer followed by 's' (seconds) or 'm' (minutes), e.g. '30s' or '15m'.
				"""
			);

	[Test]
	public void ReferenceCloneTimeoutNotADuration_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n    clone_timeout: banana\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 2, Col: 19, Chr: 44) - (Lin: 2, Col: 25, Chr: 50): Invalid duration 'banana'. Expected a positive integer followed by 's' (seconds) or 'm' (minutes), e.g. '30s' or '15m'.
			"""
			);

	[Test]
	public void ReferenceCloneTimeoutZeroAndNegative_Loaded_Outcome() =>
		Load("references:\n  a:\n    clone_timeout: 0s\n  b:\n    clone_timeout: -5m\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 2, Col: 19, Chr: 36) - (Lin: 2, Col: 21, Chr: 38): Invalid duration '0s'. Expected a positive integer followed by 's' (seconds) or 'm' (minutes), e.g. '30s' or '15m'.
			"""
			);

	[Test]
	public void ReferenceSkipNotABoolean_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n    skip: later\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 2, Col: 10, Chr: 35) - (Lin: 2, Col: 15, Chr: 40): Invalid boolean scalar 'later'.
			"""
			);

	[Test]
	public void ReferenceRepositoryNameDifferingByCase_Loaded_Outcome() =>
		Load("references:\n  Some-Repo:\n  some-repo:\n    skip: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: <default origin>
				    CheckoutStrategy: Partial
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "main"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: False
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories:
				    Some-Repo <Repository>:
				      Name: "Some-Repo"
				      Origin: <default origin>
				      CheckoutStrategy: Partial
				      Skip: False
				      Path: null
				      GitReferenceCurrent: "main"
				      GitReferenceNext: "main"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: False
				      CloneTimeout: null
				    some-repo <Repository>:
				      Name: "some-repo"
				      Origin: <default origin>
				      CheckoutStrategy: Partial
				      Skip: True
				      Path: null
				      GitReferenceCurrent: "main"
				      GitReferenceNext: "main"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: False
				      CloneTimeout: null
				  Environments: {}
				  SharedConfigurations: {}
				"""
			);

	[Test]
	public void DuplicateReference_Loaded_Outcome() =>
		Load("references:\n  some-repo:\n  some-repo:\n    skip: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 4, Col: 0, Chr: 53) - (Lin: 4, Col: 0, Chr: 53): Duplicate mapping key 'some-repo'.
			"""
			);

	[Test]
	public void ReferencesAsList_Loaded_Outcome() =>
		Load("references:\n  - some-repo\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		YamlException: {config-runtime}/assembler.yml: (Lin: 1, Col: 2, Chr: 14) - (Lin: 1, Col: 2, Chr: 14): Expected a StartMapping token but found 'StartSequence'.
		"""
			);

	[Test]
	public void ReferenceAnchorAndAlias_Loaded_Outcome() =>
		Load("shared_configuration:\n  s: &s\n    current: '9.0'\n    next: '9.1'\nreferences:\n  a: *s\n  b: *s\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: <default origin>
				    CheckoutStrategy: Partial
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "main"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: False
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories:
				    a <Repository>:
				      Name: "a"
				      Origin: <default origin>
				      CheckoutStrategy: Partial
				      Skip: False
				      Path: null
				      GitReferenceCurrent: "9.0"
				      GitReferenceNext: "9.1"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: False
				      CloneTimeout: null
				    b <Repository>:
				      Name: "b"
				      Origin: <default origin>
				      CheckoutStrategy: Partial
				      Skip: False
				      Path: null
				      GitReferenceCurrent: "9.0"
				      GitReferenceNext: "9.1"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: False
				      CloneTimeout: null
				  Environments: {}
				  SharedConfigurations:
				    s <Repository>:
				      Name: ""
				      Origin: ""
				      CheckoutStrategy: Partial
				      Skip: False
				      Path: null
				      GitReferenceCurrent: "9.0"
				      GitReferenceNext: "9.1"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: False
				      CloneTimeout: null
				"""
			);

	[Test]
	public void PrivateReferences_Loaded_KeptWhenNotSkipping() =>
		Load("references:\n  open-repo:\n  closed-repo:\n    private: true\n  closed-skipped:\n    private: true\n    skip: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: <default origin>
				    CheckoutStrategy: Partial
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "main"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: False
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories:
				    open-repo <Repository>:
				      Name: "open-repo"
				      Origin: <default origin>
				      CheckoutStrategy: Partial
				      Skip: False
				      Path: null
				      GitReferenceCurrent: "main"
				      GitReferenceNext: "main"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: False
				      CloneTimeout: null
				    private-repo_1 <Repository>:
				      Name: "private-repo_1"
				      Origin: <default origin>
				      CheckoutStrategy: Partial
				      Skip: False
				      Path: null
				      GitReferenceCurrent: "main"
				      GitReferenceNext: "main"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: True
				      CloneTimeout: null
				    private-repo_2 <Repository>:
				      Name: "private-repo_2"
				      Origin: <default origin>
				      CheckoutStrategy: Partial
				      Skip: True
				      Path: null
				      GitReferenceCurrent: "main"
				      GitReferenceNext: "main"
				      GitReferenceEdge: "main"
				      SparsePaths:
				        - "docs"
				      Private: True
				      CloneTimeout: null
				  Environments: {}
				  SharedConfigurations: {}
				"""
			);

	[Test]
	public void PrivateReferences_Loaded_RemovedWhenSkipping() =>
		Load(
			"references:\n  open-repo:\n  closed-repo:\n    private: true\n  closed-skipped:\n    private: true\n    skip: true\n",
			skipPrivateRepositories: true
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories:
			    open-repo <Repository>:
			      Name: "open-repo"
			      Origin: <default origin>
			      CheckoutStrategy: Partial
			      Skip: False
			      Path: null
			      GitReferenceCurrent: "main"
			      GitReferenceNext: "main"
			      GitReferenceEdge: "main"
			      SparsePaths:
			        - "docs"
			      Private: False
			      CloneTimeout: null
			  Environments: {}
			  SharedConfigurations: {}
			"""
			);

	[Test]
	public void NarrativeWithEveryKey_Loaded_Outcome() =>
		Load("narrative:\n  repo: git@github.com:example/docs-content.git\n  checkout_strategy: full\n  current: '9.0'\n  private: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <AssemblyConfiguration>:
				  Narrative <NarrativeRepository>:
				    Origin: "git@github.com:example/docs-content.git"
				    CheckoutStrategy: Full
				    Skip: False
				    Path: null
				    GitReferenceCurrent: "9.0"
				    GitReferenceNext: "main"
				    GitReferenceEdge: "main"
				    SparsePaths:
				      - "docs"
				    Private: True
				    CloneTimeout: null
				    Name: "docs-content"
				  ReferenceRepositories: {}
				  Environments: {}
				  SharedConfigurations: {}
				"""
			);

	[Test]
	public void NarrativeNameKey_Loaded_Outcome() =>
		Load("narrative:\n  name: other\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <AssemblyConfiguration>:
		  Narrative <NarrativeRepository>:
		    Origin: <default origin>
		    CheckoutStrategy: Partial
		    Skip: False
		    Path: null
		    GitReferenceCurrent: "main"
		    GitReferenceNext: "main"
		    GitReferenceEdge: "main"
		    SparsePaths:
		      - "docs"
		    Private: False
		    CloneTimeout: null
		    Name: "docs-content"
		  ReferenceRepositories: {}
		  Environments: {}
		  SharedConfigurations: {}
		"""
			);

	[Test]
	public void NarrativeAsList_Loaded_Outcome() =>
		Load("narrative:\n  - a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		YamlException: {config-runtime}/assembler.yml: (Lin: 1, Col: 2, Chr: 13) - (Lin: 1, Col: 2, Chr: 13): Expected a StartMapping token but found 'StartSequence'.
		"""
			);

	[Test]
	public void NarrativeWithoutBody_Loaded_Outcome() =>
		Load("narrative:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		model <AssemblyConfiguration>:
		  Narrative <NarrativeRepository>:
		    Origin: <default origin>
		    CheckoutStrategy: Partial
		    Skip: False
		    Path: null
		    GitReferenceCurrent: "main"
		    GitReferenceNext: "main"
		    GitReferenceEdge: "main"
		    SparsePaths:
		      - "docs"
		    Private: False
		    CloneTimeout: null
		    Name: "docs-content"
		  ReferenceRepositories: {}
		  Environments: {}
		  SharedConfigurations: {}
		"""
			);

	[Test]
	public void SharedConfigurationUnknownKey_Loaded_Outcome() =>
		Load("shared_configuration:\n  s:\n    colour: red\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			YamlException: {config-runtime}/assembler.yml: (Lin: 2, Col: 4, Chr: 31) - (Lin: 2, Col: 12, Chr: 39): (3,5) Unknown key 'colour' in $.shared_configuration.s. Valid keys: checkout_strategy, clone_timeout, current, edge, next, path, private, repo, skip, sparse_paths.
			"""
			);

	[Test]
	public void DocsBuilderReferenceWhenSkippingPrivate_Loaded_PointsAtLocalDocs() =>
		Load("references:\n  docs-builder:\n    skip: true\n", skipPrivateRepositories: true)
			.Should()
			.MatchInlineSnapshot(
				"""
			model <AssemblyConfiguration>:
			  Narrative <NarrativeRepository>:
			    Origin: <default origin>
			    CheckoutStrategy: Partial
			    Skip: False
			    Path: null
			    GitReferenceCurrent: "main"
			    GitReferenceNext: "main"
			    GitReferenceEdge: "main"
			    SparsePaths:
			      - "docs"
			    Private: False
			    CloneTimeout: null
			    Name: "docs-content"
			  ReferenceRepositories:
			    docs-builder <Repository>:
			      Name: "docs-builder"
			      Origin: <default origin>
			      CheckoutStrategy: Partial
			      Skip: False
			      Path: "{repo}/docs"
			      GitReferenceCurrent: "main"
			      GitReferenceNext: "main"
			      GitReferenceEdge: "main"
			      SparsePaths:
			        - "docs"
			      Private: False
			      CloneTimeout: null
			  Environments: {}
			  SharedConfigurations: {}
			"""
			);
}
