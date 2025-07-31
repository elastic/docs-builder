// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

var builder = DistributedApplication.CreateBuilder(args);

var cloneAll = builder.AddProject<Projects.docs_assembler>("DocsAssemblerCloneAll").WithArgs("repo", "clone-all");

var buildAll = builder.AddProject<Projects.docs_assembler>("DocsAssemblerBuildAll").WithArgs("repo", "build-all").WaitForCompletion(cloneAll);

var serveStatic = builder.AddProject<Projects.docs_builder>("DocsBuilderServeStatic")
	.WithHttpEndpoint(port: 4000, isProxied: false)
	.WithArgs("serve-static")
	.WithHttpHealthCheck("/", 200)
	.WaitForCompletion(buildAll);

//builder.AddElasticsearch("elasticsearch");

builder.Build().Run();
