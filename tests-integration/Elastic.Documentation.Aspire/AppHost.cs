// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

var cloneAll = builder.AddProject<Projects.docs_assembler>("DocsAssemblerCloneAll").WithArgs(["repo", "clone-all", .. globalArguments]);

var buildAll = builder.AddProject<Projects.docs_assembler>("DocsAssemblerBuildAll").WithArgs(["repo", "build-all", .. globalArguments])
	.WaitForCompletion(cloneAll);

var api = builder.AddProject<Projects.Elastic_Documentation_Api_Lambda>("ApiLambda").WithArgs(globalArguments);

var serveStatic = builder.AddProject<Projects.docs_builder>("DocsBuilderServeStatic")
	.WithHttpEndpoint(port: 4000, isProxied: false)
	.WithArgs(["serve-static", .. globalArguments])
	.WithHttpHealthCheck("/", 200)
	.WaitForCompletion(buildAll);

//builder.AddElasticsearch("elasticsearch");

builder.Build().Run();
