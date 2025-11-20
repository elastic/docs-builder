// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information


using ConsoleAppFramework;
using Elastic.Documentation;
using static Elastic.Documentation.Aspire.ResourceNames;

GlobalCli.Process(ref args, out _, out var globalArguments);

await ConsoleApp.RunAsync(args, BuildAspireHost);
return;

// ReSharper disable once RedundantLambdaParameterType
// ReSharper disable once VariableHidesOuterVariable
async Task BuildAspireHost(bool startElasticsearch, bool assumeCloned, bool assumeBuild, bool skipPrivateRepositories, Cancel ctx)
{
	var builder = DistributedApplication.CreateBuilder(args);

	var llmUrl = builder.AddParameter("LlmGatewayUrl", secret: true);
	var llmServiceAccountPath = builder.AddParameter("LlmGatewayServiceAccountPath", secret: true);

	var elasticsearchUrl = builder.AddParameter("DocumentationElasticUrl", secret: true);
	var elasticsearchApiKey = builder.AddParameter("DocumentationElasticApiKey", secret: true);

	var cloneAll = builder.AddProject<Projects.docs_builder>(AssemblerClone);
	string[] cloneArgs = assumeCloned ? ["--assume-cloned"] : [];
	cloneAll = cloneAll.WithArgs(["assembler", "clone", .. globalArguments, .. cloneArgs]);

	var buildAll = builder.AddProject<Projects.docs_builder>(AssemblerBuild);
	string[] buildArgs = assumeBuild ? ["--assume-build"] : [];
	buildAll = buildAll
		.WithArgs(["assembler", "build", .. globalArguments, .. buildArgs])
		.WaitForCompletion(cloneAll)
		.WithParentRelationship(cloneAll);

	var elasticsearchLocal = builder.AddElasticsearch(ElasticsearchLocal)
		.WithEnvironment("LICENSE", "trial");
	if (!startElasticsearch)
		elasticsearchLocal = elasticsearchLocal.WithExplicitStart();

	var elasticsearchRemote = builder.AddExternalService(ElasticsearchRemote, elasticsearchUrl);

	var api = builder.AddProject<Projects.Elastic_Documentation_Api_Lambda>(LambdaApi)
		.WithArgs(globalArguments)
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

	var indexElasticsearch = builder.AddProject<Projects.docs_builder>(ElasticsearchIngest)
		.WithArgs(["assembler", "index", .. globalArguments])
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
		.WithArgs(["assembler", "serve", .. globalArguments])
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

	await builder.Build().RunAsync(ctx);
}
