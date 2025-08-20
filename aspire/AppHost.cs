// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information


using ConsoleAppFramework;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment
// ReSharper disable NotAccessedVariable

var logLevel = LogLevel.Information;
GlobalCommandLine.Process(ref args, ref logLevel, out var skipPrivateRepositories);
var globalArguments = new List<string>();
if (skipPrivateRepositories)
	globalArguments.Add("--skip-private-repositories");

if (logLevel != LogLevel.Information)
{
	globalArguments.Add("--log-level");
	globalArguments.Add(logLevel.ToString());
}

await ConsoleApp.RunAsync(args, BuildAspireHost);
return;

// ReSharper disable once RedundantLambdaParameterType
async Task BuildAspireHost(bool startElasticsearch, bool cloneAlways, bool skipPrivateRepositories, Cancel ctx)
{
	var builder = DistributedApplication.CreateBuilder(args);

	var llmUrl = builder.AddParameter("LlmGatewayUrl", secret: true);
	var llmServiceAccountPath = builder.AddParameter("LlmGatewayServiceAccountPath", secret: true);

	var elasticsearchUrl = builder.AddParameter("DocumentationElasticUrl", secret: true);
	var elasticsearchApiKey = builder.AddParameter("DocumentationElasticApiKey", secret: true);

	var cloneAll = builder.AddProject<Projects.docs_assembler>("assembler-clone");
	var cloneDirectory = Path.Combine(Paths.GetSolutionDirectory()!.FullName, ".artifacts", "checkouts", "current");
	cloneAll = cloneAlways || !Directory.Exists(cloneDirectory)
		? cloneAll.WithArgs(["repo", "clone-all", .. globalArguments])
		: cloneAll.WithArgs(["repo", "clone-all", "--help"]);

	var buildAll = builder.AddProject<Projects.docs_assembler>("assembler-build")
		.WithArgs(["repo", "build-all", .. globalArguments])
		.WaitForCompletion(cloneAll)
		.WithParentRelationship(cloneAll);

	var elasticsearchLocal = builder.AddElasticsearch("elasticsearch-local")
		.WithEnvironment("LICENSE", "trial");
	if (!startElasticsearch)
		elasticsearchLocal = elasticsearchLocal.WithExplicitStart();

	var elasticsearchRemote = builder.AddExternalService("elasticsearch-remote", elasticsearchUrl);

	var api = builder.AddProject<Projects.Elastic_Documentation_Api_Lambda>("api-lambda")
		.WithArgs(globalArguments)
		.WithEnvironment("ENVIRONMENT", "dev")
		.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
		.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath)
		.WithExplicitStart();

	api = startElasticsearch
		? api
			.WithReference(elasticsearchLocal)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchLocal.GetEndpoint("http"))
			.WithEnvironment(context => context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearchLocal.Resource.PasswordParameter)
			.WithParentRelationship(elasticsearchLocal)
			.WaitFor(elasticsearchLocal)
		: api.WithReference(elasticsearchRemote)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
			.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey);

	var indexElasticsearch = builder.AddProject<Projects.docs_assembler>("elasticsearch-indexer-plain")
		.WithArgs(["repo", "build-all", "--exporters", "elasticsearch", .. globalArguments])
		.WithExplicitStart()
		.WaitForCompletion(cloneAll);
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

	var indexElasticsearchSemantic = builder.AddProject<Projects.docs_assembler>("elasticsearch-indexer-semantic")
		.WithArgs(["repo", "build-all", "--exporters", "semantic", .. globalArguments])
		.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchLocal.GetEndpoint("http"))
		.WithEnvironment(context => context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearchLocal.Resource.PasswordParameter)
		.WithExplicitStart()
		.WaitForCompletion(cloneAll);
	indexElasticsearchSemantic = startElasticsearch
		? indexElasticsearchSemantic
			.WaitFor(elasticsearchLocal)
			.WithReference(elasticsearchLocal)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchLocal.GetEndpoint("http"))
			.WithEnvironment(context => context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearchLocal.Resource.PasswordParameter)
			.WithParentRelationship(elasticsearchLocal)
		: indexElasticsearchSemantic
			.WithReference(elasticsearchRemote)
			.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearchUrl)
			.WithEnvironment("DOCUMENTATION_ELASTIC_APIKEY", elasticsearchApiKey)
			.WithParentRelationship(elasticsearchRemote);

	var serveStatic = builder.AddProject<Projects.docs_builder>("assembler-serve")
		.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
		.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath)
		.WithHttpEndpoint(port: 4000, isProxied: false)
		.WithArgs(["serve-static", .. globalArguments])
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


	serveStatic = startElasticsearch ? serveStatic.WaitFor(elasticsearchLocal) : serveStatic.WaitFor(buildAll);

	await builder.Build().RunAsync(ctx);
}
