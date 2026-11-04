// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Nullean.Argh;
using static Elastic.Documentation.Aspire.ResourceNames;

// Extract global doc-builder flags before argh routing so they can be forwarded
// to docs-builder sub-process invocations (--log-level, --config-source, etc.).
AspireHost.GlobalArguments = AspireHost.ExtractGlobalArgs(ref args);

var app = new ArghApp();
app.MapRoot(AspireHost.Run);
return await app.RunAsync(args);

// ── Aspire host command ───────────────────────────────────────────────────────────────────────────

internal static class AspireHost
{
	internal static string[] GlobalArguments = [];

	/// <summary>
	/// Starts the Elastic documentation Aspire AppHost.
	/// </summary>
	/// <param name="assumeCloned">
	///   Skip cloning; assume repositories are already present on disk.
	///   Defaults to <c>true</c>. Pass <c>--no-assume-cloned</c> to force a fresh clone.
	/// </param>
	/// <param name="assumeBuild">
	///   Skip the build step when the stamp matches. When omitted the assembler applies an
	///   environment-aware default (skip locally, always build on CI).
	///   Pass <c>--assume-build</c> to force skip, <c>--no-assume-build</c> to force rebuild.
	/// </param>
	[NoOptionsInjection]
	internal static async Task Run(bool? assumeCloned = null, bool? assumeBuild = null, CancellationToken ct = default)
	{
		var builder = DistributedApplication.CreateBuilder();

		var llmUrl = builder.AddParameter("LlmGatewayUrl", secret: true);
		var llmServiceAccountPath = builder.AddParameter("LlmGatewayServiceAccountPath", secret: true);

		var elasticsearchUrl = builder.AddParameter("ElasticsearchUrl", secret: true);
		var elasticsearchApiKey = builder.AddParameter("ElasticsearchApiKey", secret: true);

		var cloneAll = builder.AddProject<Projects.docs_builder>(AssemblerClone);
		// default-on: reuse existing clones unless the caller explicitly opts out
		string[] cloneArgs = (assumeCloned ?? true) ? ["--assume-cloned"] : [];
		cloneAll = cloneAll.WithArgs(["assembler", "clone", .. GlobalArguments, .. cloneArgs]);

		var buildAll = builder.AddProject<Projects.docs_builder>(AssemblerBuild);
		// forward an explicit choice to docs-builder; omit when null so docs-builder applies its own default
		string[] buildArgs = assumeBuild switch
		{
			true => ["--assume-build"],
			false => ["--no-assume-build"],
			null => []
		};
		buildAll = buildAll
			.WithArgs(["assembler", "build", .. GlobalArguments, .. buildArgs])
			.WaitForCompletion(cloneAll)
			.WithParentRelationship(cloneAll);

		var elasticsearchRemote = builder.AddExternalService(ElasticsearchRemote, elasticsearchUrl);

		// Read ENVIRONMENT and DOCS_BUILD_TYPE from the host process (injected by CI or set locally).
		// Index name pattern: docs-{type}.semantic-{env}-latest
		var rawEnvironment = Environment.GetEnvironmentVariable("ENVIRONMENT");
		var serviceEnvironment = string.IsNullOrWhiteSpace(rawEnvironment) ? "prod" : rawEnvironment;
		var rawBuildType = Environment.GetEnvironmentVariable("DOCS_BUILD_TYPE");
		var buildType = string.IsNullOrWhiteSpace(rawBuildType) ? "assembler" : rawBuildType;

		_ = builder
			.AddProject<Projects.Elastic_Documentation_Api>(Api, launchProfileName: "http")
			.WithArgs(GlobalArguments)
			.WithEnvironment("ENVIRONMENT", serviceEnvironment)
			.WithEnvironment("DOCS_BUILD_TYPE", buildType)
			.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
			.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath)
			.WithHttpHealthCheck("/docs/_api/health")
			.WithReference(elasticsearchRemote)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
			.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey);

		_ = builder
			.AddProject<Projects.Elastic_Documentation_Mcp_Remote>(RemoteMcp)
			.WithArgs(GlobalArguments)
			.WithEnvironment("ENVIRONMENT", serviceEnvironment)
			.WithEnvironment("DOCS_BUILD_TYPE", buildType)
			.WithHttpHealthCheck("/docs/_mcp/health")
			.WithReference(elasticsearchRemote)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
			.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey);

		_ = builder
			.AddProject<Projects.docs_builder>(ElasticsearchIngest)
			.WithArgs(["assembler", "index", .. GlobalArguments])
			.WaitForCompletion(cloneAll)
			.WithExplicitStart()
			.WithReference(elasticsearchRemote)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
			.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey)
			.WithParentRelationship(elasticsearchRemote);

		_ = builder
			.AddProject<Projects.docs_builder>(AssemblerServe)
			.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
			.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath)
			.WithHttpEndpoint(port: 4000, isProxied: false)
			.WithArgs(["assembler", "serve", .. GlobalArguments])
			.WithHttpHealthCheck("/", 200)
			.WaitForCompletion(buildAll)
			.WithParentRelationship(cloneAll)
			.WithReference(elasticsearchRemote)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
			.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey);

		await builder.Build().RunAsync(ct);
	}

	/// <summary>
	/// Extracts global doc-builder flags (--log-level, --config-source,
	/// --skip-private-repositories / --no-skip-private-repositories) from
	/// <paramref name="args"/> in-place, returning them for forwarding to
	/// docs-builder sub-processes.
	/// <para>
	/// <c>--skip-private-repositories</c> defaults to <c>true</c>: when neither
	/// the flag nor its <c>--no-</c> counterpart is present the flag is injected
	/// automatically. Pass <c>--no-skip-private-repositories</c> to opt out.
	/// </para>
	/// </summary>
	internal static string[] ExtractGlobalArgs(ref string[] args)
	{
		var global = new List<string>();
		var remaining = new List<string>();
		bool? skipPrivateRepositories = null;
		for (var i = 0; i < args.Length; i++)
		{
			if (args[i] == "--log-level" && i + 1 < args.Length)
			{
				global.Add("--log-level");
				global.Add(args[++i]);
			}
			else if (args[i] is "--config-source" or "--configuration-source" or "-c" && i + 1 < args.Length)
			{
				global.Add("--config-source");
				global.Add(args[++i]);
			}
			else if (args[i] == "--skip-private-repositories")
				skipPrivateRepositories = true;
			else if (args[i] == "--no-skip-private-repositories")
				skipPrivateRepositories = false;
			else
				remaining.Add(args[i]);
		}
		// default-on: skip private repos unless the caller explicitly opted out
		if (skipPrivateRepositories ?? true)
			global.Add("--skip-private-repositories");
		args = [.. remaining];
		return [.. global];
	}
}
