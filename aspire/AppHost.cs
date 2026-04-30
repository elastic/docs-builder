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
	/// <param name="startElasticsearch">Start a local Elasticsearch container</param>
	/// <param name="assumeCloned">Skip cloning; assume repositories are already present on disk</param>
	/// <param name="assumeBuild">Skip building; assume build output already exists</param>
	/// <param name="skipPrivateRepositories">Skip cloning private repositories</param>
	[NoOptionsInjection]
	internal static async Task Run(
		bool startElasticsearch = false,
		bool assumeCloned = false,
		bool assumeBuild = false,
		bool skipPrivateRepositories = false,
		CancellationToken ct = default)
	{
		var builder = DistributedApplication.CreateBuilder();

		var llmUrl = builder.AddParameter("LlmGatewayUrl", secret: true);
		var llmServiceAccountPath = builder.AddParameter("LlmGatewayServiceAccountPath", secret: true);

		var elasticsearchUrl = builder.AddParameter("DocumentationElasticUrl", secret: true);
		var elasticsearchApiKey = builder.AddParameter("DocumentationElasticApiKey", secret: true);

		var cloneAll = builder.AddProject<Projects.docs_builder>(AssemblerClone);
		string[] cloneArgs = assumeCloned ? ["--assume-cloned"] : [];
		cloneAll = cloneAll.WithArgs(["assembler", "clone", .. GlobalArguments, .. cloneArgs]);

		var buildAll = builder.AddProject<Projects.docs_builder>(AssemblerBuild);
		string[] buildArgs = assumeBuild ? ["--assume-build"] : [];
		buildAll = buildAll
			.WithArgs(["assembler", "build", .. GlobalArguments, .. buildArgs])
			.WaitForCompletion(cloneAll)
			.WithParentRelationship(cloneAll);

		var elasticsearchLocal = builder.AddElasticsearch(ElasticsearchLocal)
			.WithEnvironment("LICENSE", "trial");
		if (!startElasticsearch)
			elasticsearchLocal = elasticsearchLocal.WithExplicitStart();

		var elasticsearchRemote = builder.AddExternalService(ElasticsearchRemote, elasticsearchUrl);

		var api = builder.AddProject<Projects.Elastic_Documentation_Api_App>(Api)
			.WithArgs(GlobalArguments)
			.WithEnvironment("ENVIRONMENT", "dev")
			.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
			.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath);

		// ReSharper disable once RedundantAssignment
		api = startElasticsearch
			? api
				.WithReference(elasticsearchLocal)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchLocal.GetEndpoint("http"))
				.WithEnvironment(context => context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearchLocal.Resource.PasswordParameter)
				.WithParentRelationship(elasticsearchLocal)
				.WaitFor(elasticsearchLocal)
				.WithExplicitStart()
			: api.WithReference(elasticsearchRemote)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
				.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey)
				.WithExplicitStart();

		var mcp = builder.AddProject<Projects.Elastic_Documentation_Mcp_Remote>(RemoteMcp)
			.WithArgs(GlobalArguments)
			.WithEnvironment("ENVIRONMENT", "dev");

		// ReSharper disable once RedundantAssignment
		mcp = startElasticsearch
			? mcp
				.WithReference(elasticsearchLocal)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchLocal.GetEndpoint("http"))
				.WithEnvironment(context => context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearchLocal.Resource.PasswordParameter)
				.WithParentRelationship(elasticsearchLocal)
				.WaitFor(elasticsearchLocal)
				.WithExplicitStart()
			: mcp.WithReference(elasticsearchRemote)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
				.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey)
				.WithExplicitStart();

		var indexElasticsearch = builder.AddProject<Projects.docs_builder>(ElasticsearchIngest)
			.WithArgs(["assembler", "index", .. GlobalArguments])
			.WaitForCompletion(cloneAll)
			.WithExplicitStart();

		// ReSharper disable once RedundantAssignment
		indexElasticsearch = startElasticsearch
			? indexElasticsearch
				.WaitFor(elasticsearchLocal)
				.WithReference(elasticsearchLocal)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchLocal.GetEndpoint("http"))
				.WithEnvironment(context => context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearchLocal.Resource.PasswordParameter)
				.WithParentRelationship(elasticsearchLocal)
			: indexElasticsearch
				.WithReference(elasticsearchRemote)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
				.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey)
				.WithParentRelationship(elasticsearchRemote);

		var serveStatic = builder.AddProject<Projects.docs_builder>(AssemblerServe)
			.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
			.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath)
			.WithHttpEndpoint(port: 4000, isProxied: false)
			.WithArgs(["assembler", "serve", .. GlobalArguments])
			.WithHttpHealthCheck("/", 200)
			.WaitForCompletion(buildAll)
			.WithParentRelationship(cloneAll);

		serveStatic = startElasticsearch
			? serveStatic
				.WithReference(elasticsearchLocal)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchLocal.GetEndpoint("http"))
				.WithEnvironment(context => context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearchLocal.Resource.PasswordParameter)
			: serveStatic
				.WithReference(elasticsearchRemote)
				.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
				.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey);

		// ReSharper disable once RedundantAssignment
		serveStatic = startElasticsearch ? serveStatic.WaitFor(elasticsearchLocal) : serveStatic.WaitFor(buildAll);

		await builder.Build().RunAsync(ct);
	}

	/// <summary>
	/// Extracts global doc-builder flags (--log-level, --config-source, --skip-private-repositories)
	/// from <paramref name="args"/> in-place, returning them for forwarding to docs-builder sub-processes.
	/// </summary>
	internal static string[] ExtractGlobalArgs(ref string[] args)
	{
		var global = new List<string>();
		var remaining = new List<string>();
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
				global.Add("--skip-private-repositories");
			else
				remaining.Add(args[i]);
		}
		args = [.. remaining];
		return [.. global];
	}
}
