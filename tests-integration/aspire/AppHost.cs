// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Aspire.Hosting;
using Elastic.Documentation;
using Microsoft.Extensions.Logging;

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

var builder = DistributedApplication.CreateBuilder(args);

// Add a secret parameter named "secret"
var llmUrl = builder.AddParameter("LlmGatewayUrl", secret: true);
var llmServiceAccountPath = builder.AddParameter("LlmGatewayServiceAccountPath", secret: true);

var cloneAll = builder.AddProject<Projects.docs_assembler>("DocsAssemblerCloneAll").WithArgs(["repo", "clone-all", .. globalArguments]);

var buildAll = builder.AddProject<Projects.docs_assembler>("DocsAssemblerBuildAll").WithArgs(["repo", "build-all", .. globalArguments])
	.WaitForCompletion(cloneAll);

var elasticsearch = builder.AddElasticsearch("elasticsearch")
	.WithEnvironment("LICENSE", "trial");

var api = builder.AddProject<Projects.Elastic_Documentation_Api_Lambda>("ApiLambda").WithArgs(globalArguments)
	.WithEnvironment("ENVIRONMENT", "dev")
	.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
	.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath)
	.WaitFor(elasticsearch)
	.WithReference(elasticsearch);

var indexElasticsearch = builder.AddProject<Projects.docs_assembler>("DocsAssemblerElasticsearch")
	.WithArgs(["repo", "build-all", "--exporters", "elasticsearch", .. globalArguments])
	.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearch.GetEndpoint("http"))
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearch.Resource.PasswordParameter;
	})
	.WithReference(elasticsearch)
	.WithExplicitStart()
	.WaitFor(elasticsearch)
	.WaitForCompletion(cloneAll);

var indexElasticsearchSemantic = builder.AddProject<Projects.docs_assembler>("DocsAssemblerElasticsearchSemantic")
	.WithArgs(["repo", "build-all", "--exporters", "semantic", .. globalArguments])
	.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearch.GetEndpoint("http"))
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearch.Resource.PasswordParameter;
	})
	.WithReference(elasticsearch)
	.WithExplicitStart()
	.WaitFor(elasticsearch)
	.WaitForCompletion(cloneAll);

var serveStatic = builder.AddProject<Projects.docs_builder>("DocsBuilderServeStatic")
	.WithReference(elasticsearch)
	.WithEnvironment("LLM_GATEWAY_FUNCTION_URL", llmUrl)
	.WithEnvironment("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", llmServiceAccountPath)
	.WithEnvironment("DOCUMENTATION_ELASTIC_URL", elasticsearch.GetEndpoint("http"))
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["DOCUMENTATION_ELASTIC_PASSWORD"] = elasticsearch.Resource.PasswordParameter;
	})
	.WithHttpEndpoint(port: 4000, isProxied: false)
	.WithArgs(["serve-static", .. globalArguments])
	.WithHttpHealthCheck("/", 200)
	.WaitFor(elasticsearch)
	.WaitForCompletion(buildAll);

builder.Build().Run();
